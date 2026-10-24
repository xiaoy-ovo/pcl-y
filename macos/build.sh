#!/bin/bash
# ============================================================================
# 构建 PCL-Y 的 macOS .app（Wine 封装，界面与原版完全一致）
#
# 输入：一个包含 pcl-y.exe 及其依赖 DLL 的目录
# 输出：dist/PCL-Y.app + dist/PCL-Y-<version>-mac.zip + .tar.gz
#
# 用法：
#   bash macos/build.sh                          # 用 macos/vendor/1.2.0
#   bash macos/build.sh --input /path/to/exe-dir
#   bash macos/build.sh --version 1.2.0 --input ../out
# ============================================================================
set -euo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
REPO="$(cd "$HERE/.." && pwd)"
DIST="$REPO/dist"

APP_NAME="PCL-Y"
VERSION="1.2.0"
INPUT=""
SKIP_ZIP=0

while [ $# -gt 0 ]; do
  case "$1" in
    --input)   INPUT="$2"; shift 2 ;;
    --version) VERSION="$2"; shift 2 ;;
    --output)  DIST="$2"; shift 2 ;;
    --no-zip)  SKIP_ZIP=1; shift ;;
    -h|--help) sed -n '2,14p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *) echo "未知参数: $1" >&2; exit 2 ;;
  esac
done

[ -n "$INPUT" ] || INPUT="$HERE/vendor/$VERSION"

cyan() { printf '\033[36m==>\033[0m %s\n' "$*"; }
gray() { printf '    %s\n' "$*"; }
warn() { printf '\033[33m[!]\033[0m %s\n' "$*"; }
die()  { printf '\033[31m[x]\033[0m %s\n' "$*" >&2; exit 1; }

[ -d "$INPUT" ] || die "输入目录不存在: $INPUT
先执行： bash macos/fetch-release.sh"

# ── 清理 ────────────────────────────────────────────────────────────────────
cyan "清理旧产物"
APP="$DIST/$APP_NAME.app"
rm -rf "$APP"
rm -f "$DIST/$APP_NAME-$VERSION-mac.zip" "$DIST/$APP_NAME-$VERSION-mac.tar.gz"
mkdir -p "$DIST"

CONTENTS="$APP/Contents"
MACOS_DIR="$CONTENTS/MacOS"
RES="$CONTENTS/Resources"
RES_APP="$RES/app"
RES_SCRIPTS="$RES/scripts"
mkdir -p "$MACOS_DIR" "$RES_APP" "$RES_SCRIPTS"

# ── 复制原版程序 ────────────────────────────────────────────────────────────
cyan "复制原版程序文件"
REQUIRED="pcl-y.exe pcl-y.exe.config MeloongCore.dll MeloongCore.Wpf.dll PCLCS.dll Microsoft.Win32.Registry.dll"
COUNT=0
for f in $REQUIRED; do
  if [ -f "$INPUT/$f" ]; then
    cp "$INPUT/$f" "$RES_APP/$f"
    gray "$f ($(du -h "$INPUT/$f" | cut -f1))"
    COUNT=$((COUNT + 1))
  else
    warn "缺少 $f"
  fi
done
[ "$COUNT" -gt 0 ] || die "$INPUT 里没有找到任何程序文件"

# ── 启动脚本 ────────────────────────────────────────────────────────────────
cyan "安装启动脚本"
cp "$HERE/launcher/launch.sh" "$MACOS_DIR/$APP_NAME"
cp "$HERE/launcher/setup.sh"  "$RES_SCRIPTS/setup.sh"
cp "$HERE/launcher/doctor.sh" "$RES_SCRIPTS/doctor.sh"
cp "$HERE/launcher/README.txt" "$RES_SCRIPTS/README.txt"
cp "$HERE/launcher/README.txt" "$CONTENTS/启动说明.txt"
gray "Contents/MacOS/$APP_NAME"
gray "Contents/Resources/scripts/{setup,doctor}.sh"

# 去掉可能残留的 CRLF
if command -v perl >/dev/null 2>&1; then
  find "$MACOS_DIR" "$RES_SCRIPTS" -type f -exec perl -pi -e 's/\r\n/\n/g' {} \;
fi

# ── Info.plist ──────────────────────────────────────────────────────────────
cyan "写入 Info.plist"
sed "s|<string>1\.2\.0</string>|<string>$VERSION</string>|g" "$HERE/Info.plist" > "$CONTENTS/Info.plist"
printf 'APPL????' > "$CONTENTS/PkgInfo"
gray "CFBundleExecutable = $APP_NAME"

# ── 图标 ────────────────────────────────────────────────────────────────────
cyan "图标"
if [ -f "$HERE/assets/appicon.icns" ]; then
  cp "$HERE/assets/appicon.icns" "$RES/appicon.icns"
  gray "appicon.icns ($(du -h "$HERE/assets/appicon.icns" | cut -f1))"
else
  warn "缺少 assets/appicon.icns"
fi

# ── 权限 ────────────────────────────────────────────────────────────────────
cyan "设置可执行权限"
chmod +x "$MACOS_DIR/$APP_NAME" "$RES_SCRIPTS/setup.sh" "$RES_SCRIPTS/doctor.sh"
gray "chmod +x 完成"

# ── 签名（仅 macOS）────────────────────────────────────────────────────────
if command -v codesign >/dev/null 2>&1; then
  cyan "ad-hoc 代码签名"
  xattr -cr "$APP" 2>/dev/null || true
  if codesign --force --deep --sign - "$APP" 2>&1; then
    gray "签名成功"
    codesign -dv "$APP" 2>&1 | head -3 | sed 's/^/    /'
  else
    warn "签名失败（不影响本地运行）"
  fi
else
  gray "非 macOS 环境，跳过签名；目标机执行 bash macos/post-install.sh"
fi

# ── 统计 ────────────────────────────────────────────────────────────────────
cyan "bundle 统计"
FILES=$(find "$APP" -type f | wc -l | tr -d ' ')
SIZE=$(du -sh "$APP" | cut -f1)
gray "$FILES 个文件, $SIZE"

# ── 打包 ────────────────────────────────────────────────────────────────────
if [ "$SKIP_ZIP" -eq 1 ]; then
  echo
  echo "完成: $APP"
  exit 0
fi

cyan "打包 zip（写入 Unix 权限位）"
ZIP="$DIST/$APP_NAME-$VERSION-mac.zip"
if command -v ditto >/dev/null 2>&1; then
  ( cd "$DIST" && ditto -c -k --sequesterRsrc --keepParent "$APP_NAME.app" "$(basename "$ZIP")" )
  gray "ditto 打包"
elif command -v python3 >/dev/null 2>&1; then
  python3 "$HERE/zip_app.py" "$APP" "$ZIP" | sed 's/^/    /'
elif command -v python >/dev/null 2>&1; then
  python "$HERE/zip_app.py" "$APP" "$ZIP" | sed 's/^/    /'
else
  ( cd "$DIST" && zip -qry "$(basename "$ZIP")" "$APP_NAME.app" )
  warn "没有 python/ditto，用 zip 打包（会丢失可执行位，目标机需跑 post-install.sh）"
fi

cyan "打包 tar.gz（保留权限位的备用格式）"
TAR="$DIST/$APP_NAME-$VERSION-mac.tar.gz"
( cd "$DIST" && tar -czf "$(basename "$TAR")" "$APP_NAME.app" )
gray "$(basename "$TAR")"

echo
echo "════════════════════════════════════════"
echo "  $APP"
[ -f "$ZIP" ] && echo "  $ZIP  ($(du -h "$ZIP" | cut -f1))"
[ -f "$TAR" ] && echo "  $TAR  ($(du -h "$TAR" | cut -f1))"
echo "════════════════════════════════════════"
echo
echo "在 macOS 上安装："
echo "  1) 解压后把 PCL-Y.app 拖进 /Applications"
echo "  2) xattr -cr \"/Applications/PCL-Y.app\""
echo "  3) 双击（首次会装 Wine 环境，5-15 分钟）"
echo "  4) 装完再双击一次"
