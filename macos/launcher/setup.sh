#!/bin/bash
# ============================================================================
# PCL-Y 运行环境初始化（只在首次运行时跑一次）
#
# 做的事情：
#   1. 建 64 位 wineprefix
#   2. 装 .NET Framework 4.8（WPF 必需，wine-mono 跑不了 WPF）
#   3. 装常用字体（中文显示必需）
#   4. 装 winetricks 依赖（corefonts / gdiplus / 等）
#   5. 打上 PCL-Y 专用的注册表项
#   6. 写 .pcly-ready 标记
# ============================================================================
set -uo pipefail

SUPPORT="$HOME/Library/Application Support/PCL-Y-Mac"
PREFIX="$SUPPORT/wineprefix"
LOG_DIR="$SUPPORT/Logs"
DOWNLOADS="$SUPPORT/downloads"
mkdir -p "$SUPPORT" "$LOG_DIR" "$DOWNLOADS"

LOG="$LOG_DIR/setup-$(date +%Y%m%d-%H%M%S).log"

log()  { printf '\033[36m==>\033[0m %s\n' "$*" | tee -a "$LOG"; }
warn() { printf '\033[33m[!]\033[0m %s\n' "$*" | tee -a "$LOG"; }
die()  { printf '\033[31m[x]\033[0m %s\n' "$*" | tee -a "$LOG"; read -r -p "按回车退出..."; exit 1; }

exec > >(tee -a "$LOG") 2>&1

clear
cat <<'BANNER'
  ____   ____ _        __   __  ___
 |  _ \ / ___| |       \ \ / / |__ \
 | |_) | |   | |        \ V /     ) |
 |  __/| |___| |___      | |     / /
 |_|    \____|_____|     |_|    |_|

  PCL-Y macOS 运行环境安装
BANNER
echo
echo "日志：$LOG"
echo

# ── 1. 找 Wine ──────────────────────────────────────────────────────────────
find_wine() {
  [ -n "${PCLY_WINE:-}" ] && [ -x "${PCLY_WINE}" ] && { echo "$PCLY_WINE"; return; }
  local c
  for c in wine64 wine wine-stable wine-staging; do
    command -v "$c" >/dev/null 2>&1 && { command -v "$c"; return; }
  done
  local p
  for p in \
    "/Applications/Wine Stable.app/Contents/Resources/wine/bin/wine64" \
    "/Applications/Wine Staging.app/Contents/Resources/wine/bin/wine64" \
    "/Applications/Wine Stable.app/Contents/Resources/wine/bin/wine" \
    "/Applications/Wine Staging.app/Contents/Resources/wine/bin/wine" \
    "/Applications/CrossOver.app/Contents/SharedSupport/CrossOver/bin/wine" \
    "/opt/homebrew/bin/wine64" "/usr/local/bin/wine64" \
    "/opt/homebrew/bin/wine"   "/usr/local/bin/wine"
  do
    [ -x "$p" ] && { echo "$p"; return; }
  done
}

WINE="$(find_wine || true)"
[ -z "$WINE" ] && die "未找到 Wine。请先执行： brew install --cask --no-quarantine wine-stable"

WINE_BIN_DIR="$(dirname "$WINE")"
export PATH="$WINE_BIN_DIR:$PATH"
log "Wine: $WINE"
"$WINE" --version || die "Wine 无法运行"

# ── 2. 找 winetricks ────────────────────────────────────────────────────────
WINETRICKS="$(command -v winetricks || true)"
if [ -z "$WINETRICKS" ]; then
  for p in /opt/homebrew/bin/winetricks /usr/local/bin/winetricks "$WINE_BIN_DIR/winetricks"; do
    [ -x "$p" ] && { WINETRICKS="$p"; break; }
  done
fi
if [ -z "$WINETRICKS" ]; then
  warn "未找到 winetricks，尝试自动下载"
  curl -fsSL "https://raw.githubusercontent.com/Winetricks/winetricks/master/src/winetricks" \
       -o "$DOWNLOADS/winetricks" || die "winetricks 下载失败，请执行： brew install winetricks"
  chmod +x "$DOWNLOADS/winetricks"
  WINETRICKS="$DOWNLOADS/winetricks"
fi
log "winetricks: $WINETRICKS"

export WINEPREFIX="$PREFIX"
export WINEARCH=win64
export WINEDEBUG=-all
export WINEDLLOVERRIDES="mscoree=d;mshtml=d"
export WINETRICKS="$WINETRICKS"

# ── 3. 建 prefix ────────────────────────────────────────────────────────────
if [ ! -d "$PREFIX/drive_c" ]; then
  log "创建 wineprefix（64 位）..."
  "$WINE" wineboot --init || die "wineboot 失败"
  sleep 3
  # 关掉 mono / gecko 安装弹窗
  "$WINE" reg add "HKCU\\Software\\Wine\\WineDbg" /v ShowCrashDialog /t REG_DWORD /d 0 /f >/dev/null 2>&1
else
  log "复用已有 wineprefix"
fi

# 等 wineserver 稳定
"$WINE" wineserver -w 2>/dev/null || true

# ── 4. .NET Framework 4.8 ───────────────────────────────────────────────────
NET48_KEY='HKLM\Software\Microsoft\NET Framework Setup\NDP\v4\Full'
if "$WINE" reg query "$NET48_KEY" /v Release >/dev/null 2>&1; then
  REL="$("$WINE" reg query "$NET48_KEY" /v Release 2>/dev/null | grep -oE '[0-9]+' | tail -1)"
  log ".NET Framework 已安装（Release=$REL）"
else
  log "安装 .NET Framework 4.8（这步最慢，5–15 分钟，别关窗口）"
  # winetricks 的 dotnet48 会自己处理 WPF 所需的组件
  "$WINETRICKS" -q dotnet48 || warn "dotnet48 安装返回非零，继续尝试"

  if ! "$WINE" reg query "$NET48_KEY" /v Release >/dev/null 2>&1; then
    warn "winetricks 安装失败，改用离线安装包"
    NDP="$DOWNLOADS/ndp48-x86-x64-allos-enu.exe"
    if [ ! -f "$NDP" ]; then
      log "下载 .NET Framework 4.8 离线安装包"
      curl -fL --retry 3 -o "$NDP" \
        "https://go.microsoft.com/fwlink/?linkid=2088631" || die "下载失败"
    fi
    "$WINE" "$NDP" /q /norestart || warn "静默安装返回非零"
    "$WINE" wineserver -w 2>/dev/null || true
  fi
fi

# ── 5. 字体（中文必需） ─────────────────────────────────────────────────────
log "安装字体"
"$WINETRICKS" -q corefonts 2>/dev/null || warn "corefonts 安装跳过"
"$WINETRICKS" -q cjkfonts 2>/dev/null || warn "cjkfonts 安装跳过（可稍后手动装）"

# 把 macOS 的中文字体软链进 prefix，WPF 找不到字体会显示方块
FONT_SRC="/System/Library/Fonts"
FONT_DST="$PREFIX/drive_c/windows/Fonts"
mkdir -p "$FONT_DST"
for f in "PingFang.ttc" "STHeiti Light.ttc" "Hiragino Sans GB.ttc" "Arial Unicode.ttf"; do
  [ -f "$FONT_SRC/$f" ] && ln -sf "$FONT_SRC/$f" "$FONT_DST/$f" 2>/dev/null
done
# 注册中文字体映射
"$WINE" reg add "HKCU\\Software\\Wine\\Fonts\\Replacements" /v "Microsoft YaHei"   /d "PingFang SC" /f >/dev/null 2>&1
"$WINE" reg add "HKCU\\Software\\Wine\\Fonts\\Replacements" /v "SimSun"           /d "Songti SC"   /f >/dev/null 2>&1
"$WINE" reg add "HKCU\\Software\\Wine\\Fonts\\Replacements" /v "SimHei"           /d "Heiti SC"    /f >/dev/null 2>&1
"$WINE" reg add "HKCU\\Software\\Wine\\Fonts\\Replacements" /v "MS Gothic"        /d "Hiragino Sans" /f >/dev/null 2>&1

# ── 6. WPF 相关组件 ────────────────────────────────────────────────────────
log "安装 WPF 依赖组件"
for pkg in gdiplus riched20 msxml6 vcrun2019; do
  "$WINETRICKS" -q "$pkg" 2>/dev/null && log "  $pkg OK" || warn "  $pkg 跳过"
done

# ── 6.5 Windows 版 JRE（PCL 用它启动 Minecraft）───────────────────────────
# macOS 自带的 java 是 Mach-O，Wine 跑不了；PCL 需要 java.exe。
install_jre() {
  local JRE_DIR="C:\\Program Files\\Eclipse Adoptium"
  local HOST_JRE="$PREFIX/drive_c/Program Files/Eclipse Adoptium"

  if [ -x "$HOST_JRE"/*/bin/java.exe ] 2>/dev/null; then
    log "Windows JRE 已存在"
    return
  fi

  log "下载 Windows 版 Eclipse Temurin JRE 21"
  local URL="https://api.adoptium.net/v3/binary/latest/21/ga/windows/x64/jre/hotspot/normal/eclipse"
  local ZIP="$DOWNLOADS/temurin-jre21-win-x64.zip"
  local UNZIP="$DOWNLOADS/temurin-jre21"

  if [ ! -f "$ZIP" ]; then
    curl -fL --retry 3 --progress-bar -o "$ZIP" "$URL" || { warn "JRE 下载失败，可稍后手动安装"; return; }
  fi

  rm -rf "$UNZIP"; mkdir -p "$UNZIP"
  unzip -q "$ZIP" -d "$UNZIP" || { warn "JRE 解压失败"; return; }

  local SRC
  SRC="$(find "$UNZIP" -maxdepth 3 -type d \( -name '*jre*' -o -name 'jdk-*' \) -not -path '*/bin/*' | head -1)"
  [ -z "$SRC" ] && { warn "JRE 目录结构异常"; return; }

  mkdir -p "$HOST_JRE"
  local NAME
  NAME="$(basename "$SRC")"
  rm -rf "$HOST_JRE/$NAME"
  cp -R "$SRC" "$HOST_JRE/$NAME" || { warn "JRE 复制失败"; return; }

  log "  JRE 安装到 $JRE_DIR\\$NAME"

  # 让 PCL 下次扫描能直接找到
  "$WINE" reg add "HKCU\\Software\\PCL" /v JavaPath \
    /d "C:\\Program Files\\Eclipse Adoptium\\$NAME\\bin\\java.exe" /f >/dev/null 2>&1 || true
}
install_jre || true

# ── 7. 显卡/DPI 相关注册表 ─────────────────────────────────────────────────
log "写入显示与性能设置"
"$WINE" reg add "HKCU\\Software\\Wine\\Direct3D" /v renderer /d gl /f >/dev/null 2>&1
"$WINE" reg add "HKCU\\Software\\Wine\\Direct3D" /v VideoMemorySize /d 2048 /f >/dev/null 2>&1
"$WINE" reg add "HKCU\\Control Panel\\Desktop" /v LogPixels /t REG_DWORD /d 96 /f >/dev/null 2>&1
"$WINE" reg add "HKCU\\Control Panel\\Desktop" /v Win8DpiScaling /t REG_DWORD /d 0 /f >/dev/null 2>&1

# 关闭 Wine 的崩溃对话框，PCL 自己会处理异常
"$WINE" reg add "HKCU\\Software\\Wine\\WineDbg" /v ShowCrashDialog /t REG_DWORD /d 0 /f >/dev/null 2>&1

# ── 8. 完成 ────────────────────────────────────────────────────────────────
"$WINE" wineserver -w 2>/dev/null || true

cat > "$PREFIX/.pcly-ready" <<EOF
installed_at=$(date '+%Y-%m-%d %H:%M:%S')
wine=$WINE
winetricks=$WINETRICKS
log=$LOG
EOF

echo
log "安装完成"
echo
echo "现在可以关闭这个终端窗口，回到 Finder 双击 PCL-Y.app 启动。"
echo
# 非交互执行（自动化/CI）时 read 会返回非 0，不能让它污染退出码
read -r -p "按回车关闭..." _ || true
exit 0
