#!/bin/bash
# ============================================================================
# PCL-Y 环境诊断：跑不起来时先执行这个
# ============================================================================
set -uo pipefail

SUPPORT="$HOME/Library/Application Support/PCL-Y-Mac"
PREFIX="$SUPPORT/wineprefix"

G='\033[32m'; Y='\033[33m'; R='\033[31m'; C='\033[36m'; N='\033[0m'
ok()   { printf "${G}  OK ${N} %s\n" "$*"; }
bad()  { printf "${R}  !! ${N} %s\n" "$*"; }
warn() { printf "${Y}  ?? ${N} %s\n" "$*"; }
hdr()  { printf "\n${C}── %s %s${N}\n" "$*" "$(printf '─%.0s' $(seq 1 $((46 - ${#1}))))"; }

echo "=============================================================="
echo " PCL-Y macOS 诊断  $(date '+%Y-%m-%d %H:%M:%S')"
echo "=============================================================="

hdr "系统"
if command -v sw_vers >/dev/null 2>&1; then
  ok "macOS $(sw_vers -productVersion) ($(sw_vers -buildVersion))"
else
  warn "非 macOS 环境（$(uname -s)），部分检查项跳过"
fi
ok "架构 $(uname -m)"
[ "$(uname -m)" = "arm64" ] && warn "Apple Silicon：Wine 需要 Rosetta 2（softwareupdate --install-rosetta）"

hdr "Wine"
WINE=""
for c in wine64 wine wine-stable wine-staging; do
  command -v "$c" >/dev/null 2>&1 && { WINE="$(command -v "$c")"; break; }
done
if [ -z "$WINE" ]; then
  for p in \
    "/Applications/Wine Stable.app/Contents/Resources/wine/bin/wine64" \
    "/Applications/Wine Staging.app/Contents/Resources/wine/bin/wine64" \
    "/Applications/Wine Stable.app/Contents/Resources/wine/bin/wine" \
    "/Applications/Wine Staging.app/Contents/Resources/wine/bin/wine" \
    "/Applications/CrossOver.app/Contents/SharedSupport/CrossOver/bin/wine" \
    "/opt/homebrew/bin/wine64" "/usr/local/bin/wine64"
  do
    [ -x "$p" ] && { WINE="$p"; break; }
  done
fi

if [ -n "$WINE" ]; then
  ok "找到 $WINE"
  "$WINE" --version 2>/dev/null | sed 's/^/     /'
else
  bad "未找到 Wine —— brew install --cask --no-quarantine wine-stable"
fi

hdr "winetricks"
if command -v winetricks >/dev/null 2>&1; then
  ok "$(command -v winetricks)"
else
  warn "未找到 —— brew install winetricks"
fi

hdr "wineprefix"
if [ -d "$PREFIX/drive_c" ]; then
  ok "$PREFIX"
  du -sh "$PREFIX" 2>/dev/null | sed 's/^/     /'
else
  bad "prefix 不存在：$PREFIX"
fi

hdr ".NET Framework"
if [ -n "$WINE" ] && [ -d "$PREFIX" ]; then
  export WINEPREFIX="$PREFIX" WINEDEBUG=-all
  if "$WINE" reg query 'HKLM\Software\Microsoft\NET Framework Setup\NDP\v4\Full' /v Release >/dev/null 2>&1; then
    REL="$("$WINE" reg query 'HKLM\Software\Microsoft\NET Framework Setup\NDP\v4\Full' /v Release 2>/dev/null | grep -oE '[0-9]+' | tail -1)"
    if [ -n "${REL:-}" ] && [ "$REL" -ge 528040 ] 2>/dev/null; then
      ok "4.8 已安装 (Release=$REL)"
    else
      warn "已安装但不是 4.8 (Release=${REL:-?})，PCL 可能报错"
    fi
  else
    bad "未安装 .NET Framework —— 运行 setup.sh"
  fi

  hdr "字体"
  FONT_DST="$PREFIX/drive_c/windows/Fonts"
  for f in "PingFang.ttc" "STHeiti Light.ttc" "Arial Unicode.ttf"; do
    [ -e "$FONT_DST/$f" ] && ok "$f" || warn "缺少 $f（中文可能显示方块）"
  done
fi

hdr "应用文件"
APP="${1:-}"
if [ -z "$APP" ]; then
  HERE="$(cd "$(dirname "$0")" && pwd)"
  # .app 内布局：Contents/Resources/scripts/doctor.sh → ../app
  # 开发态布局：mac/launcher/doctor.sh        → ../bundle/Resources/app
  for cand in "$HERE/../app" "$HERE/../bundle/Resources/app" "$HERE/../../Resources/app"; do
    if [ -d "$cand" ]; then APP="$(cd "$cand" && pwd)"; break; fi
  done
fi
if [ -n "$APP" ] && [ -d "$APP" ]; then
  ok "$APP"
  for f in pcl-y.exe pcl-y.exe.config MeloongCore.dll MeloongCore.Wpf.dll PCLCS.dll; do
    [ -f "$APP/$f" ] && ok "  $f ($(du -h "$APP/$f" | cut -f1))" || bad "  缺 $f"
  done
else
  warn "未定位到应用目录，可传入参数： bash doctor.sh /path/to/Resources/app"
fi

hdr "日志"
if [ -d "$SUPPORT/Logs" ]; then
  ls -lt "$SUPPORT/Logs" | head -6 | sed 's/^/     /'
  LAST="$(ls -t "$SUPPORT/Logs"/*.log 2>/dev/null | head -1)"
  if [ -n "${LAST:-}" ]; then
    echo
    echo "     最近 20 行 ($(basename "$LAST")):"
    tail -20 "$LAST" | sed 's/^/       /'
  fi
else
  warn "还没有日志"
fi

hdr "建议"
cat <<'EOF'
  1. 首次运行必须先跑 setup.sh（安装 .NET Framework 4.8）
  2. 中文显示方块 → 运行 setup.sh 里的字体部分，或 brew install --cask font-noto-sans-cjk
  3. 白屏 / 闪退 → 用 --debug 启动看 Wine 输出：
       ./PCL-Y.app/Contents/MacOS/PCL-Y --debug
  4. 界面模糊 → 系统设置里把 PCL-Y 的「使用低分辨率打开」取消勾选
  5. CrossOver 用户：把 PCLY_WINE 指向 CrossOver 的 wine 可执行文件
EOF

echo
