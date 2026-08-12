'由于包含加解密等安全信息，本文件中的部分代码已被删除

Imports System.Net.Http
Imports System.Windows.Shapes

Friend Module ModSecret

    '标注 PCL 的不同分支，仅用于替换标记
    Public Const VersionBranchMain As String = "OpenSource"
    '在开源版的注册表与常规版的注册表隔离，以防数据冲突
    Public Const RegFolder As String = "PCLDebug"
    '用于微软登录的 ClientId
    Public OAuthClientId As String = If(Environment.GetEnvironmentVariable("PCL_MS_CLIENT_ID"), "")
    'CurseForge API Key
    Public CurseForgeAPIKey As String = If(Environment.GetEnvironmentVariable("PCL_CURSEFORGE_API_KEY"), "")
    '用于匿名数据收集的腾讯云日志服务上报 URL，形如 https://{region}.cls.tencentcs.com/track?topic_id={topic_id}
    Public Const ClsBaseUrl As String = ""

#Region "网络鉴权"

    Friend Function SecretCdnSign(UrlWithMark As String) As String
        If Not UrlWithMark.EndsWithF("{CDN}") Then Return UrlWithMark
        Return UrlWithMark.Replace("{CDN}", "").Replace(" ", "%20")
    End Function
    ''' <summary>
    ''' 设置 Headers 的 UA、Referer。
    ''' </summary>
    Friend Sub SecretHeadersSign(Url As String, ByRef Req As HttpRequestMessage, Optional SimulateBrowserHeaders As Boolean = False)
        If Not Req.Headers.UserAgent.Any Then
            If Url.Contains("baidupcs.com") OrElse Url.Contains("baidu.com") Then
                Req.Headers.Add("User-Agent", "LogStatistic")  '#4951
            ElseIf SimulateBrowserHeaders Then
                Req.Headers.Add("User-Agent", $"pcl-y/{VersionBaseName}.{CInt(BuildType)} Mozilla/5.0 AppleWebKit/537.36 Chrome/63.0.3239.132 Safari/537.36")
            Else
                Req.Headers.Add("User-Agent", $"pcl-y/{VersionBaseName}.{CInt(BuildType)}")
            End If
        End If
        If Not SimulateBrowserHeaders Then Req.Headers.Add("Referer", $"http://{VersionCode}.open.pcl2.server/")
        If Url.Contains("api.curseforge.com") OrElse Url.Contains("forgecdn.net") Then Req.Headers.Add("x-api-key", CurseForgeAPIKey)
    End Sub

#End Region

#Region "主题"

    ''' <summary>
    ''' 内置主题预设。
    ''' Style：整体 UI 风格模板——
    ''' 0  经典（横渐变顶栏 + 对角主背景）
    ''' 8  液态玻璃（玻璃高光顶栏 + 深色流动光带）
    ''' 9  毛玻璃（磨砂浅色漫射光）
    ''' 10 大海（深海渐变 + 波浪微光）
    ''' 11 夜空（深蓝黑 + 星光）
    ''' 12 重生（暗底升起的新生之光）
    ''' 13 黑客（黑绿矩阵风格）
    ''' 14 天空（蓝天白云）
    ''' 15 信仰（金色圣光）
    ''' </summary>
    Public Structure ThemePreset
        Public Name As String
        Public Hue As Integer
        Public Sat As Integer
        Public LightAdjust As Integer
        Public TopbarDelta As Integer
        Public Style As Integer
    End Structure
    Public ReadOnly ThemePresets As ThemePreset() = {
        New ThemePreset With {.Name = "经典", .Hue = 210, .Sat = 85, .LightAdjust = 0, .TopbarDelta = 0, .Style = 0},
        New ThemePreset With {.Name = "液态玻璃", .Hue = 200, .Sat = 35, .LightAdjust = 0, .TopbarDelta = 0, .Style = 8},
        New ThemePreset With {.Name = "毛玻璃", .Hue = 200, .Sat = 45, .LightAdjust = 10, .TopbarDelta = 0, .Style = 9},
        New ThemePreset With {.Name = "大海", .Hue = 210, .Sat = 70, .LightAdjust = -12, .TopbarDelta = 0, .Style = 10},
        New ThemePreset With {.Name = "夜空", .Hue = 225, .Sat = 60, .LightAdjust = -16, .TopbarDelta = 0, .Style = 11},
        New ThemePreset With {.Name = "重生", .Hue = 30, .Sat = 80, .LightAdjust = -8, .TopbarDelta = 0, .Style = 12},
        New ThemePreset With {.Name = "黑客", .Hue = 130, .Sat = 80, .LightAdjust = -18, .TopbarDelta = 0, .Style = 13},
        New ThemePreset With {.Name = "信仰", .Hue = 40, .Sat = 85, .LightAdjust = 0, .TopbarDelta = 0, .Style = 15},
        New ThemePreset With {.Name = "配色方案", .Hue = -1, .Sat = 0, .LightAdjust = 0, .TopbarDelta = 0, .Style = 0}}

    Public Color1 As New MyColor(52, 61, 74)
    Public Color2 As New MyColor(11, 91, 203)
    Public Color3 As New MyColor(19, 112, 243)
    Public Color4 As New MyColor(72, 144, 245)
    Public Color5 As New MyColor(150, 192, 249)
    Public Color6 As New MyColor(213, 230, 253)
    Public Color7 As New MyColor(222, 236, 253)
    Public Color8 As New MyColor(234, 242, 254)
    Public ColorBg0 As New MyColor(150, 192, 249)
    Public ColorBg1 As New MyColor(190, Color7)
    Public ColorGray1 As New MyColor(64, 64, 64)
    Public ColorGray2 As New MyColor(115, 115, 115)
    Public ColorGray3 As New MyColor(140, 140, 140)
    Public ColorGray4 As New MyColor(166, 166, 166)
    Public ColorGray5 As New MyColor(204, 204, 204)
    Public ColorGray6 As New MyColor(235, 235, 235)
    Public ColorGray7 As New MyColor(240, 240, 240)
    Public ColorGray8 As New MyColor(245, 245, 245)
    Public ColorSemiTransparent As New MyColor(1, Color8)

    Public ThemeNow As Integer = -1
    Public ColorHue As Integer = 210, ColorSat As Integer = 85, ColorLightAdjust As Integer = 0, ColorHueTopbarDelta As OneOf(Of Integer, Integer()) = 0
    Public ThemeStyle As Integer = 0
    Public ThemeDontClick As Integer = 0

    Public Sub ThemeRefresh(Optional NewTheme As Integer = -1)
        Try
            If ThemeNow = NewTheme AndAlso NewTheme >= 0 Then Return
            If NewTheme >= 0 Then ThemeNow = NewTheme

            '根据主题预设设置颜色参数
            If ThemeNow >= 0 AndAlso ThemeNow < ThemePresets.Length Then
                If ThemeNow = 8 Then
                    '自定义：从设置读取滑块值
                    ColorHue = Settings.Get(Of Integer)("UiThemeCustomHue")
                    ColorSat = Settings.Get(Of Integer)("UiThemeCustomSat")
                    ColorLightAdjust = Settings.Get(Of Integer)("UiThemeCustomLight")
                    ColorHueTopbarDelta = Settings.Get(Of Integer)("UiThemeCustomDelta")
                    ThemeStyle = 0
                Else
                    Dim Preset = ThemePresets(ThemeNow)
                    ColorHue = Preset.Hue
                    ColorSat = Preset.Sat
                    ColorLightAdjust = Preset.LightAdjust
                    ColorHueTopbarDelta = Preset.TopbarDelta
                    ThemeStyle = Preset.Style
                End If
            End If

            Color1 = New MyColor().FromHSL2(ColorHue, ColorSat * 0.2, 25 + ColorLightAdjust * 0.3)
            Color2 = New MyColor().FromHSL2(ColorHue, ColorSat, 45 + ColorLightAdjust)
            Color3 = New MyColor().FromHSL2(ColorHue, ColorSat, 55 + ColorLightAdjust)
            Color4 = New MyColor().FromHSL2(ColorHue, ColorSat, 65 + ColorLightAdjust)
            Color5 = New MyColor().FromHSL2(ColorHue, ColorSat, 80 + ColorLightAdjust * 0.4)
            Color6 = New MyColor().FromHSL2(ColorHue, ColorSat, 91 + ColorLightAdjust * 0.1)
            Color7 = New MyColor().FromHSL2(ColorHue, ColorSat, 95)
            Color8 = New MyColor().FromHSL2(ColorHue, ColorSat, 97)
            ColorBg0 = Color4 * 0.4 + Color5 * 0.4 + ColorGray4 * 0.2
            ColorBg1 = New MyColor(190, Color7)

            ColorSemiTransparent = New MyColor(1, Color8)
            Application.Current.Resources("ColorBrush1") = New SolidColorBrush(Color1)
            Application.Current.Resources("ColorBrush2") = New SolidColorBrush(Color2)
            Application.Current.Resources("ColorBrush3") = New SolidColorBrush(Color3)
            Application.Current.Resources("ColorBrush4") = New SolidColorBrush(New MyColor(204, Color4))
            Application.Current.Resources("ColorBrush5") = New SolidColorBrush(New MyColor(199, Color5))
            Application.Current.Resources("ColorBrush6") = New SolidColorBrush(New MyColor(184, Color6))
            Application.Current.Resources("ColorBrush7") = New SolidColorBrush(New MyColor(179, Color7))
            Application.Current.Resources("ColorBrush8") = New SolidColorBrush(New MyColor(179, Color8))
            Application.Current.Resources("ColorBrushBg0") = New SolidColorBrush(ColorBg0)
            Application.Current.Resources("ColorBrushBg1") = New SolidColorBrush(ColorBg1)
            Application.Current.Resources("ColorObject1") = CType(Color1, Color)
            Application.Current.Resources("ColorObject2") = CType(Color2, Color)
            Application.Current.Resources("ColorObject3") = CType(Color3, Color)
            Application.Current.Resources("ColorObject4") = CType(Color4, Color)
            Application.Current.Resources("ColorObject5") = CType(Color5, Color)
            Application.Current.Resources("ColorObject6") = CType(Color6, Color)
            Application.Current.Resources("ColorObject7") = CType(Color7, Color)
            Application.Current.Resources("ColorObject8") = CType(Color8, Color)
            Application.Current.Resources("ColorObjectBg0") = CType(ColorBg0, Color)
            Application.Current.Resources("ColorObjectBg1") = CType(ColorBg1, Color)
            ThemeRefreshMain()
        Catch ex As Exception
            Logger.Error(ex, "刷新主题颜色失败", LogBehavior.Toast)
        End Try
    End Sub
    Public Sub ThemeRefreshMain()
        RunInUi(
        Sub()
            If Not FrmMain.IsLoaded Then Return
            Dim H As Integer = ColorHue, S As Integer = ColorSat, LA As Integer = ColorLightAdjust
            Dim Deltas = ColorHueTopbarDelta.Switch(Function(d) New Integer() {-d, 0, d}, Function(d) d)
            Dim MkC As Func(Of Integer, Integer, MyColor) = Function(d, L) New MyColor().FromHSL2((H + d + 360) Mod 360, S, L)
            Dim TitleBrush As LinearGradientBrush
            '======== 顶栏 ========
            Select Case ThemeStyle
                Case 1 '素雅：浅色柔和双段
                    TitleBrush = New LinearGradientBrush With {.EndPoint = New Point(1, 0), .StartPoint = New Point(0, 0)}
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(128, MkC(Deltas(0), 55 + LA))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(107, MkC(Deltas(2), 62 + LA))})
                Case 2 '斜锋：135° 斜向三段
                    TitleBrush = New LinearGradientBrush With {.EndPoint = New Point(1, 0), .StartPoint = New Point(0, 1)}
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(166, MkC(Deltas(0), 46 + LA))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0.5, .Color = New MyColor(140, MkC(Deltas(1), 56 + LA))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(153, MkC(Deltas(2), 48 + LA))})
                Case 3 '光弧：中央高亮
                    TitleBrush = New LinearGradientBrush With {.EndPoint = New Point(1, 0), .StartPoint = New Point(0, 0)}
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(97, MkC(Deltas(0), 48 + LA))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0.42, .Color = New MyColor(204, MkC(Deltas(1), 58 + LA))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0.58, .Color = New MyColor(204, MkC(Deltas(1), 58 + LA))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(97, MkC(Deltas(2), 48 + LA))})
                Case 4 '幽夜：深色顶栏
                    TitleBrush = New LinearGradientBrush With {.EndPoint = New Point(1, 0), .StartPoint = New Point(0, 0)}
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(209, MkC(Deltas(0), 36 + LA * 0.6))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0.5, .Color = New MyColor(191, MkC(Deltas(1), 44 + LA * 0.6))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(209, MkC(Deltas(2), 36 + LA * 0.6))})
                Case 5 '琉璃：垂直上深下浅
                    TitleBrush = New LinearGradientBrush With {.EndPoint = New Point(0, 1), .StartPoint = New Point(0, 0)}
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(153, MkC(Deltas(0), 50 + LA))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(199, MkC(Deltas(2), 62 + LA))})
                Case 6 '晨光：左下到右上逆光
                    TitleBrush = New LinearGradientBrush With {.EndPoint = New Point(1, 0), .StartPoint = New Point(0, 1)}
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(140, MkC(Deltas(0), 48 + LA))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0.6, .Color = New MyColor(184, MkC(Deltas(1), 58 + LA))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(128, MkC(Deltas(2), 50 + LA))})
                Case 7 '星海：中央亮弧
                    TitleBrush = New LinearGradientBrush With {.EndPoint = New Point(1, 0), .StartPoint = New Point(0, 0)}
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(115, MkC(Deltas(0), 50 + LA))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0.45, .Color = New MyColor(209, MkC(Deltas(1), 60 + LA))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0.55, .Color = New MyColor(209, MkC(Deltas(1), 60 + LA))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(115, MkC(Deltas(2), 50 + LA))})
                Case 8 '液态玻璃：玻璃高光（细高光 + 深色玻璃）
                    TitleBrush = New LinearGradientBrush With {.EndPoint = New Point(0, 1), .StartPoint = New Point(0, 0)}
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(107, New MyColor(255, 255, 255))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0.12, .Color = New MyColor(140, MkC(Deltas(0), 60))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(204, MkC(Deltas(2), 40))})
                Case 9 '毛玻璃：磨砂浅色
                    TitleBrush = New LinearGradientBrush With {.EndPoint = New Point(1, 0), .StartPoint = New Point(0, 0)}
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(140, New MyColor(250, 250, 250))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0.5, .Color = New MyColor(128, MkC(Deltas(1), 66 + LA))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(115, New MyColor(245, 248, 252))})
                Case 10 '大海：深海渐变
                    TitleBrush = New LinearGradientBrush With {.EndPoint = New Point(1, 0), .StartPoint = New Point(0, 0)}
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(235, MkC(Deltas(0), 26))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0.5, .Color = New MyColor(224, MkC(Deltas(1), 33))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(235, MkC(Deltas(2), 26))})
                Case 11 '夜空：深蓝黑
                    TitleBrush = New LinearGradientBrush With {.EndPoint = New Point(1, 0), .StartPoint = New Point(0, 0)}
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(242, MkC(Deltas(0), 14))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0.5, .Color = New MyColor(235, MkC(Deltas(1), 20))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(242, MkC(Deltas(2), 14))})
                Case 12 '重生：暗金新生
                    TitleBrush = New LinearGradientBrush With {.EndPoint = New Point(1, 0), .StartPoint = New Point(0, 0)}
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(230, MkC(Deltas(0), 30))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0.5, .Color = New MyColor(224, MkC(Deltas(1), 40))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(230, MkC(Deltas(2), 28))})
                Case 13 '黑客：黑绿
                    TitleBrush = New LinearGradientBrush With {.EndPoint = New Point(1, 0), .StartPoint = New Point(0, 0)}
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(242, MkC(Deltas(0), 10))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0.5, .Color = New MyColor(230, MkC(Deltas(1), 18))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(242, MkC(Deltas(2), 10))})
                Case 15 '信仰：金棕圣光
                    TitleBrush = New LinearGradientBrush With {.EndPoint = New Point(1, 0), .StartPoint = New Point(0, 0)}
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(230, MkC(Deltas(0), 34))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0.5, .Color = New MyColor(217, MkC(Deltas(1), 46))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(230, MkC(Deltas(2), 32))})
                Case Else '0 经典：横渐变三段
                    TitleBrush = New LinearGradientBrush With {.EndPoint = New Point(1, 0), .StartPoint = New Point(0, 0)}
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(184, MkC(Deltas(0), 48 + LA))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 0.5, .Color = New MyColor(153, MkC(Deltas(1), 54 + LA))})
                    TitleBrush.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(115, MkC(Deltas(2), 48 + LA))})
            End Select
            TitleBrush.Freeze()
            FrmMain.PanTitle.Background = Nothing
            FrmMain.PanTitleBorder.Background = TitleBrush
            '======== 结构形态 ========
            '窗口圆角
            Dim Corner As Double = 6
            Select Case ThemeStyle
                Case 11 '夜空：直角窗口
                    Corner = 0
                Case 12, 15 '重生/信仰：大圆角
                    Corner = 14
            End Select
            If FrmMain.RectForm.RadiusX <> Corner Then
                FrmMain.RectForm.RadiusX = Corner
                FrmMain.RectForm.RadiusY = Corner
            End If
            '顶栏形态：悬浮胶囊 or 全宽
            If ThemeStyle = 12 OrElse ThemeStyle = 15 Then
                FrmMain.PanTitleBorder.Margin = New Thickness(8, 6, 8, 0)
                FrmMain.PanTitleBorder.CornerRadius = New CornerRadius(14)
                FrmMain.PanTitleBorder.BorderThickness = New Thickness(1)
                Dim EdgeBrush As New SolidColorBrush(New MyColor(102, MkC(0, 64)))
                EdgeBrush.Freeze()
                FrmMain.PanTitleBorder.BorderBrush = EdgeBrush
            Else
                FrmMain.PanTitleBorder.Margin = New Thickness(0)
                FrmMain.PanTitleBorder.CornerRadius = New CornerRadius(0)
                FrmMain.PanTitleBorder.BorderThickness = New Thickness(0)
                FrmMain.PanTitleBorder.BorderBrush = Nothing
            End If
            '内容区遮罩：染上主题色，让整个界面浸在主题氛围里
            Dim MaskAlpha As Double = 0.22
            If ThemeStyle >= 10 Then MaskAlpha = 0.26
            Dim MaskC As MyColor = New MyColor(255, 255, 255)
            If Settings.Get(Of Boolean)("UiBackgroundColorful") Then
                Dim ML As Integer = If(ThemeStyle >= 10, 78, 86)
                MaskC = New MyColor().FromHSL2(ColorHue, ColorSat * 0.4, ML)
            End If
            Dim MaskBrush As New SolidColorBrush(New MyColor(MaskAlpha, MaskC))
            MaskBrush.Freeze()
            FrmMain.PanWhiteMask.Fill = MaskBrush
            '顶栏底部霓虹线
            Dim GlowC As MyColor = MkC(0, 55)
            If ThemeStyle = 13 Then GlowC = New MyColor().FromHSL2(130, 90, 50)
            Dim GlowBrush As New LinearGradientBrush With {.EndPoint = New Point(1, 0), .StartPoint = New Point(0, 0)}
            GlowBrush.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(0, GlowC)})
            GlowBrush.GradientStops.Add(New GradientStop With {.Offset = 0.5, .Color = New MyColor(230, GlowC)})
            GlowBrush.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(0, GlowC)})
            GlowBrush.Freeze()
            FrmMain.PanTitleGlowLine.Fill = GlowBrush
            Dim GlowWideBrush As New LinearGradientBrush With {.EndPoint = New Point(1, 0), .StartPoint = New Point(0, 0)}
            GlowWideBrush.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(0, GlowC)})
            GlowWideBrush.GradientStops.Add(New GradientStop With {.Offset = 0.5, .Color = New MyColor(128, GlowC)})
            GlowWideBrush.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(0, GlowC)})
            GlowWideBrush.Freeze()
            FrmMain.PanTitleGlowWide.Fill = GlowWideBrush
            '======== 主页面背景 ========
            Dim MainBrush As Brush
            If Settings.Get(Of Boolean)("UiBackgroundColorful") Then
                Select Case ThemeStyle
                    Case 1 '素雅：垂直柔和浅渐变
                        Dim G1 As New LinearGradientBrush With {.EndPoint = New Point(0.5, 1), .StartPoint = New Point(0.5, 0)}
                        G1.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(56, MkC(0, 90))})
                        G1.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(77, MkC(0, 94))})
                        MainBrush = G1
                    Case 2 '斜锋：45° 双色强渐变
                        Dim G2 As New LinearGradientBrush With {.EndPoint = New Point(1, 1), .StartPoint = New Point(0, 0)}
                        G2.GradientStops.Add(New GradientStop With {.Offset = -0.1, .Color = New MyColor(102, MkC(-25, 90))})
                        G2.GradientStops.Add(New GradientStop With {.Offset = 0.55, .Color = New MyColor(89, MkC(0, 91))})
                        G2.GradientStops.Add(New GradientStop With {.Offset = 1.1, .Color = New MyColor(102, MkC(25, 90))})
                        MainBrush = G2
                    Case 3 '光弧：径向中央光
                        Dim G3 As New RadialGradientBrush
                        G3.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(107, MkC(0, 93))})
                        G3.GradientStops.Add(New GradientStop With {.Offset = 0.7, .Color = New MyColor(77, MkC(-10, 91))})
                        G3.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(46, MkC(15, 90))})
                        MainBrush = G3
                    Case 4 '幽夜：浅底，暗角交给装饰层
                        Dim G4 As New LinearGradientBrush With {.EndPoint = New Point(1, 0), .StartPoint = New Point(0, 0)}
                        G4.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(77, MkC(0, 90))})
                        G4.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(61, MkC(5, 92))})
                        MainBrush = G4
                    Case 5 '琉璃：中央光晕
                        Dim G5 As New RadialGradientBrush
                        G5.GradientOrigin = New Point(0.5, 0.35)
                        G5.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(115, MkC(0, 93))})
                        G5.GradientStops.Add(New GradientStop With {.Offset = 0.65, .Color = New MyColor(71, MkC(-8, 91))})
                        G5.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(51, MkC(10, 90))})
                        MainBrush = G5
                    Case 6 '晨光：顶部大光斑
                        Dim G6 As New RadialGradientBrush
                        G6.GradientOrigin = New Point(0.5, 0)
                        G6.Center = New Point(0.5, 0)
                        G6.RadiusX = 0.9
                        G6.RadiusY = 0.9
                        G6.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(107, MkC(0, 94))})
                        G6.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(51, MkC(-15, 89))})
                        MainBrush = G6
                    Case 7 '星海：浅底，光斑交给装饰层
                        Dim G7 As New LinearGradientBrush With {.EndPoint = New Point(0.5, 1), .StartPoint = New Point(0.5, 0)}
                        G7.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(64, MkC(0, 92))})
                        G7.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(82, MkC(-8, 93))})
                        MainBrush = G7
                    Case 8 '液态玻璃：深色流动玻璃
                        Dim L1 As New LinearGradientBrush With {.EndPoint = New Point(0, 1), .StartPoint = New Point(0, 0)}
                        L1.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(230, MkC(0, 30))})
                        L1.GradientStops.Add(New GradientStop With {.Offset = 0.6, .Color = New MyColor(224, MkC(-10, 24))})
                        L1.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(230, MkC(10, 30))})
                        MainBrush = L1
                    Case 9 '毛玻璃：漫射浅色
                        Dim L2 As New LinearGradientBrush With {.EndPoint = New Point(0, 1), .StartPoint = New Point(0, 0)}
                        L2.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(140, MkC(0, 92))})
                        L2.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(153, MkC(0, 95))})
                        MainBrush = L2
                    Case 10 '大海：深海向下渐深
                        Dim L3 As New LinearGradientBrush With {.EndPoint = New Point(0, 1), .StartPoint = New Point(0, 0)}
                        L3.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(230, MkC(0, 30))})
                        L3.GradientStops.Add(New GradientStop With {.Offset = 0.65, .Color = New MyColor(235, MkC(-5, 22))})
                        L3.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(240, MkC(5, 14))})
                        MainBrush = L3
                    Case 11 '夜空：深蓝黑
                        Dim L4 As New LinearGradientBrush With {.EndPoint = New Point(0, 1), .StartPoint = New Point(0, 0)}
                        L4.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(242, MkC(0, 16))})
                        L4.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(242, MkC(-10, 8))})
                        MainBrush = L4
                    Case 12 '重生：暗底金光渐起
                        Dim L5 As New LinearGradientBrush With {.EndPoint = New Point(1, 1), .StartPoint = New Point(0, 0)}
                        L5.GradientStops.Add(New GradientStop With {.Offset = -0.1, .Color = New MyColor(217, MkC(15, 36))})
                        L5.GradientStops.Add(New GradientStop With {.Offset = 0.6, .Color = New MyColor(230, MkC(-8, 22))})
                        L5.GradientStops.Add(New GradientStop With {.Offset = 1.1, .Color = New MyColor(235, MkC(-20, 12))})
                        MainBrush = L5
                    Case 13 '黑客：近黑
                        Dim L6 As New LinearGradientBrush With {.EndPoint = New Point(0, 1), .StartPoint = New Point(0, 0)}
                        L6.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(242, MkC(0, 10))})
                        L6.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(242, MkC(0, 4))})
                        MainBrush = L6
                    Case 15 '信仰：暗金殿堂
                        Dim L8 As New LinearGradientBrush With {.EndPoint = New Point(0, 1), .StartPoint = New Point(0, 0)}
                        L8.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(224, MkC(0, 34))})
                        L8.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(230, MkC(-15, 22))})
                        MainBrush = L8
                    Case Else '0 经典：对角渐变
                        Dim G8 As New LinearGradientBrush With {.EndPoint = New Point(0.1, 1), .StartPoint = New Point(0.9, 0)}
                        G8.GradientStops.Add(New GradientStop With {.Offset = -0.1, .Color = New MyColor(89, MkC(-15, 91))})
                        G8.GradientStops.Add(New GradientStop With {.Offset = 0.4, .Color = New MyColor(89, MkC(0, 91))})
                        G8.GradientStops.Add(New GradientStop With {.Offset = 1.1, .Color = New MyColor(89, MkC(15, 91))})
                        MainBrush = G8
                End Select
            Else
                MainBrush = New SolidColorBrush(New MyColor(102, New MyColor(245, 245, 245)))
            End If
            MainBrush.Freeze()
            FrmMain.PanForm.Background = MainBrush
            '======== 装饰层 ========
            '深色氛围主题下加厚侧边栏底色，保证可读性
            Dim SideFill As MyColor = New MyColor(184, New MyColor(255, 255, 255))
            If ThemeStyle >= 10 AndAlso ThemeStyle <= 15 Then SideFill = New MyColor(219, New MyColor(250, 250, 250))
            Dim SideBrush As New SolidColorBrush(SideFill)
            SideBrush.Freeze()
            FrmMain.RectLeftBackground.Fill = SideBrush
            FrmMain.PanDeco.Children.Clear()
            FrmMain.PanDecoTitle.Children.Clear()
            ThemeAnimated.Clear()
            Dim W As Double = FrmMain.PanForm.ActualWidth
            Dim HD As Double = FrmMain.PanForm.ActualHeight
            If W <= 0 OrElse HD <= 0 Then Return
            Dim Gold As MyColor = MkC(0, 82)
            Select Case ThemeStyle
                Case 8 '液态玻璃：顶栏斜反光 + 流动高光带，全屏淡蓝氛围
                    Dim Reflect As New Rectangle With {.Width = W * 0.55, .Height = 40}
                    Dim RB As New LinearGradientBrush With {.EndPoint = New Point(1, 1), .StartPoint = New Point(0, 0)}
                    RB.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(140, New MyColor(255, 255, 255))})
                    RB.GradientStops.Add(New GradientStop With {.Offset = 0.25, .Color = New MyColor(13, New MyColor(255, 255, 255))})
                    RB.GradientStops.Add(New GradientStop With {.Offset = 0.38, .Color = New MyColor(64, New MyColor(255, 255, 255))})
                    RB.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(0, New MyColor(255, 255, 255))})
                    RB.Freeze()
                    Reflect.Fill = RB
                    Reflect.RenderTransform = New RotateTransform(-32)
                    Canvas.SetLeft(Reflect, W * 0.15)
                    Canvas.SetTop(Reflect, 4)
                    FrmMain.PanDecoTitle.Children.Add(Reflect)
                    DecoGlow(New MyColor(255, 255, 255), 128, W * 0.55, 22, W * 0.16, 6, False, False, True)
                    DecoGlow(New MyColor(255, 255, 255), 82, W * 0.9, 14, W * 0.1, 4, False, False, True)
                    DecoGlow(New MyColor(255, 255, 255), 128, W * 0.25, 24, W * 0.2, 9, False, False, True)
                    DecoGlow(New MyColor(160, 205, 255), 102, W * 0.5, HD * 0.5, W * 0.45, HD * 0.4)
                Case 9 '毛玻璃：磨砂颗粒 + 顶部漫射 + 底部雾
                    Dim Grain As New DrawingGroup
                    Dim GGeom As New GeometryGroup
                    Dim GR As New Random(7)
                    For i = 1 To 140
                        GGeom.Children.Add(New EllipseGeometry(New Point(GR.NextDouble() * W, GR.NextDouble() * HD), 0.6 + GR.NextDouble(), 0.6 + GR.NextDouble()))
                    Next
                    Grain.Children.Add(New GeometryDrawing(New SolidColorBrush(New MyColor(13, New MyColor(196, 198, 210))), Nothing, GGeom))
                    Dim GrainBrush As New DrawingBrush With {.Drawing = Grain}
                    GrainBrush.Freeze()
                    Dim GrainRect As New Rectangle With {.Width = W, .Height = HD, .Fill = GrainBrush}
                    Canvas.SetLeft(GrainRect, 0)
                    Canvas.SetTop(GrainRect, 0)
                    FrmMain.PanDeco.Children.Add(GrainRect)
                    DecoGlow(New MyColor(255, 255, 255), 128, W * 0.5, HD * 0.1, W * 0.7, HD * 0.4)
                    DecoGlow(New MyColor(255, 255, 255), 128, W * 0.5, HD * 1.02, W * 0.75, HD * 0.16)
                    '顶栏：白色颗粒更明显
                    Dim TG As New DrawingGroup
                    Dim TGGeom As New GeometryGroup
                    For i = 1 To 60
                        TGGeom.Children.Add(New EllipseGeometry(New Point(GR.NextDouble() * W, GR.NextDouble() * 46), 0.6 + GR.NextDouble(), 0.6 + GR.NextDouble()))
                    Next
                    TG.Children.Add(New GeometryDrawing(New SolidColorBrush(New MyColor(41, New MyColor(255, 255, 255))), Nothing, TGGeom))
                    Dim TGBrush As New DrawingBrush With {.Drawing = TG}
                    TGBrush.Freeze()
                    Dim TGRect As New Rectangle With {.Width = W, .Height = 46, .Fill = TGBrush}
                    Canvas.SetLeft(TGRect, 0)
                    Canvas.SetTop(TGRect, 0)
                    FrmMain.PanDecoTitle.Children.Add(TGRect)
                    DecoGlow(New MyColor(255, 255, 255), 41, W * 0.5, 10, W * 0.5, 16, False, False, True)
                Case 10 '大海：波浪线 + 底部波光 + 折射光
                    DecoWave(W, HD, New MyColor(89, 89, 92), 89, HD * 0.66, 1.8)
                    DecoWave(W, HD, New MyColor(89, 89, 92), 71, HD * 0.78, 2.6)
                    DecoGlow(New MyColor(255, 255, 255), 64, W * 0.5, HD * 0.98, W * 0.9, HD * 0.12)
                    DecoGlow(New MyColor(255, 255, 255), 51, W * 0.2, HD * 0.9, W * 0.5, HD * 0.06)
                    DecoGlow(New MyColor(255, 255, 255), 41, W * 0.2, HD * 0.28, W * 0.3, HD * 0.1)
                    DecoGlow(New MyColor(255, 255, 255), 51, W * 0.78, HD * 0.44, W * 0.22, HD * 0.08)
                    '顶栏：海面线 + 波光
                    DecoGlow(New MyColor(255, 255, 255), 128, W * 0.5, 44, W * 0.5, 2.5, False, False, True)
                    DecoGlow(New MyColor(255, 255, 255), 102, W * 0.72, 34, W * 0.12, 3, False, False, True)
                    DecoGlow(New MyColor(255, 255, 255), 77, W * 0.3, 24, W * 0.07, 2, False, False, True)
                Case 11 '夜空：顶栏星光 + 月光 + 流星，全屏淡蓝柔光与深蓝氛围
                    Dim Rnd As New Random(42)
                    For i = 1 To 22
                        Dim Sx = Rnd.NextDouble() * W
                        Dim Sy = 4 + Rnd.NextDouble() * 40
                        Dim Sz = 0.7 + Rnd.NextDouble() * 1.9
                        Dim Sa = 102 + Rnd.NextDouble() * 140
                        Dim SC As MyColor = If(Rnd.NextDouble() < 0.2, MkC(-30, 80), New MyColor(255, 255, 255))
                        DecoGlow(SC, Sa, Sx, Sy, Sz, Sz, True, False, True)
                    Next
                    For i = 1 To 12
                        Dim Sx = Rnd.NextDouble() * W
                        Dim Sy = Rnd.NextDouble() * HD
                        Dim Sz = 0.9 + Rnd.NextDouble() * 2.2
                        DecoGlow(New MyColor(255, 255, 255), 46, Sx, Sy, Sz, Sz)
                    Next
                    DecoGlow(New MyColor(255, 244, 200), 166, W * 0.82, 14, 10, 10, False, False, True)
                    DecoGlow(New MyColor(255, 244, 200), 46, W * 0.82, 14, 22, 22, False, False, True)
                    Dim Meteor As New Rectangle With {.Width = W * 0.22, .Height = 2}
                    Dim MB As New LinearGradientBrush With {.EndPoint = New Point(1, 0), .StartPoint = New Point(0, 0)}
                    MB.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(230, New MyColor(255, 255, 255))})
                    MB.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(0, New MyColor(255, 255, 255))})
                    MB.Freeze()
                    Meteor.Fill = MB
                    Meteor.RenderTransform = New RotateTransform(30)
                    Canvas.SetLeft(Meteor, W * 0.5)
                    Canvas.SetTop(Meteor, 16)
                    FrmMain.PanDecoTitle.Children.Add(Meteor)
                    '全屏深蓝氛围（内容区更夜空）
                    DecoGlow(New MyColor(51, 90, 150), 46, W * 0.5, HD * 0.7, W * 0.6, HD * 0.4)
                Case 12 '重生：上升光柱 + 中央光球 + 余烬
                    Dim Pillar As New Path
                    Dim PGeo As New PathGeometry
                    Dim PF As New PathFigure
                    PF.StartPoint = New Point(W * 0.5 - W * 0.42, HD)
                    PF.Segments.Add(New LineSegment With {.Point = New Point(W * 0.5 - W * 0.1, HD * 0.3)})
                    PF.Segments.Add(New LineSegment With {.Point = New Point(W * 0.5 + W * 0.1, HD * 0.3)})
                    PF.Segments.Add(New LineSegment With {.Point = New Point(W * 0.5 + W * 0.42, HD)})
                    PF.IsClosed = True
                    PGeo.Figures.Add(PF)
                    Pillar.Data = PGeo
                    Dim PillarB As New LinearGradientBrush With {.EndPoint = New Point(0, 0), .StartPoint = New Point(0, 1)}
                    PillarB.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(102, Gold)})
                    PillarB.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(0, Gold)})
                    PillarB.Freeze()
                    Pillar.Fill = PillarB
                    Canvas.SetLeft(Pillar, 0)
                    Canvas.SetTop(Pillar, 0)
                    FrmMain.PanDeco.Children.Add(Pillar)
                    DecoGlow(Gold, 89, W * 0.38, HD * 0.5, W * 0.2, HD * 0.2)
                    DecoGlow(Gold, 102, W * 0.38, HD * 0.52, W * 0.4, HD * 0.4)
                    DecoGlow(Gold, 77, W * 0.08, HD * 0.92, W * 0.24, HD * 0.24)
                    '顶栏：中央光球 + 两侧余烬
                    DecoGlow(Gold, 217, W * 0.5, 24, 12, 12, False, False, True)
                    DecoGlow(Gold, 89, W * 0.5, 24, 30, 16, False, False, True)
                    DecoGlow(Gold, 51, W * 0.14, 20, 7, 7, False, False, True)
                    DecoGlow(Gold, 41, W * 0.86, 26, 5, 5, False, False, True)
                    DecoGlow(Gold, 46, W * 0.3, 12, 4, 4, False, False, True)
                Case 13 '黑客：矩阵网格 + 字符雨 + 扫描线动画
                    DecoGrid(W, HD, New MyColor(41, 255, 60), 56, 26.0)
                    Dim CharBrush As New SolidColorBrush(New MyColor(82, New MyColor(90, 255, 120)))
                    CharBrush.Freeze()
                    Dim KR As New Random(77)
                    For i = 1 To 8
                        Dim T As New TextBlock
                        Dim SB As New Text.StringBuilder
                        For j = 1 To 9
                            SB.Append(KR.Next(2).ToString())
                        Next
                        T.Text = SB.ToString()
                        T.FontFamily = New FontFamily("Consolas")
                        T.FontSize = 11
                        T.Foreground = CharBrush
                        Canvas.SetLeft(T, 10 + i * (W / 9.5))
                        Canvas.SetTop(T, KR.NextDouble() * HD * 0.8)
                        FrmMain.PanDeco.Children.Add(T)
                    Next
                    DecoGlow(New MyColor(82, 255, 120), 51, W * 0.7, HD * 0.25, W * 0.35, HD * 0.3)
                    DecoGlow(New MyColor(82, 255, 120), 51, W * 0.15, HD * 0.8, W * 0.25, HD * 0.2)
                    DecoGlow(New MyColor(82, 255, 120), 41, W * 0.5, HD * 0.6, W * 0.62, HD * 0.02, False, True)
                    '顶栏：顶部绿光 + 字符雨
                    DecoGlow(New MyColor(82, 255, 120), 51, W * 0.5, 2, W * 0.8, 3, False, False, True)
                    Dim TKB As New SolidColorBrush(New MyColor(115, New MyColor(90, 255, 120)))
                    TKB.Freeze()
                    Dim TKR As New Random(88)
                    For i = 1 To 6
                        Dim T As New TextBlock
                        Dim SB2 As New Text.StringBuilder
                        For j = 1 To 4
                            SB2.Append(TKR.Next(2).ToString())
                        Next
                        T.Text = SB2.ToString()
                        T.FontFamily = New FontFamily("Consolas")
                        T.FontSize = 9
                        T.Foreground = TKB
                        Canvas.SetLeft(T, 6 + i * (W / 7))
                        Canvas.SetTop(T, 6 + TKR.NextDouble() * 30)
                        FrmMain.PanDecoTitle.Children.Add(T)
                    Next
                Case 15 '信仰：圣光锥 + 放射光线 + 金晕
                    Dim PathL As New Path
                    Dim Geo As New PathGeometry
                    Dim F As New PathFigure
                    F.StartPoint = New Point(W * 0.5 - W * 0.16, 0)
                    F.Segments.Add(New LineSegment With {.Point = New Point(W * 0.5 - W * 0.03, HD * 0.62)})
                    F.Segments.Add(New LineSegment With {.Point = New Point(W * 0.5 + W * 0.03, HD * 0.62)})
                    F.Segments.Add(New LineSegment With {.Point = New Point(W * 0.5 + W * 0.16, 0)})
                    F.IsClosed = True
                    Geo.Figures.Add(F)
                    PathL.Data = Geo
                    Dim PB As New LinearGradientBrush With {.EndPoint = New Point(0, 1), .StartPoint = New Point(0, 0)}
                    PB.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(107, New MyColor(255, 250, 220))})
                    PB.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(0, New MyColor(255, 250, 220))})
                    PB.Freeze()
                    PathL.Fill = PB
                    Canvas.SetLeft(PathL, 0)
                    Canvas.SetTop(PathL, 0)
                    FrmMain.PanDeco.Children.Add(PathL)
                    '放射光线（顶栏，从中央金晕向两侧发散）
                    Dim RayBrush As New SolidColorBrush(New MyColor(102, New MyColor(255, 248, 210)))
                    RayBrush.Freeze()
                    For i = -4 To 4
                        If i = 0 Then Continue For
                        Dim Ray As New Rectangle With {.Width = W * 0.4, .Height = 1.4}
                        Ray.Fill = RayBrush
                        Dim RT As New RotateTransform(i * 5)
                        Ray.RenderTransform = RT
                        Ray.RenderTransformOrigin = New Point(0, 0)
                        Canvas.SetLeft(Ray, W * 0.5 - W * 0.2)
                        Canvas.SetTop(Ray, 24)
                        FrmMain.PanDecoTitle.Children.Add(Ray)
                    Next
                    DecoGlow(Gold, 217, W * 0.5, 24, 10, 10, False, False, True)
                    DecoGlow(Gold, 89, W * 0.5, 24, 26, 14, False, False, True)
                    DecoGlow(Gold, 140, W * 0.5, HD * 0.5, W * 0.42, HD * 0.42)
                    DecoGlow(MkC(0, 88), 89, W * 0.5, HD * 0.86, W * 0.5, HD * 0.2)
            End Select
        End Sub)
    End Sub
    Private Sub DecoWave(W As Double, HD As Double, C As MyColor, Alpha As Double, Y As Double, Stroke As Double)
        '正弦波浪线
        Dim P As New Path
        Dim PG As New PathGeometry
        Dim PF As New PathFigure
        PF.StartPoint = New Point(0, Y)
        Dim Amp = HD * 0.012
        Dim Segs = 8
        For i = 1 To Segs
            Dim X1 = W * i / Segs
            Dim X0 = W * (i - 1) / Segs
            PF.Segments.Add(New QuadraticBezierSegment(New Point((X0 + X1) / 2, Y + Amp), New Point(X1, Y + Amp), True))
            Amp = -Amp
        Next
        PG.Figures.Add(PF)
        P.Data = PG
        Dim SB2 As New SolidColorBrush(New MyColor(Alpha, C))
        SB2.Freeze()
        P.Stroke = SB2
        P.StrokeThickness = Stroke
        P.Fill = Nothing
        Canvas.SetLeft(P, 0)
        Canvas.SetTop(P, 0)
        FrmMain.PanDeco.Children.Add(P)
    End Sub
    Public Sub ThemeAnimationTick()
        '每 250ms 由主时钟调用：星星闪烁、扫描线移动
        If ThemeAnimated.Count = 0 OrElse Not FrmMain.IsLoaded Then Return
        RunInUi(
        Sub()
            For Each E In ThemeAnimated
                E.Tick()
            Next
        End Sub)
    End Sub
    Private ThemeAnimated As New List(Of DecoAnimated)
    Private Class DecoAnimated
        Public Target As FrameworkElement
        Public Kind As Integer '0 星星闪烁 1 扫描线下落 2 扫描线水平移动
        Public X As Double
        Public Y As Double
        Public MaxX As Double
        Public MaxY As Double
        Private Shared Rnd As New Random(233)
        Public Sub Tick()
            Try
                Select Case Kind
                    Case 0
                        If Rnd.Next(3) = 0 Then Target.Opacity = 0.25 + Rnd.NextDouble() * 0.75
                    Case 1
                        Y += 5
                        If Y > MaxY + 40 Then Y = -20
                        Canvas.SetTop(Target, Y)
                    Case 2
                        X += 4
                        If X > MaxX + 40 Then X = -24
                        Canvas.SetLeft(Target, X)
                End Select
            Catch ex As Exception
            End Try
        End Sub
    End Class
    Private Sub DecoGlow(C As MyColor, Alpha As Double, CX As Double, CY As Double, RadiusX As Double, RadiusY As Double, Optional Animated As Boolean = False, Optional Scanline As Boolean = False, Optional InTitle As Boolean = False)
        Dim E As New Ellipse With {.Width = RadiusX * 2, .Height = RadiusY * 2}
        Dim B As New RadialGradientBrush
        B.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor(Alpha, C)})
        B.GradientStops.Add(New GradientStop With {.Offset = 0.55, .Color = New MyColor(Alpha * 0.35, C)})
        B.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor(0, C)})
        B.Freeze()
        E.Fill = B
        Canvas.SetLeft(E, CX - RadiusX)
        Canvas.SetTop(E, CY - RadiusY)
        If InTitle Then FrmMain.PanDecoTitle.Children.Add(E) Else FrmMain.PanDeco.Children.Add(E)
        If Animated OrElse Scanline Then
            ThemeAnimated.Add(New DecoAnimated With {.Target = E, .Kind = If(Scanline, 2, 0), .X = CX - RadiusX, .Y = CY - RadiusY, .MaxX = RadiusX * 2, .MaxY = RadiusY * 2})
        End If
    End Sub
    Private Sub DecoEllipse(C As MyColor, Alpha As Double, CX As Double, CY As Double, RX As Double, RY As Double, Optional InTitle As Boolean = False)
        Dim E As New Ellipse With {.Width = RX * 2, .Height = RY * 2}
        Dim B As New SolidColorBrush(New MyColor(Alpha, C))
        B.Freeze()
        E.Fill = B
        Canvas.SetLeft(E, CX - RX)
        Canvas.SetTop(E, CY - RY)
        If InTitle Then FrmMain.PanDecoTitle.Children.Add(E) Else FrmMain.PanDeco.Children.Add(E)
    End Sub
    Private Sub DecoCloud(C As MyColor, Alpha As Double, CX As Double, CY As Double, Size As Double, Optional InTitle As Boolean = False)
        '云朵：三个椭圆叠成
        Dim H1 = Size * 0.38
        DecoEllipse(C, Alpha, CX - Size * 0.35, CY, Size * 0.5, H1, InTitle)
        DecoEllipse(C, Alpha, CX, CY - H1 * 0.45, Size * 0.64, H1 * 1.2, InTitle)
        DecoEllipse(C, Alpha, CX + Size * 0.32, CY, Size * 0.5, H1, InTitle)
    End Sub
    Private Sub DecoGrid(W As Double, HD As Double, C As MyColor, Alpha As Double, StepSize As Double, Optional InTitle As Boolean = False)
        Dim R As New Rectangle With {.Width = W, .Height = HD}
        Dim Group As New GeometryGroup
        Dim Y As Double = StepSize
        While Y < HD
            Group.Children.Add(New LineGeometry(New Point(0, Y), New Point(W, Y)))
            Y += StepSize
        End While
        Dim X As Double = StepSize
        While X < W
            Group.Children.Add(New LineGeometry(New Point(X, 0), New Point(X, HD)))
            X += StepSize
        End While
        Dim DG As New DrawingGroup
        DG.Children.Add(New GeometryDrawing(New SolidColorBrush(New MyColor(Alpha, C)), Nothing, Group))
        Dim DB As New DrawingBrush With {.Drawing = DG}
        DB.Freeze()
        R.Fill = DB
        Canvas.SetLeft(R, 0)
        Canvas.SetTop(R, 0)
        If InTitle Then FrmMain.PanDecoTitle.Children.Add(R) Else FrmMain.PanDeco.Children.Add(R)
    End Sub
    Friend Sub ThemeCheckAll(EffectSetup As Boolean)
    End Sub
    Friend Function ThemeCheckOne(Id As Integer) As Boolean
        Return True
    End Function
    Friend Function ThemeUnlock(Id As Integer, Optional ShowDoubleHint As Boolean = True, Optional UnlockHint As String = Nothing) As Boolean
        Return False
    End Function

#End Region

#Region "更新"

    ''' <summary>
    ''' xcl 的 GitHub 更新检查仓库。
    ''' </summary>
    Private Const GitHubUpdateUrl As String = "https://api.github.com/repos/xiaoy-ovo/pcl-y/releases/latest"

    Friend Sub UpdateCheckByButton()
        Hint("正在检查更新……")
        RunInThread(Sub() GitHubUpdateCheck(True))
    End Sub

    ''' <summary>
    ''' 通过 GitHub Releases 检查更新，并在发现新版本时询问是否更新。
    ''' </summary>
    Friend Sub GitHubUpdateCheck(Optional Manual As Boolean = False)
        Try
            Dim ReleaseInfo As JObject = Nothing
            Try
                Using Handler As New HttpClientHandler()
                    Handler.UseProxy = False '强制直连，避免系统代理导致的 403
                    Handler.UseDefaultCredentials = False
                    Using Client As New HttpClient(Handler)
                        Client.Timeout = TimeSpan.FromSeconds(15)
                        Client.DefaultRequestHeaders.UserAgent.ParseAdd($"pcl-y/{VersionBaseName} Mozilla/5.0")
                        Client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json")
                        ReleaseInfo = JObject.Parse(Client.GetStringAsync(GitHubUpdateUrl).GetAwaiter().GetResult())
                    End Using
                End Using
            Catch ex As Exception
                If Manual Then Hint($"未检查到更新：{ex.Message}")
                Logger.Warn(ex, "GitHub 更新检查失败")
                Return
            End Try
            '解析版本号
            Dim TagName As String = If(ReleaseInfo("tag_name") Is Nothing, "", ReleaseInfo("tag_name").ToString())
            Dim NewVersion As Version = Nothing
            If Not Version.TryParse(TagName.TrimStart("v"c, "V"c), NewVersion) Then
                If Manual Then Hint($"检测到的版本号无法识别：{If(TagName = "", "（空）", TagName)}")
                Return
            End If
            '与当前版本比较
            Dim CurrentVersion As Version = Nothing
            Version.TryParse(VersionBaseName, CurrentVersion)
            If CurrentVersion IsNot Nothing AndAlso NewVersion <= CurrentVersion Then
                If Manual Then Hint($"当前已是最新版本（{VersionDisplay}）")
                Return
            End If
            '发现新版本，询问是否更新
            Dim Body As String = $"发现新版本 v{NewVersion.ToString()}（当前 {VersionDisplay}）{vbCrLf}{vbCrLf}是否立即下载并更新？"
            If MyMsgBox(Body, "发现新版本", "立即更新", "稍后再说") = 1 Then
                DownloadAndInstallUpdate(ReleaseInfo)
            End If
        Catch ex As Exception
            Logger.Error(ex, "GitHub 更新检查出错")
        End Try
    End Sub

    ''' <summary>
    ''' 下载最新版压缩包，解压后通过脚本在退出后替换自身并重启。
    ''' </summary>
    Private Sub DownloadAndInstallUpdate(ReleaseInfo As JObject)
        '选择第一个 zip 或 exe 资产
        Dim Asset As JObject = Nothing
        If ReleaseInfo("assets") IsNot Nothing Then
            For Each A In DirectCast(ReleaseInfo("assets"), JArray)
                Dim Name As String = If(A("name") Is Nothing, "", A("name").ToString().ToLower())
                If Name.EndsWith(".zip") OrElse Name.EndsWith(".exe") Then
                    Asset = A
                    Exit For
                End If
            Next
        End If
        If Asset Is Nothing OrElse Asset("browser_download_url") Is Nothing Then
            Hint("更新包中没有可用的下载文件，请前往 GitHub 手动下载")
            OpenWebsite($"https://github.com/xiaoy-ovo/pcl-y/releases")
            Return
        End If
        Dim UpdateFolder As String = PathTemp & "pcl-yUpdate\"
        Try
            '下载
            Hint("正在下载更新，请稍候……")
            Dim DownloadPath As String = UpdateFolder & "update.bin"
            If Directory.Exists(UpdateFolder) Then Directory.Delete(UpdateFolder, True)
            Directory.CreateDirectory(UpdateFolder)
            Using Handler As New HttpClientHandler()
                Handler.UseProxy = False '强制直连，避免系统代理导致的 403
                Using Client As New HttpClient(Handler)
                    Client.Timeout = TimeSpan.FromSeconds(120)
                    Client.DefaultRequestHeaders.UserAgent.ParseAdd($"pcl-y/{VersionBaseName} Mozilla/5.0")
                    Client.DefaultRequestHeaders.Accept.ParseAdd("application/octet-stream")
                    File.WriteAllBytes(DownloadPath, Client.GetByteArrayAsync(Asset("browser_download_url").ToString()).GetAwaiter().GetResult())
                End Using
            End Using
            '解压或直接作为 exe 使用
            Dim NewExePath As String = Nothing
            If DownloadPath.ToLower().EndsWith(".exe") Then
                NewExePath = DownloadPath
            Else
                System.IO.Compression.ZipFile.ExtractToDirectory(DownloadPath, UpdateFolder)
                Dim AllExe = Directory.GetFiles(UpdateFolder, "*.exe", SearchOption.AllDirectories)
                NewExePath = AllExe.FirstOrDefault(Function(F) F.ToLower().Contains("pcl-y")) OrElse AllExe.FirstOrDefault()
            End If
            If String.IsNullOrEmpty(NewExePath) Then
                Hint("更新包中未找到可执行文件，请前往 GitHub 手动下载")
                OpenWebsite($"https://github.com/xiaoy-ovo/pcl-y/releases")
                Return
            End If
            '准备替换脚本：等本进程退出后复制全部更新文件并重启
            Dim BatContent As String = "@echo off" & vbCrLf &
                "timeout /t 2 /nobreak >nul" & vbCrLf
            For Each F In Directory.GetFiles(UpdateFolder, "*", SearchOption.AllDirectories)
                Dim FileName As String = PathUtils.GetLastPart(F)
                Dim LowerName As String = FileName.ToLower()
                If LowerName = "update.bat" OrElse LowerName = "update.bin" OrElse LowerName.EndsWith(".new") OrElse LowerName = "latest.exe" Then Continue For
                BatContent &= "copy /y """ & F & """ """ & Paths.Base & FileName & """" & vbCrLf
            Next
            BatContent &= "start """" """ & PathExe & """" & vbCrLf &
                "del ""%~f0"""
            Dim BatPath As String = UpdateFolder & "update.bat"
            File.WriteAllText(BatPath, BatContent, Encoding.Default)
            Dim StartInfo As New ProcessStartInfo(BatPath) With {
                .WindowStyle = ProcessWindowStyle.Hidden,
                .CreateNoWindow = True,
                .WorkingDirectory = UpdateFolder}
            StartProcess(StartInfo)
            Logger.Info($"更新即将应用，退出当前进程以完成替换")
            FrmMain.EndProgram(False)
        Catch ex As Exception
            Logger.Error(ex, "更新下载或安装失败")
            Hint($"更新失败：{ex.Message}")
        End Try
    End Sub

    Friend IsUpdateWaitingRestart As Boolean = False
    Public Sub UpdateRestart(TriggerRestartAndByEnd As Boolean)
        If TriggerRestartAndByEnd Then FrmMain.EndProgram(False)
    End Sub
    Public Sub UpdateReplace(ProcessId As Integer, OldFileName As String, NewFileName As String, TriggerRestart As Boolean)
        Try
            If File.Exists(NewFileName) Then File.Copy(NewFileName, OldFileName, True)
            If TriggerRestart Then UpdateRestart(True)
        Catch ex As Exception
            Logger.Warn(ex, "UpdateReplace 失败")
        End Try
    End Sub

    ''' <summary>
    ''' 确保 PathTemp/Latest.exe 是最新正式版的 PCL，它会被用于整合包打包。
    ''' 如果不是，则下载一个。
    ''' </summary>
    Friend Sub DownloadLatestPCL(Optional LoaderToSyncProgress As LoaderBase = Nothing)
        '注意：如果要自行实现这个功能，请换用另一个文件路径，以免与官方版本冲突
    End Sub

#End Region

#Region "联网配置"

    ''' <summary>
    ''' 联网获取的配置信息。
    ''' 若获取失败或仍在获取中，可能为 Nothing。
    ''' </summary>
    Public ServerConfig As JObject

    Public ServerLoader As New LoaderTask(Of Integer, Integer)("PCL 配置更新", Sub() Logger.Info("该版本中不包含更新通知功能……"), Priority:=ThreadPriority.BelowNormal) With
        {.ReloadTimeout = 1000 * 60 * 60} '超时 1 小时

#End Region

#Region "赞助等级"

    Public ReadOnly Property CurrentRank As DonationRank
        Get
            Return DonationRank.None
        End Get
    End Property

    Public Sub InputPotatoCode(IsUpdating As Boolean)
    End Sub
    Friend Sub GeneratePotatoCode()
    End Sub

    ''' <summary>
    ''' 获取设备识别码。
    ''' </summary>
    Friend Function GetIdentify() As String
        Return "0000-0000-0000-0000"
    End Function

#End Region

End Module
