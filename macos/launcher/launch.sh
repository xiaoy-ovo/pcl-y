#!/bin/bash
# ============================================================================
# PCL-Y macOS 启动器
# 用 Wine 跑原版 pcl-y.exe（.NET Framework 4.8 + WPF），界面完全保持原样。
# 本脚本由 .app bundle 的 Contents/MacOS/PCL-Y 调用。
# ============================================================================
set -uo pipefail

# ── 路径 ────────────────────────────────────────────────────────────────────
SELF="$(cd "$(dirname "$0")" && pwd)"
if [ -d "$SELF/../Resources" ]; then
  CONTENTS="$(cd "$SELF/.." && pwd)"           # .app/Contents
else
  CONTENTS="$(cd "$SELF/../bundle" && pwd)"    # 开发态：mac/bundle
fi

RES="$CONTENTS/Resources"
APP_DIR="$RES/app"
SCRIPTS="$RES/scripts"

SUPPORT="$HOME/Library/Application Support/PCL-Y-Mac"
PREFIX="$SUPPORT/wineprefix"
LOG_DIR="$SUPPORT/Logs"
mkdir -p "$SUPPORT" "$LOG_DIR"
LOG="$LOG_DIR/launcher-$(date +%Y%m%d-%H%M%S).log"

EXE_NAME="pcl-y.exe"
DEBUG=0
[ "${1:-}" = "--debug" ] && DEBUG=1

# ── 输出 ────────────────────────────────────────────────────────────────────
log()  { printf '%s\n' "$*" | tee -a "$LOG" >&2; }
die()  {
  log "错误: $*"
  osascript -e "display alert \"PCL-Y 启动失败\" message \"$*\" as critical" >/dev/null 2>&1
  exit 1
}
dialog() {
  osascript -e "display dialog \"$1\" buttons {\"好\"} default button 1 with title \"PCL-Y\"" >/dev/null 2>&1
}

log "=============================================================="
log "PCL-Y macOS launcher  $(date '+%Y-%m-%d %H:%M:%S')"
log "bundle : $CONTENTS"
log "prefix : $PREFIX"
log "=============================================================="

[ -f "$APP_DIR/$EXE_NAME" ] || die "应用文件缺失：$APP_DIR/$EXE_NAME（请重新解压）"

# ── 定位 Wine ───────────────────────────────────────────────────────────────
find_wine() {
  if [ -n "${PCLY_WINE:-}" ] && [ -x "${PCLY_WINE}" ]; then
    echo "$PCLY_WINE"; return 0
  fi

  local c
  for c in wine64 wine wine-stable wine-staging; do
    if command -v "$c" >/dev/null 2>&1; then
      command -v "$c"; return 0
    fi
  done

  local p
  for p in \
    "/Applications/Wine Stable.app/Contents/Resources/wine/bin/wine64" \
    "/Applications/Wine Staging.app/Contents/Resources/wine/bin/wine64" \
    "/Applications/Wine Stable.app/Contents/Resources/wine/bin/wine" \
    "/Applications/Wine Staging.app/Contents/Resources/wine/bin/wine" \
    "/Applications/CrossOver.app/Contents/SharedSupport/CrossOver/bin/wine" \
    "$HOME/Applications/Wine Stable.app/Contents/Resources/wine/bin/wine64" \
    "/opt/homebrew/bin/wine64" \
    "/usr/local/bin/wine64" \
    "/opt/homebrew/bin/wine" \
    "/usr/local/bin/wine"
  do
    [ -x "$p" ] && { echo "$p"; return 0; }
  done

  return 1
}

WINE="$(find_wine || true)"
if [ -z "$WINE" ]; then
  log "未找到 Wine"
  osascript <<'APPLESCRIPT' >/dev/null 2>&1
display alert "需要先安装 Wine" message "PCL-Y 是 .NET Framework + WPF 程序，macOS 上必须通过 Wine 运行。

推荐安装方式（任选其一）：

1) Homebrew（推荐）
   brew install --cask --no-quarantine wine-stable
   brew install winetricks

2) CrossOver（付费，兼容性最好）
   https://www.codeweavers.com/crossover

安装完成后重新打开 PCL-Y。" as critical
APPLESCRIPT
  # 顺便打开 Wine 下载页
  open "https://github.com/Gcenx/macOS_Wine_builds/releases" >/dev/null 2>&1 || true
  exit 1
fi

log "wine   : $WINE"
"$WINE" --version >>"$LOG" 2>&1 || log "警告: wine --version 执行失败"

# ── 首次运行检查 ────────────────────────────────────────────────────────────
MARKER="$PREFIX/.pcly-ready"
if [ ! -f "$MARKER" ]; then
  log "wineprefix 未初始化，启动安装流程"
  dialog "首次运行需要初始化运行环境（安装 .NET Framework 4.8），大约 5–15 分钟。已打开终端窗口，请按提示操作，完成后再重新打开 PCL-Y。"

  if [ -x "$SCRIPTS/setup.sh" ]; then
    chmod +x "$SCRIPTS/setup.sh" 2>/dev/null || true
    open -a Terminal "$SCRIPTS/setup.sh" >/dev/null 2>&1 \
      || osascript -e "tell application \"Terminal\" to do script \"bash '$SCRIPTS/setup.sh'\"" >/dev/null 2>&1 \
      || die "无法打开终端运行 setup.sh，请手动执行：bash \"$SCRIPTS/setup.sh\""
  else
    die "缺少 setup.sh，无法初始化运行环境"
  fi
  exit 0
fi

# ── Wine 环境变量 ───────────────────────────────────────────────────────────
export WINEPREFIX="$PREFIX"
export WINEARCH=win64
export WINEDEBUG="${WINEDEBUG:--all}"
[ "$DEBUG" = "1" ] && export WINEDEBUG="+seh,+loaddll,+warn"

# WPF 需要完整的 .NET，禁止 wine-mono 接管
export WINEDLLOVERRIDES="mscoree=d;mshtml=d"
export WINE_LARGE_ADDRESS_AWARE=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1

# 让 Wine 使用 macOS 的字体渲染
export FREETYPE_PROPERTIES="truetype:interpreter-version=35"

# 关掉 Wine 的 mono/gecko 弹窗
export WINEDLLOVERRIDES="$WINEDLLOVERRIDES;winemenubuilder.exe=d"

cd "$APP_DIR" || die "无法进入应用目录"

log "启动 $EXE_NAME ..."
log "日志: $LOG"

# ── 启动 ────────────────────────────────────────────────────────────────────
# PCL 会往当前目录写配置，工作目录必须是 app 目录
if [ "$DEBUG" = "1" ]; then
  "$WINE" "$EXE_NAME" 2>&1 | tee -a "$LOG"
  exit ${PIPESTATUS[0]}
else
  "$WINE" "$EXE_NAME" >>"$LOG" 2>&1
  code=$?
  log "退出码: $code"
  if [ "$code" -ne 0 ] && [ "$code" -ne 1 ]; then
    dialog "PCL-Y 异常退出（代码 $code）。日志：$LOG"
  fi
  exit "$code"
fi
