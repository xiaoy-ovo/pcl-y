'===== 陶瓦联机（Terracotta）集成 =====
'联机功能由开源项目 Terracotta（陶瓦联机，AGPL-3.0）提供技术支持。
Public Module ModTerracotta

    '当前集成的 Terracotta 版本（随时可更新）
    Private Const TerracottaVersion As String = "0.4.2"
    '官方下载地址（gitee 镜像优先，GitHub 备用）
    Private Const TerracottaGiteeUrl As String = "https://gitee.com/burningtnt/Terracotta/releases/download/v0.4.2/terracotta-0.4.2-windows-x86_64-pkg.tar.gz"
    Private Const TerracottaGithubUrl As String = "https://github.com/burningtnt/Terracotta/releases/download/v0.4.2/terracotta-0.4.2-windows-x86_64-pkg.tar.gz"
    Private Const TerracottaExeName As String = "terracotta-0.4.2-windows-x86_64.exe"

    ''' <summary>
    ''' 陶瓦联机存放目录（PCL 根目录下 Terracotta 文件夹）。
    ''' </summary>
    Public ReadOnly Property TerracottaDir As String
        Get
            Return Paths.Base & "Terracotta\"
        End Get
    End Property

    ''' <summary>
    ''' 陶瓦联机主程序路径。
    ''' </summary>
    Public ReadOnly Property TerracottaExe As String
        Get
            Return TerracottaDir & TerracottaExeName
        End Get
    End Property

    ''' <summary>
    ''' 打开陶瓦联机；未安装时自动下载安装。
    ''' </summary>
    Public Sub OpenTerracotta()
        RunInThread(
        Sub()
            Try
                If Not File.Exists(TerracottaExe) Then
                    RunInUi(Sub() Hint("正在下载陶瓦联机组件，请稍候……"))
                    If Not DownloadTerracotta() Then Return
                End If
                If File.Exists(TerracottaExe) Then
                    RunInUi(Sub() Hint("正在启动陶瓦联机……"))
                    Process.Start(New ProcessStartInfo(TerracottaExe) With {.WorkingDirectory = TerracottaDir, .UseShellExecute = False})
                End If
            Catch ex As Exception
                Logger.Error(ex, "打开陶瓦联机失败")
                RunInUi(Sub() Hint("打开陶瓦联机失败：" & ex.Message, HintType.Red))
            End Try
        End Sub)
    End Sub

    ''' <summary>
    ''' 下载并解压陶瓦联机。成功返回 True。
    ''' </summary>
    Private Function DownloadTerracotta() As Boolean
        Dim TarPath As String = PathTemp & "terracotta-" & TerracottaVersion & ".tar.gz"
        Try
            If File.Exists(TarPath) Then File.Delete(TarPath)
            Try
                DownloadToFile(TerracottaGiteeUrl, TarPath)
            Catch ex As Exception
                Logger.Warn(ex, "从 gitee 下载陶瓦联机失败，尝试 GitHub")
                DownloadToFile(TerracottaGithubUrl, TarPath)
            End Try
            ExtractTarGz(TarPath, TerracottaDir)
            If Not File.Exists(TerracottaExe) Then Throw New Exception("解压后未找到主程序")
            Try
                File.Delete(TarPath)
            Catch
            End Try
            Return True
        Catch ex As Exception
            Logger.Error(ex, "下载陶瓦联机失败")
            RunInUi(Sub() Hint("下载陶瓦联机失败：" & ex.Message, HintType.Red))
            Return False
        End Try
    End Function

    ''' <summary>
    ''' 使用 HttpClient 直连下载文件到指定路径。
    ''' </summary>
    Private Sub DownloadToFile(Url As String, TargetPath As String)
        Using Handler As New HttpClientHandler()
            Handler.UseProxy = False '强制直连，避免系统代理导致的 403
            Using Client As New HttpClient(Handler)
                Client.Timeout = TimeSpan.FromSeconds(120)
                Client.DefaultRequestHeaders.UserAgent.ParseAdd($"pcl-y/{VersionBaseName} Mozilla/5.0")
                Client.DefaultRequestHeaders.Accept.ParseAdd("application/octet-stream")
                File.WriteAllBytes(TargetPath, Client.GetByteArrayAsync(Url).GetAwaiter().GetResult())
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' 解压 tar.gz 文件（内置实现，不依赖系统 tar）。
    ''' </summary>
    Private Sub ExtractTarGz(GzPath As String, DestDir As String)
        Dim TarData As Byte()
        Using Fs As New FileStream(GzPath, FileMode.Open, FileAccess.Read)
            Using Gz As New GZipStream(Fs, CompressionMode.Decompress)
                Using Ms As New MemoryStream
                    Gz.CopyTo(Ms)
                    TarData = Ms.ToArray()
                End Using
            End Using
        End Using
        Directory.CreateDirectory(DestDir)
        Dim Pos As Integer = 0
        Dim PendingName As String = Nothing
        While Pos + 512 <= TarData.Length
            '检查是否为全零填充块（tar 结尾）
            Dim IsZero As Boolean = True
            For i = Pos To Pos + 511
                If TarData(i) <> 0 Then
                    IsZero = False
                    Exit For
                End If
            Next
            If IsZero Then Exit While
            Dim Name As String = GetTarName(TarData, Pos)
            Dim TypeFlag As Byte = TarData(Pos + 156)
            Dim Size As Long = GetTarSize(TarData, Pos)
            If TypeFlag = 76 Then 'GNU longname：真实文件名在数据块中
                PendingName = TrimNull(Encoding.ASCII.GetString(TarData, Pos + 512, CInt(Size)))
                Pos += 512 + CInt((Size + 511) \ 512) * 512
                Continue While
            End If
            If PendingName IsNot Nothing Then
                Name = PendingName
                PendingName = Nothing
            End If
            If TypeFlag = 48 OrElse TypeFlag = 0 Then '普通文件
                Name = Name.TrimStart("/"c)
                If Name <> "" Then
                    Dim DataSize As Integer = If(Size > Integer.MaxValue, Integer.MaxValue, CInt(Size))
                    Dim Data As Byte() = New Byte(DataSize - 1) {}
                    Array.Copy(TarData, Pos + 512, Data, 0, DataSize)
                    File.WriteAllBytes(Path.Combine(DestDir, Name), Data)
                End If
            End If
            Pos += 512 + CInt((Size + 511) \ 512) * 512
        End While
    End Sub

    Private Function GetTarName(TarData As Byte(), Pos As Integer) As String
        Dim Raw As Byte() = New Byte(99) {}
        Array.Copy(TarData, Pos, Raw, 0, 100)
        Return TrimNull(Encoding.ASCII.GetString(Raw))
    End Function

    Private Function GetTarSize(TarData As Byte(), Pos As Integer) As Long
        Dim Raw As Byte() = New Byte(11) {}
        Array.Copy(TarData, Pos + 124, Raw, 0, 12)
        Dim Text As String = TrimNull(Encoding.ASCII.GetString(Raw)).Trim()
        If Text = "" Then Return 0
        Return Convert.ToInt64(Text.TrimEnd(Chr(0), " "c), 8)
    End Function

    Private Function TrimNull(Text As String) As String
        Dim Index As Integer = Text.IndexOf(Chr(0))
        If Index >= 0 Then Return Text.Substring(0, Index)
        Return Text
    End Function

End Module