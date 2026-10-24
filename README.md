# Plain Craft Launcher

[![Stars](https://img.shields.io/github/stars/Meloong-Git/PCL?style=flat&logo=data:image/svg%2bxml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZlcnNpb249IjEiIHdpZHRoPSIxNiIgaGVpZ2h0PSIxNiI+PHBhdGggZD0iTTggLjI1YS43NS43NSAwIDAgMSAuNjczLjQxOGwxLjg4MiAzLjgxNSA0LjIxLjYxMmEuNzUuNzUgMCAwIDEgLjQxNiAxLjI3OWwtMy4wNDYgMi45Ny43MTkgNC4xOTJhLjc1MS43NTEgMCAwIDEtMS4wODguNzkxTDggMTIuMzQ3bC0zLjc2NiAxLjk4YS43NS43NSAwIDAgMS0xLjA4OC0uNzlsLjcyLTQuMTk0TC44MTggNi4zNzRhLjc1Ljc1IDAgMCAxIC40MTYtMS4yOGw0LjIxLS42MTFMNy4zMjcuNjY4QS43NS43NSAwIDAgMSA4IC4yNVoiIGZpbGw9IiNlYWM1NGYiLz48L3N2Zz4=&logoSize=auto&label=Stars&labelColor=444444&color=eac54f)](https://github.com/Meloong-Git/PCL/)
[![Issues](https://img.shields.io/github/issues/Meloong-Git/PCL?style=flat&label=Issues&labelColor=444444&color=1F883D)](https://github.com/Meloong-Git/PCL/issues)
[![哔哩哔哩](https://img.shields.io/badge/动态-BiliBili-00A4DB?style=flat&labelColor=444444&logoSize=auto)](https://space.bilibili.com/11343203/dynamic)
[![爱发电](https://img.shields.io/badge/赞助-爱发电-946ce6?style=flat&labelColor=444444&logoSize=auto)](https://meloong.com/afd/a/LTCat)

包含 PCL 的大部分源代码，包括 UI 库、动画模块、下载模块、Minecraft 启动模块等！<br/>
代码并不是实时更新的，它会在每次 PCL 发布更新时同步一次。<br/>
非常感谢社区的大力支持！

<br/>

### 相关资源
- [PCL 下载](https://meloong.com/pcl)：下载可免费使用的正式版 PCL！
- [PCL 功能投票](https://github.com/Meloong-Git/PCL/discussions/2)：来参加投票吧，开发者会优先处理票数较高的项目！
- [帮助文档库](https://github.com/LTCatt/PCL2Help)：PCL 内置帮助文档的存储库

<br/>

---

## macOS 版

本仓库提供 pcl-y 的 macOS 封装版：把 Windows 版 `pcl-y.exe` 打包成 `.app`，
**界面与功能与原版完全一致**，不需要重新实现 UI。

### 下载

到 [Releases](https://github.com/xiaoy-ovo/pcl-y/releases) 下载 `PCL-Y-<版本>-mac.zip`。

### 安装

1. 解压，把 `PCL-Y.app` 拖进 `/Applications`
2. 清除隔离属性（否则会提示"已损坏"）：

   ```bash
   xattr -cr "/Applications/PCL-Y.app"
   ```

3. 先装 Wine：

   ```bash
   brew install --cask --no-quarantine wine-stable
   brew install winetricks
   ```

4. 双击 `PCL-Y.app`。首次运行会打开终端，自动安装运行环境
   （wineprefix、.NET Framework 4.8、中文字体、Windows 版 JRE），约 5–15 分钟。
5. 装完再双击一次即可正常启动。

### 为什么需要 Wine

原版是 **.NET Framework 4.8 + WPF** 程序，依赖 `PresentationFramework`、
`System.Windows.Forms`、`Microsoft.Win32.Registry` 等 Windows 专属程序集。
macOS 没有 .NET Framework 运行时，`net8.0-windows` 目标也无法在 macOS 上加载。
Wine 提供 Win32 + WPF 兼容层，是目前唯一能保持原界面的方案。

### 构建

```bash
bash macos/fetch-release.sh 1.2.0    # 拉取原版发行包到 macos/vendor/
bash macos/build.sh --version 1.2.0  # 组装 dist/PCL-Y.app
bash macos/tests/test-launch.sh      # 17 项启动器测试
bash macos/tests/test-setup.sh       # 16 项安装流程测试
```

详见 [macos/README.md](macos/README.md)。CI 见 `.github/workflows/macos.yml`，
可在 Actions 里手动触发构建并上传到 Release。

### 已知限制

- Wine 有额外开销，启动器首屏约 3–8 秒
- 微软账号登录依赖 WebView2，Wine 下大概率不可用，建议离线模式
- Minecraft 同样运行在 Wine 中，图形性能取决于 D3D→Metal 转换效率
- 未做 Apple 公证，需要用户手动 `xattr -cr`
