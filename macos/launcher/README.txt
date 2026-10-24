PCL-Y macOS 版使用说明
==================================================

本版本是原版 pcl-y 1.2.0 的 macOS 封装。界面与功能与原版完全一致，
通过 Wine 运行原版 pcl-y.exe（.NET Framework 4.8 + WPF）。

为什么必须用 Wine
--------------------------------------------------
原版是 Windows 专属的 WPF 程序，依赖 PresentationFramework /
System.Windows.* / Microsoft.Win32.Registry，macOS 没有对应运行时，
.NET 官方也不提供 macOS 上的 .NET Framework 4.8。
Wine 提供了 Win32/WPF 所需的兼容层，是目前唯一能"界面不变"跑起来的方式。


首次使用
--------------------------------------------------
1. 安装 Wine（二选一）

   Homebrew（推荐）：
       brew install --cask --no-quarantine wine-stable
       brew install winetricks

   CrossOver（付费，兼容性最好）：
       https://www.codeweavers.com/crossover

2. 双击 PCL-Y.app
   首次运行会弹出提示，确认后会打开终端窗口自动安装：
       - 64 位 wineprefix
       - .NET Framework 4.8（WPF 必需，最慢的一步）
       - 中文字体
       - Windows 版 JRE（PCL 启动游戏要用）

   这一步大约 5–15 分钟，取决于网速。

3. 安装完成后，重新双击 PCL-Y.app 即可。


为什么还要装 Windows 版 JRE
--------------------------------------------------
PCL 会调用 java.exe 启动 Minecraft。macOS 自带的 java 是 Mach-O 格式，
Wine 无法执行。所以安装脚本会在 wineprefix 里装一份 Windows 版
Eclipse Temurin JRE，PCL 在「设置 → Java」里能自动识别。

如果你已经有 Windows 版 JDK，也可以在 PCL 里手动指定路径
（形如 C:\Program Files\Java\jdk-17\bin\java.exe）。


已知限制
--------------------------------------------------
- 启动器本体走 Wine，比原生慢，首屏大约 3–8 秒
- 高分屏下界面可能略糊：Finder 里右键 PCL-Y.app → 显示简介 →
  勾选「使用低分辨率打开」可以缓解（或取消勾选，视显示器而定）
- 游戏本体不经过 Wine：PCL 用 Windows JRE 启动 Minecraft 时，
  Minecraft 也是 Windows 版 Java 程序，同样在 Wine 里跑。
  图形性能取决于 Wine 的 D3D→Metal 转换，集显机器可能只有 30–60 FPS
- 微软账号登录依赖 WebView2，Wine 下大概率打不开，建议用离线模式


故障排查
--------------------------------------------------
先跑诊断：

    bash "PCL-Y.app/Contents/Resources/scripts/doctor.sh"

带 Wine 详细输出启动：

    "PCL-Y.app/Contents/MacOS/PCL-Y" --debug

日志目录：

    ~/Library/Application Support/PCL-Y-Mac/Logs/

常见问题：

  双击没反应
      → 终端执行 xattr -cr "PCL-Y.app"

  提示「无法打开，因为无法验证开发者」
      → 系统设置 → 隐私与安全性 → 仍要打开

  白屏 / 闪退
      → 多半是 .NET Framework 没装成功，重跑 setup.sh：
        bash "PCL-Y.app/Contents/Resources/scripts/setup.sh"

  中文显示方块
      → 字体没链进去，重跑 setup.sh 的字体步骤，
        或 brew install --cask font-noto-sans-cjk

  PCL 里找不到 Java
      → 重跑 setup.sh；或手动指定 C:\Program Files\Java\...\bin\java.exe

  游戏启动崩溃
      → 与 Wine 的 OpenGL 有关，尝试设置环境变量：
        export LIBGL_ALWAYS_SOFTWARE=1
      然后从终端启动 PCL-Y
