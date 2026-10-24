#!/bin/bash
# ============================================================================
# setup.sh 的离线流程测试：用假 wine / 假 winetricks / 假 curl 跑通全流程
# 验证每一步都按预期被调用，且最终写出 .pcly-ready 标记。
# ============================================================================
set -uo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SETUP="$ROOT/launcher/setup.sh"

PASS=0; FAIL=0
G='\033[32m'; R='\033[31m'; N='\033[0m'
ok()  { PASS=$((PASS+1)); printf "${G}  PASS${N}  %s\n" "$*"; }
no()  { FAIL=$((FAIL+1)); printf "${R}  FAIL${N}  %s\n" "$*"; }

echo "════ setup.sh 离线流程测试 ════"

FAKE_HOME="$(mktemp -d)"
BIN="$FAKE_HOME/bin"
mkdir -p "$BIN" "$FAKE_HOME/Library/Application Support"

export STUB_LOG="$FAKE_HOME/calls.log"
: > "$STUB_LOG"

# 假 wine：记录所有调用，reg query 第一次返回失败（模拟未装 .NET）
cat > "$BIN/wine" <<'STUB'
#!/bin/bash
echo "wine $*" >> "$STUB_LOG"
case "$1" in
  --version)  echo "wine-9.0 (Stub)"; exit 0 ;;
  reg)
    case "$2" in
      query)
        # 让 .NET 检测先失败，安装后再成功
        if [ -f "$STUB_HOME/dotnet-installed" ]; then
          echo "    Release    REG_SZ    533320"
          exit 0
        fi
        exit 1 ;;
      add) exit 0 ;;
    esac ;;
  wineserver) exit 0 ;;
  wineboot)   exit 0 ;;
esac
exit 0
STUB
chmod +x "$BIN/wine"

# 假 winetricks：dotnet48 时写入标记文件
cat > "$BIN/winetricks" <<'STUB'
#!/bin/bash
echo "winetricks $*" >> "$STUB_LOG"
for a in "$@"; do
  case "$a" in
    dotnet48) touch "$STUB_HOME/dotnet-installed" ;;
  esac
done
exit 0
STUB
chmod +x "$BIN/winetricks"

# 假 curl / unzip：避免真的下载
cat > "$BIN/curl" <<'STUB'
#!/bin/bash
echo "curl $*" >> "$STUB_LOG"
out=""
prev=""
for a in "$@"; do
  [ "$prev" = "-o" ] && out="$a"
  prev="$a"
done
[ -n "$out" ] && echo "stub" > "$out"
exit 0
STUB
chmod +x "$BIN/curl"

cat > "$BIN/unzip" <<'STUB'
#!/bin/bash
echo "unzip $*" >> "$STUB_LOG"
dest=""
prev=""
for a in "$@"; do
  [ "$prev" = "-d" ] && dest="$a"
  prev="$a"
done
if [ -n "$dest" ]; then
  mkdir -p "$dest/jdk-21.0.1+12-jre"
  mkdir -p "$dest/jdk-21.0.1+12-jre/bin"
  echo stub > "$dest/jdk-21.0.1+12-jre/bin/java.exe"
fi
exit 0
STUB
chmod +x "$BIN/unzip"

# ── 跑 setup.sh ─────────────────────────────────────────────────────────────
echo
echo "── 执行 setup.sh ──"

OUT="$(
  HOME="$FAKE_HOME" \
  PCLY_WINE="$BIN/wine" \
  STUB_HOME="$FAKE_HOME" \
  PATH="$BIN:/usr/bin:/bin:/usr/sbin:/sbin" \
  bash "$SETUP" 2>&1
)" 
CODE=$?

echo "$OUT" | grep -E "==>|\[!\]|\[x\]" | head -25 | sed 's/^/    /'
echo

# ── 断言 ────────────────────────────────────────────────────────────────────
if [ "$CODE" -eq 0 ]; then ok "setup.sh 退出码 0"; else no "setup.sh 退出码 $CODE"; fi

SUPPORT="$FAKE_HOME/Library/Application Support/PCL-Y-Mac"
PREFIX="$SUPPORT/wineprefix"

if [ -f "$PREFIX/.pcly-ready" ]; then
  ok ".pcly-ready 标记已写入"
  grep -q "wine=" "$PREFIX/.pcly-ready" && ok "标记内含 wine 路径" || no "标记内容不完整"
else
  no ".pcly-ready 未写入"
fi

if grep -q "winetricks -q dotnet48" "$STUB_LOG"; then
  ok "调用了 winetricks dotnet48"
else
  no "未调用 dotnet48（日志: $(grep winetricks "$STUB_LOG" | head -3)）"
fi

if grep -q "winetricks -q corefonts" "$STUB_LOG"; then
  ok "调用了 corefonts"
else
  no "未调用 corefonts"
fi

if grep -q "winetricks -q cjkfonts" "$STUB_LOG"; then
  ok "调用了 cjkfonts"
else
  no "未调用 cjkfonts"
fi

for pkg in gdiplus riched20 msxml6 vcrun2019; do
  if grep -q "winetricks -q $pkg" "$STUB_LOG"; then
    ok "调用了 $pkg"
  else
    no "未调用 $pkg"
  fi
done

if grep -q 'reg add HKCU\\Software\\Wine\\Fonts\\Replacements' "$STUB_LOG"; then
  ok "写入了字体替换注册表"
else
  no "未写字体替换"
fi

if grep -q 'reg add HKCU\\Software\\Wine\\Direct3D' "$STUB_LOG"; then
  ok "写入了 Direct3D 设置"
else
  no "未写 Direct3D 设置"
fi

if grep -q "curl" "$STUB_LOG" && grep -q "adoptium" "$STUB_LOG"; then
  ok "下载了 Windows JRE"
else
  no "未下载 JRE（日志: $(grep curl "$STUB_LOG" | head -2)）"
fi

JRE_ROOT="$PREFIX/drive_c/Program Files/Eclipse Adoptium"
if [ -d "$JRE_ROOT" ] && [ -n "$(ls -A "$JRE_ROOT" 2>/dev/null)" ]; then
  ok "JRE 已解压到 prefix"
else
  no "JRE 未安装（$JRE_ROOT）"
fi

# 日志
if [ -d "$SUPPORT/Logs" ] && [ -n "$(ls -A "$SUPPORT/Logs" 2>/dev/null)" ]; then
  ok "setup 日志已生成"
else
  no "setup 日志缺失"
fi

# 二次执行应当跳过 dotnet48
echo
echo "── 二次执行（应跳过已安装组件）──"
: > "$STUB_LOG"
HOME="$FAKE_HOME" PCLY_WINE="$BIN/wine" STUB_HOME="$FAKE_HOME" \
  PATH="$BIN:/usr/bin:/bin" bash "$SETUP" >/dev/null 2>&1

if grep -q "dotnet48" "$STUB_LOG"; then
  no "二次执行仍重装了 dotnet48"
else
  ok "二次执行跳过 dotnet48"
fi

echo
echo "════════════════════════════════════════"
printf "结果: ${G}%d 通过${N}, ${R}%d 失败${N}\n" "$PASS" "$FAIL"
rm -rf "$FAKE_HOME"
[ "$FAIL" -eq 0 ] || exit 1
