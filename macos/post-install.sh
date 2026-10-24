#!/bin/bash
# ============================================================================
# 在 macOS 上首次解压后执行：清隔离属性 + 补权限 + ad-hoc 签名
# 用法： bash post-install.sh "PCL-Y.app"
# ============================================================================
set -uo pipefail

APP="${1:-PCL-Y.app}"
[ -d "$APP" ] || { echo "找不到 $APP"; exit 1; }

echo "==> 清除 com.apple.quarantine"
xattr -cr "$APP" 2>/dev/null || true

echo "==> 设置可执行权限"
chmod +x "$APP/Contents/MacOS/"* 2>/dev/null || true
find "$APP/Contents/Resources/scripts" -name '*.sh' -exec chmod +x {} \; 2>/dev/null || true

echo "==> 修正换行符（防止从 Windows 打包带回 CRLF）"
find "$APP/Contents/MacOS" "$APP/Contents/Resources/scripts" -type f -exec perl -pi -e 's/\r\n/\n/g' {} \; 2>/dev/null || true

echo "==> ad-hoc 代码签名"
if command -v codesign >/dev/null 2>&1; then
  codesign --force --deep --sign - "$APP" && echo "    签名成功"
  codesign -dv "$APP" 2>&1 | head -4 | sed 's/^/    /'
else
  echo "    未找到 codesign（xcode-select --install 可安装）"
fi

echo
echo "==> 下一步"
echo "    1) 双击 PCL-Y.app"
echo "    2) 首次运行会提示安装运行环境，点确定后跟随终端提示（5–15 分钟）"
echo "    3) 装完后重新双击 PCL-Y.app"
echo
echo "    出问题时："
echo "      bash \"$APP/Contents/Resources/scripts/doctor.sh\""
