Imports Microsoft.Graphics.Canvas
Imports SpriteAsset
Imports Newtonsoft.Json.Linq

Public NotInheritable Class MainPage
    Inherits Page

    Private FileOpen As New Windows.Storage.Pickers.FileOpenPicker()
    Private FileSave As New Windows.Storage.Pickers.FileSavePicker()
    Friend Currentfile As String
    Friend Editor As ComplexSheet

    Public Sub New()
        InitializeComponent()

        FileOpen.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail
        FileOpen.FileTypeFilter.Add(".png")
        FileOpen.FileTypeFilter.Add(".bmp")

        FileSave.FileTypeChoices.Add("ComplexSheet", New String() {".cmplx"})
    End Sub

    Private Sub MainPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Frame_.Navigate(GetType(BoundsEditor), Me)
    End Sub

    Public Sub IsFileChanged(value As Boolean)
        If value Then
            SymbolUnsaved.Opacity = 1
            TextUnsaved.Text = "You have unsaved changes."
        Else
            SymbolUnsaved.Opacity = 0
            TextUnsaved.Text = "Your changes are saved."
        End If
    End Sub

    Private Sub RefreshWhateverPage()
        CheckBoundCount()
        If TypeOf Frame_.Content Is BoundsEditor Then
            CType(Frame_.Content, BoundsEditor).ReloadBounds()
        ElseIf TypeOf Frame_.Content Is MultiEditor Then
            CType(Frame_.Content, MultiEditor).ReloadAll()
        End If
    End Sub

    Public Sub CheckBoundCount()
        If Editor Is Nothing Then TextBoundCount.Text = "No image loaded."
        TextBoundCount.Text = "Bounds: " & Editor.BoundsCount
    End Sub

    ''' <summary>
    ''' Instantiates a new Editor with the selected image or ComplexSheet, if any.
    ''' </summary>
    Friend Async Sub LoadBitmap(sender As Object, e As RoutedEventArgs)
        FileOpen.FileTypeFilter.Add(".cmplx")
        Dim file = Await FileOpen.PickSingleFileAsync()
        FileOpen.FileTypeFilter.Remove(".cmplx")
        If file IsNot Nothing Then
            If file.FileType = ".cmplx" Then
                Dim data = Await Windows.Storage.FileIO.ReadTextAsync(file)
                Dim json = JObject.Parse(data)
                Editor = New ComplexSheet(json)
            Else
                Currentfile = file.Path
                Dim stream = Await file.OpenReadAsync()
                Dim Image = Await CanvasBitmap.LoadAsync(CanvasDevice.GetSharedDevice, stream)
                Editor = New ComplexSheet(Currentfile, Image)
            End If
        End If
        RefreshWhateverPage()
        ButtonSwap.IsEnabled = True
        IsFileChanged(False)
    End Sub

    Public Async Sub SwapBitmap(sender As Object, e As RoutedEventArgs)
        Dim file = Await FileOpen.PickSingleFileAsync()
        If file IsNot Nothing Then
            Currentfile = file.Path
            Dim stream = Await file.OpenReadAsync()
            Dim Image = Await CanvasBitmap.LoadAsync(CanvasDevice.GetSharedDevice, stream)
            Editor.Image = Image
        End If
        RefreshWhateverPage()
        IsFileChanged(True)
    End Sub

    Friend Async Sub SaveSheet(sender As Object, e As RoutedEventArgs)
        If Editor Is Nothing Then Return
        FileSave.SuggestedFileName = Editor.BitmapName.Replace(".", "_") + ".cmplx"
        Dim file = Await FileSave.PickSaveFileAsync()
        If file IsNot Nothing Then
            Dim export = Editor.Serialize
            Await Windows.Storage.FileIO.WriteTextAsync(file, export.ToString(formatting:=Newtonsoft.Json.Formatting.None, Nothing))
            IsFileChanged(False)
        End If
    End Sub

    Private Sub ButtonBoundsEditor_Click(sender As Object, e As RoutedEventArgs)
        Frame_.Navigate(GetType(BoundsEditor), Me, New Animation.SlideNavigationTransitionInfo With {.Effect = Animation.SlideNavigationTransitionEffect.FromLeft})
        If Editor IsNot Nothing Then
            CType(Frame_.Content, BoundsEditor).ReloadBounds()
        End If
    End Sub
    Private Sub ButtonSpriteEditor_Click(sender As Object, e As RoutedEventArgs)
        Frame_.Navigate(GetType(MultiEditor), Me, New Animation.SlideNavigationTransitionInfo With {.Effect = Animation.SlideNavigationTransitionEffect.FromRight})
        If Editor IsNot Nothing Then
            CType(Frame_.Content, MultiEditor).ReloadAll()
        End If
    End Sub
End Class