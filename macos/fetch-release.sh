#!/bin/bash
# ============================================================================
# 从 GitHub Release 拉取 pcl-y 的 Windows 发行包，解压到 macos/vendor/<version>
#
# 用法：
#   bash macos/fetch-release.sh              # 取 1.2.0
#   bash macos/fetch-release.sh 1.1.0        # 取指定版本
#   bash macos/fetch-release.sh 1.2.0 ./dist # 指定输出目录
# ============================================================================
set -euo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
REPO_OWNER="${PCLY_REPO_OWNER:-xiaoy-ovo}"
REPO_NAME="${PCLY_REPO_NAME:-pcl-y}"

VERSION="${1:-1.2.0}"
OUT="${2:-$HERE/vendor/$VERSION}"

cyan() { printf '\033[36m==>\033[0m %s\n' "$*"; }
gray() { printf '    %s\n' "$*"; }
die()  { printf '\033[31m[x]\033[0m %s\n' "$*" >&2; exit 1; }

# 版本号 → tag：1.2.0 对应 V1.2.0
TAG="V$VERSION"
ZIP_NAME="pcl-y-$VERSION.zip"
URL="https://github.com/$REPO_OWNER/$REPO_NAME/releases/download/$TAG/$ZIP_NAME"

cyan "拉取 $TAG / $ZIP_NAME"
gray "$URL"

mkdir -p "$(dirname "$OUT")"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

if command -v gh >/dev/null 2>&1 && gh auth status >/dev/null 2>&1; then
  cyan "用 gh 下载（支持私有仓库）"
  gh release download "$TAG" -R "$REPO_OWNER/$REPO_NAME" -p "$ZIP_NAME" -D "$TMP" --clobber \
    || die "gh release download 失败"
else
  cyan "用 curl 下载"
  curl -fL --retry 3 --progress-bar -o "$TMP/$ZIP_NAME" "$URL" \
    || die "下载失败：$URL"
fi

[ -s "$TMP/$ZIP_NAME" ] || die "下载的文件为空"

cyan "校验"
SIZE=$(du -h "$TMP/$ZIP_NAME" | cut -f1)
gray "$ZIP_NAME ($SIZE)"
if command -v shasum >/dev/null 2>&1; then
  gray "sha256: $(shasum -a 256 "$TMP/$ZIP_NAME" | cut -d' ' -f1)"
fi

cyan "解压到 $OUT"
rm -rf "$OUT"
mkdir -p "$OUT"

case "$ZIP_NAME" in
  *.zip) unzip -q -o "$TMP/$ZIP_NAME" -d "$OUT" ;;
  *) die "未知的压缩格式: $ZIP_NAME" ;;
esac

# 有些包会多一层目录，拍平
if [ ! -f "$OUT/pcl-y.exe" ]; then
  INNER=$(find "$OUT" -maxdepth 2 -name 'pcl-y.exe' | head -1)
  if [ -n "$INNER" ]; then
    cyan "拍平嵌套目录"
    mv "$(dirname "$INNER")"/* "$OUT"/ 2>/dev/null || true
  fi
fi

[ -f "$OUT/pcl-y.exe" ] || die "解压后没有 pcl-y.exe，检查包结构"

cyan "内容"
ls -la "$OUT" | sed 's/^/    /'

echo
echo "完成: $OUT"
echo "下一步: bash macos/build.sh --version $VERSION"
