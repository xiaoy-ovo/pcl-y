'===== 一键性能优化 =====
Public Module ModOptimize

    ''' <summary>
    ''' 一键性能优化：内存自动分配 + 显卡高性能 + 安装性能优化模组。
    ''' </summary>
    Public Sub OneKeyOptimize()
        RunInThread(
        Sub()
            Try
                Dim Log As New System.Text.StringBuilder
                '1. 内存优化：自动分配
                Settings.Set("LaunchRamType", 0)
                Log.AppendLine("已开启内存自动分配。")

                '2. 显卡优化：强制高性能
                Try
                    SetGPUPreference(PathExe)
                    Log.AppendLine("已开启显卡高性能（核显/独显优化）。")
                Catch ex As Exception
                    Log.AppendLine("显卡优化失败：" & ex.Message)
                End Try

                '3. 安装性能优化模组
                Log.Append(InstallOptimizeMods())

                Dim Result As String = Log.ToString()
                RunInUi(Sub() MyMsgBox(Result, "一键性能优化完成"))
            Catch ex As Exception
                Logger.Error(ex, "一键性能优化失败")
                RunInUi(Sub() Hint("一键性能优化失败：" & ex.Message, HintType.Red))
            End Try
        End Sub)
    End Sub

    ''' <summary>
    ''' 为当前选择的版本安装性能优化模组（Sodium / Lithium / Phosphor）。
    ''' 返回执行说明文本。
    ''' </summary>
    Private Function InstallOptimizeMods() As String
        Dim Instance As McInstance = McInstanceSelected
        If Instance Is Nothing Then
            Return "未选择游戏版本，跳过模组安装。"
        End If
        Dim VanillaName As String = Instance.Version.VanillaName
        Dim LoaderFlags As ModLoaders = Instance.Version.ModLoaders
        If String.IsNullOrEmpty(VanillaName) Then
            Return "当前版本信息不完整，跳过模组安装。"
        End If
        'Sodium 仅支持 Fabric / NeoForge
        If Not LoaderFlags.Flags.Intersect({ModLoaders.Fabric, ModLoaders.NeoForge}).Any() Then
            Return $"当前版本为 {VanillaName}（{If(LoaderFlags = ModLoaders.None, "原版", LoaderFlags.ToString)}），Sodium 系列模组仅支持 Fabric / NeoForge，跳过模组安装。"
        End If
        '目标 mods 文件夹
        Dim ModsFolder As String = Instance.PathIndie & "mods\"
        DirectoryUtils.Create(ModsFolder)
        '模组 ID 与显示名
        Dim Targets As New List(Of (Id As String, Name As String)) From {
            (Id:="sodium", Name:="Sodium（钠）"),
            (Id:="lithium", Name:="Lithium（锂）"),
            (Id:="phosphor", Name:="Phosphor（磷）")}
        '收集匹配当前版本与加载器的下载
        Dim Files As New List(Of NetFile)
        Dim Skipped As New List(Of String)
        For Each Target In Targets
            Try
                Dim Versions As List(Of ResourceVersion) = ResourceVersion.FromProjectId(Target.Id, ResourcePlatforms.Modrinth, LoadDependencies:=True)
                Dim Matches As List(Of ResourceVersion) = Versions.Where(Function(v) v.GameVersions.Contains(VanillaName) AndAlso
                    v.ModLoaders.Flags.Intersect(LoaderFlags.Flags).Any()).ToList()
                Dim Match As ResourceVersion = Matches.FirstOrDefault(Function(v) v.ReleaseType = ResourceVersion.ReleaseTypes.Release)
                If Match Is Nothing Then Match = Matches.FirstOrDefault()
                If Match Is Nothing Then
                    Skipped.Add($"{Target.Name}（无 {VanillaName} 适配版本）")
                    Continue For
                End If
                Files.Add(Match.ToNetFile(ModsFolder, ResourceVersion.DownloadReason.Standalone, VanillaName, Match.ModLoaders))
            Catch ex As Exception
                Logger.Warn(ex, $"获取模组 {Target.Name} 版本信息失败")
                Skipped.Add($"{Target.Name}（获取失败：{ex.Message}）")
            End Try
        Next
        If Not Files.Any() Then
            Return "未找到可下载的优化模组。" & If(Skipped.Any, " " & Skipped.Join("，"), "")
        End If
        '下载并显示进度
        Dim TaskList As New List(Of LoaderBase)
        TaskList.Add(New LoaderDownload("下载性能优化模组", Files) With {.Block = True, .ProgressWeight = 1})
        Dim Combo As New LoaderCombo(Of String)("性能优化模组安装", TaskList) With {.OnStateChanged =
        Sub(MyLoader)
            Select Case MyLoader.State
                Case LoadState.Failed
                    RunInUi(Sub() Hint(MyLoader.Name & "失败：" & MyLoader.Error.GetDisplay(False), HintType.Red))
                Case LoadState.Canceled
                    RunInUi(Sub() Hint(MyLoader.Name & "已取消！", HintType.Blue))
            End Select
        End Sub}
        Combo.Start(ModsFolder)
        LoaderTaskbarAdd(Combo)
        Return $"正在安装性能优化模组：{Files.Count} 个。{If(Skipped.Any, vbCrLf & "跳过：" & Skipped.Join("；"), "")}"
    End Function

End Module