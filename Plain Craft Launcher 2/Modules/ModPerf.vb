Public Module ModPerf

    ''' <summary>
    ''' 释放当前进程的工作集（物理内存），降低启动器内存占用。
    ''' </summary>
    Public Sub ReleaseMemory()
        Try
            Using P As Process = Process.GetCurrentProcess()
                ReleaseEmptyWorkingSet(P.Handle)
            End Using
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>
    ''' 检测是否存在 Intel 核显。
    ''' </summary>
    Public Function HasIntelGpu() As Boolean
        Try
            Using Key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}")
                If Key Is Nothing Then Return False
                For Each SubName In Key.GetSubKeyNames()
                    Using SubKey = Key.OpenSubKey(SubName)
                        If SubKey IsNot Nothing Then
                            Dim Desc = SubKey.GetValue("DriverDesc")?.ToString
                            If Desc IsNot Nothing AndAlso Desc.IndexOf("Intel", StringComparison.OrdinalIgnoreCase) >= 0 Then Return True
                        End If
                    End Using
                Next
            End Using
        Catch ex As Exception
        End Try
        Return False
    End Function

    ''' <summary>
    ''' 启动后台内存回收循环（仅在启用极致性能释放时生效）。
    ''' </summary>
    Public Sub StartMemoryReleaser()
        RunInNewThread(
            Sub()
                While True
                    Thread.Sleep(30000)
                    If Settings.Get(Of Boolean)("PerformanceUltimate") Then ReleaseMemory()
                End While
            End Sub, "PerfMemoryReleaser", ThreadPriority.BelowNormal)
    End Sub

    <Runtime.InteropServices.DllImport("kernel32.dll")>
    Private Function ReleaseEmptyWorkingSet(ProcessHandle As IntPtr) As Boolean
    End Function

End Module
