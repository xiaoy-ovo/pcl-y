#!/bin/bash
# ============================================================================
# launch.sh 的离线测试：用假 wine 验证路径解析、环境变量、参数传递
# 用法： bash test-launch.sh
# ============================================================================
set -uo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"          # mac/
DIST="$(cd "$ROOT/.." && pwd)/dist"
APP="$DIST/PCL-Y.app"

PASS=0; FAIL=0
G='\033[32m'; R='\033[31m'; N='\033[0m'
ok()   { PASS=$((PASS+1)); printf "${G}  PASS${N}  %s\n" "$*"; }
no()   { FAIL=$((FAIL+1)); printf "${R}  FAIL${N}  %s\n" "$*"; }
eq()   { if [ "$2" = "$3" ]; then ok "$1"; else no "$1 (期望 [$3] 实际 [$2])"; fi; }

echo "════ launch.sh 离线测试 ════"

[ -d "$APP" ] || { echo "先跑 build.sh / build.ps1"; exit 1; }

# ── 准备假 wine ─────────────────────────────────────────────────────────────
FAKE_HOME="$(mktemp -d)"
mkdir -p "$FAKE_HOME/bin" "$FAKE_HOME/AppData"

cat > "$FAKE_HOME/bin/wine" <<'STUB'
#!/bin/bash
# 假 wine：把调用记录下来，方便断言
echo "ARGV: $*" >> "$STUB_LOG"
case "$1" in
  --version) echo "wine-9.0 (Stub)"; exit 0 ;;
  reg)       exit 0 ;;
  wineserver) exit 0 ;;
esac
# 模拟 PCL 启动后立刻退出
echo "STUB: launched $1 in cwd=$(pwd)" >> "$STUB_LOG"
exit 0
STUB
chmod +x "$FAKE_HOME/bin/wine"

# ── 测试 1：找不到 wine 时的行为 ────────────────────────────────────────────
echo
echo "── 测试 1：没有 Wine 时应给出安装提示 ──"
OUT="$(HOME="$FAKE_HOME" PCLY_WINE="" PATH="/usr/bin:/bin" \
       bash "$APP/Contents/MacOS/PCL-Y" 2>&1)"
CODE=$?
if echo "$OUT" | grep -q "未找到 Wine"; then
  ok "提示未找到 Wine"
else
  no "缺少 Wine 提示（输出: $(echo "$OUT" | head -3)）"
fi
eq "退出码非 0" "$([ "$CODE" -ne 0 ] && echo yes || echo no)" "yes"

# ── 测试 2：prefix 未初始化时应提示跑 setup ─────────────────────────────────
echo
echo "── 测试 2：prefix 未初始化时引导 setup.sh ──"
export STUB_LOG="$FAKE_HOME/stub1.log"
OUT="$(HOME="$FAKE_HOME" PCLY_WINE="$FAKE_HOME/bin/wine" PATH="$FAKE_HOME/bin:/usr/bin:/bin" \
       bash "$APP/Contents/MacOS/PCL-Y" 2>&1)"
if echo "$OUT" | grep -q "wineprefix 未初始化"; then
  ok "提示首次运行初始化"
else
  no "缺少首次运行提示（输出: $(echo "$OUT" | head -5)）"
fi
if echo "$OUT" | grep -q "wine   : $FAKE_HOME/bin/wine"; then
  ok "正确识别 wine 路径"
else
  no "wine 路径未记录"
fi

# ── 测试 3：prefix 就绪后正常启动 ───────────────────────────────────────────
echo
echo "── 测试 3：prefix 就绪后启动 pcl-y.exe ──"
SUPPORT="$FAKE_HOME/Library/Application Support/PCL-Y-Mac"
mkdir -p "$SUPPORT/wineprefix"
touch "$SUPPORT/wineprefix/.pcly-ready"
export STUB_LOG="$FAKE_HOME/stub2.log"
: > "$STUB_LOG"

HOME="$FAKE_HOME" PCLY_WINE="$FAKE_HOME/bin/wine" PATH="$FAKE_HOME/bin:/usr/bin:/bin" \
  bash "$APP/Contents/MacOS/PCL-Y" >/dev/null 2>&1
CODE=$?

eq "退出码 0" "$CODE" "0"

if grep -q "ARGV: pcl-y.exe" "$STUB_LOG" 2>/dev/null; then
  ok "调用 wine pcl-y.exe"
else
  no "没有调用 wine（日志: $(cat "$STUB_LOG" 2>/dev/null | head -3)）"
fi

if grep -q "launched pcl-y.exe in cwd=$APP/Contents/Resources/app" "$STUB_LOG" 2>/dev/null; then
  ok "工作目录切到 Resources/app"
else
  no "工作目录错误（日志: $(grep launched "$STUB_LOG" 2>/dev/null)）"
fi

# ── 测试 4：日志落盘 ────────────────────────────────────────────────────────
echo
echo "── 测试 4：日志写入 ──"
LOG_DIR="$SUPPORT/Logs"
if [ -d "$LOG_DIR" ] && [ -n "$(ls -A "$LOG_DIR" 2>/dev/null)" ]; then
  ok "日志目录已创建"
  LAST="$(ls -t "$LOG_DIR"/*.log 2>/dev/null | head -1)"
  if grep -q "PCL-Y macOS launcher" "$LAST" 2>/dev/null; then
    ok "日志包含启动记录"
  else
    no "日志内容缺失"
  fi
else
  no "日志目录为空"
fi

# ── 测试 5：应用文件缺失时的报错 ────────────────────────────────────────────
echo
echo "── 测试 5：应用文件缺失 ──"
TMPAPP="$FAKE_HOME/Broken.app"
mkdir -p "$TMPAPP/Contents/MacOS" "$TMPAPP/Contents/Resources/scripts"
cp "$APP/Contents/MacOS/PCL-Y" "$TMPAPP/Contents/MacOS/PCL-Y"
chmod +x "$TMPAPP/Contents/MacOS/PCL-Y"

OUT="$(HOME="$FAKE_HOME" PCLY_WINE="$FAKE_HOME/bin/wine" PATH="$FAKE_HOME/bin:/usr/bin:/bin" \
       bash "$TMPAPP/Contents/MacOS/PCL-Y" 2>&1)"
if echo "$OUT" | grep -q "应用文件缺失"; then
  ok "检测到应用文件缺失"
else
  no "未检测到缺失（输出: $(echo "$OUT" | head -3)）"
fi

# ── 测试 6：Info.plist 合法性 ───────────────────────────────────────────────
echo
echo "── 测试 6：Info.plist ──"
PLIST="$APP/Contents/Info.plist"
if grep -q "CFBundleExecutable" "$PLIST" && grep -q "<string>PCL-Y</string>" "$PLIST"; then
  ok "CFBundleExecutable = PCL-Y"
else
  no "plist 缺少 CFBundleExecutable"
fi
EXEC_NAME="$(grep -A1 'CFBundleExecutable' "$PLIST" | grep -o '<string>[^<]*</string>' | sed 's/<[^>]*>//g')"
if [ -f "$APP/Contents/MacOS/$EXEC_NAME" ]; then
  ok "plist 指向的可执行文件存在"
else
  no "plist 指向 $EXEC_NAME 但文件不存在"
fi

# ── 测试 7：权限位 ──────────────────────────────────────────────────────────
echo
echo "── 测试 7：可执行权限 ──"
for f in "Contents/MacOS/PCL-Y" "Contents/Resources/scripts/setup.sh" "Contents/Resources/scripts/doctor.sh"; do
  if [ -x "$APP/$f" ]; then ok "$f 可执行"; else no "$f 不可执行"; fi
done

# ── 测试 8：换行符 ──────────────────────────────────────────────────────────
echo
echo "── 测试 8：LF 换行 ──"
for f in "Contents/MacOS/PCL-Y" "Contents/Resources/scripts/setup.sh"; do
  if grep -q $'\r' "$APP/$f" 2>/dev/null; then
    no "$f 含 CRLF"
  else
    ok "$f 纯 LF"
  fi
done

# ── 汇总 ────────────────────────────────────────────────────────────────────
echo
echo "════════════════════════════════════════"
printf "结果: ${G}%d 通过${N}, ${R}%d 失败${N}\n" "$PASS" "$FAIL"
rm -rf "$FAKE_HOME"
[ "$FAIL" -eq 0 ] || exit 1
