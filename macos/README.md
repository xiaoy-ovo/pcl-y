# macOS 构建目录

这个目录负责把 Windows 版 `pcl-y.exe` 封装成 macOS 的 `.app`，界面和功能与原版完全一致。

## 为什么需要它

原版是 **.NET Framework 4.8 + WPF** 程序：

```
pcl-y.exe.config    → supportedRuntime sku=".NETFramework,Version=v4.6.2"
PCLCS.dll           → TFM .NETFramework,Version=v4.8
MeloongCore.Wpf.dll → TFM .NETFramework,Version=v4.8
```

依赖 `PresentationFramework` / `PresentationCore` / `WindowsBase` / `System.Windows.Forms` /
`Microsoft.Win32.Registry` / `Microsoft.VisualBasic` —— 全部是 Windows 专属程序集。
macOS 上没有 .NET Framework 运行时，`net8.0-windows` 目标也无法在 macOS 上加载。

要**不改一行界面代码**地跑起来，只能用 Wine 提供 Win32 + WPF 兼容层。
本目录就是这层封装：`.app` 的启动器负责装好 wineprefix 和 .NET Framework 4.8，
然后用 Wine 执行原版 exe。

## 目录

```
macos/
├── build.sh            组装 .app（输入 vendor/<version>/，输出 dist/）
├── fetch-release.sh    从 GitHub Release 拉 Windows 发行包到 vendor/
├── post-install.sh     目标机上清 quarantine + 补权限 + ad-hoc 签名
├── zip_app.py          打包 zip 并写入 Unix 权限位（0755）
├── Info.plist          bundle 元信息模板
├── assets/
│   ├── appicon.icns    图标
│   └── appicon.png
├── launcher/
│   ├── launch.sh       → Contents/MacOS/PCL-Y（主启动器）
│   ├── setup.sh        首次运行：wineprefix + .NET 4.8 + 字体 + JRE
│   ├── doctor.sh       诊断
│   └── README.txt      用户说明（同时放进 .app）
├── tests/
│   ├── test-launch.sh  17 项启动器测试（假 wine）
│   └── test-setup.sh   16 项安装流程测试（假 wine/winetricks/curl）
└── vendor/<version>/   拉下来的原版文件（不入库）
```

## 构建

### 1. 拉原版

```bash
bash macos/fetch-release.sh 1.2.0
```

从 `https://github.com/xiaoy-ovo/pcl-y/releases/download/V1.2.0/pcl-y-1.2.0.zip`
下载并解压到 `macos/vendor/1.2.0/`。

也可以手动把 `pcl-y.exe` 等 6 个文件放进 `macos/vendor/1.2.0/`。

### 2. 组装

```bash
bash macos/build.sh --version 1.2.0
```

产物：

```
dist/PCL-Y.app
dist/PCL-Y-1.2.0-mac.zip        (约 3.2 MB)
dist/PCL-Y-1.2.0-mac.tar.gz     (约 3.1 MB)
```

在 macOS 上构建会自动做 ad-hoc 签名；在 Linux 上构建会跳过，由目标机的
`post-install.sh` 补上。

### 3. 测试

```bash
bash macos/tests/test-launch.sh   # 17 通过
bash macos/tests/test-setup.sh    # 16 通过
```

两个测试都不需要 macOS 或 Wine —— 用假 `wine` / `winetricks` / `curl` 验证
路径解析、环境变量、工作目录、日志、错误处理、安装步骤调用与幂等性。

## bundle 结构

```
PCL-Y.app/
└── Contents/
    ├── Info.plist                 CFBundleExecutable = PCL-Y
    ├── PkgInfo                    APPL????
    ├── 启动说明.txt
    ├── MacOS/
    │   └── PCL-Y                  bash 启动器（0755）
    └── Resources/
        ├── appicon.icns
        ├── app/
        │   ├── pcl-y.exe          原版主程序
        │   ├── pcl-y.exe.config
        │   ├── MeloongCore.dll
        │   ├── MeloongCore.Wpf.dll
        │   ├── PCLCS.dll
        │   └── Microsoft.Win32.Registry.dll
        └── scripts/
            ├── setup.sh           首次运行环境安装
            ├── doctor.sh          诊断
            └── README.txt
```

`Contents/MacOS` 里只放一个可执行文件（macOS 不允许子目录），程序文件全在
`Resources/app`。

## 启动流程

```
双击 PCL-Y.app
   ↓
Contents/MacOS/PCL-Y
   ↓
定位 Wine（PCLY_WINE → PATH → /Applications/Wine*.app → CrossOver → Homebrew）
   ↓
检查 ~/Library/Application Support/PCL-Y-Mac/wineprefix/.pcly-ready
   ↓
   不存在 → 弹窗 + 打开 Terminal 跑 scripts/setup.sh
   ↓              （wineprefix + .NET 4.8 + 字体 + Windows JRE）
   存在   → WINEPREFIX=... wine pcl-y.exe
   ↓
日志 tee 到 ~/Library/Application Support/PCL-Y-Mac/Logs/
```

## 关键技术点

| 点 | 说明 |
|---|---|
| `WINEDLLOVERRIDES="mscoree=d"` | **必须**。不禁用的话 Wine 用自带 wine-mono 跑 .NET，而 wine-mono 不支持 WPF，直接白屏 |
| `.NET Framework 4.8` | WPF 必需，`winetricks -q dotnet48` 或离线安装包 |
| 中文字体 | 把 macOS 的 PingFang/Heiti 软链进 prefix 并注册 `Wine\Fonts\Replacements`，否则界面全是方块 |
| Windows 版 JRE | macOS 自带 java 是 Mach-O，Wine 跑不了；`setup.sh` 会下 Temurin JRE 21 的 Windows 版 |
| 权限位 | Windows 打的 zip 不保存 Unix mode，`zip_app.py` 手写 `external_attr`，否则解压后不可执行 |
| CRLF | Windows 上构建时强制转 LF，否则 macOS 报 `bad interpreter: /bin/bash^M` |

## CI

`.github/workflows/macos.yml`：

1. **bundle**（ubuntu）：拉发行包 → 组装 → 跑两套测试 → 校验结构 → 上传 artifact
2. **verify-macos**（macos-14）：解压 artifact → 检查可执行位 → ad-hoc 签名 → 启动器干跑 → doctor
3. **publish**（可选）：`workflow_dispatch` 时把产物上传到对应 Release

手动触发：

```
Actions → Build macOS → Run workflow → version=1.2.0 → upload_release=true
```

## 已知限制

- **性能**：Wine 有开销，启动器首屏 3–8 秒
- **登录**：微软账号依赖 WebView2，Wine 下大概率打不开，建议离线模式
- **游戏性能**：Minecraft 也在 Wine 里跑，D3D→Metal 有损耗，集显约 30–60 FPS
- **高分屏**：界面可能略糊，Finder 里勾「使用低分辨率打开」可缓解
- **未公证**：需要用户 `xattr -cr`；正式分发需要 Apple Developer 证书 + notarytool
