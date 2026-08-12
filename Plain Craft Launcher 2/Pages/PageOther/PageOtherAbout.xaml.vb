Public Class PageOtherAbout

    Private Shadows IsLoaded As Boolean = False
    Private Sub PageOtherAbout_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        '重复加载部分
        PanBack.ScrollToHome()

        '非重复加载部分
        If IsLoaded Then Return
        IsLoaded = True

        ItemAboutPcl.Text = "BY XIAOY_OVO"
        LabAboutDesc.Text = "pcl-y " & VersionDisplay

    End Sub

    Private Sub BtnAboutUpdate_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnAboutUpdate.Click
        UpdateCheckByButton()
    End Sub

    Public Shared Sub CopyIdentify()
        ClipboardSet(Identify)
    End Sub

End Class
