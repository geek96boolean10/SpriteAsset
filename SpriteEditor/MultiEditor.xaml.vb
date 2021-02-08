' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

Imports Microsoft.Graphics.Canvas
Imports SpriteAsset
Imports Windows.Graphics.Imaging
Imports Windows.Storage.Streams
''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class MultiEditor
    Inherits Page

    Public MainPage As MainPage

    Private Property Editor As ComplexSheet
        Get
            Return MainPage.Editor
        End Get
        Set(value As ComplexSheet)
            MainPage.Editor = value
        End Set
    End Property

    Protected Overrides Sub OnNavigatedTo(e As NavigationEventArgs)
        MyBase.OnNavigatedTo(e)
        Me.MainPage = e.Parameter
    End Sub

    ''' <summary>
    ''' rebuilds sprite listview, then bounds thumbs
    ''' </summary>
    Public Sub ReloadAll()
        If Editor Is Nothing Then
            SpritesList.Items.Clear()
            Return
        End If

        SpritesList.Items.Clear()
        For Each spr In Editor.Sprites
            Dim msb As New MultiSpriteBuilder(spr)
            SpritesList.Items.Add(msb)
        Next

        ReloadBounds()
    End Sub

    Public Async Sub ReloadBounds()
        BoundsList.Items.Clear()
        Dim bd As List(Of Rect) = Nothing
        Dim id As List(Of Integer) = Nothing
        Dim c = Editor.GetAllBounds(id, bd)
        For i = 0 To c - 1
            Dim ras As New InMemoryRandomAccessStream
            Dim soft As New SoftwareBitmap(BitmapPixelFormat.Bgra8, bd(i).Width, bd(i).Height, BitmapAlphaMode.Premultiplied)

            Dim acc = CanvasBitmap.CreateFromSoftwareBitmap(CanvasDevice.GetSharedDevice, soft)
            acc.CopyPixelsFromBitmap(Editor.Image, 0, 0, bd(i).Left, bd(i).Top, bd(i).Width, bd(i).Height)

            Await acc.SaveAsync(ras, CanvasBitmapFileFormat.Png)
            acc.Dispose()

            Dim newitem = New BoundsThumbViewItem(id(i), AddressOf Editor.GetBounds, ras)
            BoundsList.Items.Add(newitem)
            BoundsList.SelectedItem = newitem
        Next
    End Sub

    Private CurrentSelectedBounds As BoundsThumbViewItem
    Private CurrentSelectedSprite As MultiSpriteBuilder

    Private Sub BoundsList_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles BoundsList.SelectionChanged
        CurrentSelectedBounds = BoundsList.SelectedItem
    End Sub

    Private Sub SpritesList_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles SpritesList.SelectionChanged
        CurrentSelectedSprite = SpritesList.SelectedItem
        LoadMSBData()
    End Sub
    Private Sub ButtonInsertSelected_Click(sender As Object, e As RoutedEventArgs) Handles ButtonInsertSelected.Click
        If CurrentSelectedBounds Is Nothing Then Return
        If CurrentSelectedSprite Is Nothing Then Return
        MainPage.IsFileChanged(True)
        Dim entry As New SpriteBoundDetail
        entry.ImageThumbnail.Source = CurrentSelectedBounds.Thumb
        entry.TextID.Text = CurrentSelectedBounds.BoundsIdentifier
        Dim index = AssignmentList.SelectedIndex + 1
        AssignmentList.Items.Insert(index, entry)
        CurrentSelectedSprite.Source.AddNewDetail(index,
                                           CurrentSelectedBounds.BoundsIdentifier,
                                           New AssignmentPropertyPair(0, 0))
        CurrentSelectedSprite.Update()
        CurrentSelectedSprite.WhichIsNew = index
        LoadMSBData()
        AssignmentList.SelectedIndex = index ' select what we just made
    End Sub

    Private Sub ButtonAddSprite_Click(sender As Object, e As RoutedEventArgs) Handles ButtonAddSprite.Click
        If Editor Is Nothing Then
            Return
        End If
        MainPage.IsFileChanged(True)
        Dim newSpriteObject = Editor.CreateSprite()
        Dim msb As New MultiSpriteBuilder(newSpriteObject)
        SpritesList.Items.Add(msb)
        SpritesList.SelectedItem = msb
        CurrentSelectedSprite = msb
        LoadMSBData()
    End Sub

    Private Shared NewItem As New SolidColorBrush(Windows.UI.Color.FromArgb(30, 50, 200, 200))

    ''' <summary>
    ''' When a MSB is selected, populate the shared controls with its details and the details/assignments
    ''' </summary>
    Private Sub LoadMSBData()
        If CurrentSelectedSprite Is Nothing Then
            AssignmentList.Items.Clear()
            TextboxClass.Text = ""
            TextboxVariant.Text = ""
            RadioSingle.IsChecked = True
            Return
        End If
        TextboxClass.Text = CurrentSelectedSprite.ClassName
        TextboxVariant.Text = CurrentSelectedSprite.VariantName
        Select Case CurrentSelectedSprite.Type
            Case MultiType.Single
                RadioSingle.IsChecked = True
            Case MultiType.Directional
                RadioDirection.IsChecked = True
            Case MultiType.Neighbor
                RadioNeighbor.IsChecked = True
            Case MultiType.Animation
                RadioAnimated.IsChecked = True
        End Select
        AssignmentList.Items.Clear()
        For i = 0 To CurrentSelectedSprite.Source.Bounds.Count - 1
            Dim boundID = CurrentSelectedSprite.Source.Bounds(i)
            Dim assignm = CurrentSelectedSprite.Source.BoundAssignment(i)
            Dim detail = New SpriteBoundDetail
            If CurrentSelectedSprite.WhichIsNew = i Then
                detail.BackgroundBorder.Background = newItem
            End If
            detail.TextID.Text = boundID
            Dim thumbSrc As BoundsThumbViewItem =
                BoundsList.Items.Where(
                    Function(obj As Object) As Boolean ' 
                        If TypeOf obj Is BoundsThumbViewItem Then
                            Return DirectCast(obj, BoundsThumbViewItem).BoundsIdentifier = boundID
                        End If
                        Return False
                    End Function)(0)
            Dim thumb = thumbSrc.Thumb
            detail.ImageThumbnail.Source = thumb
            detail.AssignmentValue = assignm.Assignment
            detail.PropertyValue = assignm.Property
            Dim ii = i
            AddHandler detail.ValuesUpdated,
                Sub()
                    MainPage.IsFileChanged(True)
                    CurrentSelectedSprite.Source.BoundAssignment(ii) =
                        New AssignmentPropertyPair(detail.AssignmentValue, detail.PropertyValue)
                    CurrentSelectedSprite.Update()
                End Sub

            AssignmentList.Items.Add(detail)
        Next
    End Sub

    ''' <summary>
    ''' Changes the description text based on what radio button is selected
    ''' </summary>
    Public Sub TypeChanged()
        If CurrentSelectedSprite Is Nothing Then Return
        MainPage.IsFileChanged(True)
        Dim str = ""
        If RadioSingle.IsChecked Then
            CurrentSelectedSprite.Type = MultiType.Single
            str = "Only the first sprite in the list will be used. This will always generate a static sprite. " &
            "The Assignment and Property fields are always ignored."
        ElseIf RadioDirection.IsChecked Then
            CurrentSelectedSprite.Type = MultiType.Directional
            str = "Use the Assignment field to assign a specific sprite for a given direction. Up to eight are supported; " &
                "if not all eight are provided, only the first four are used, with diagonals defaulting to East/West. " &
                "The Property field is ignored. At minimum, an East-facing image and a West-facing image must be assigned." &
                "The accepted assignments are:" & vbCrLf &
                "0 -> East, 1 -> West," & vbCrLf &
                "2 -> North, 3 -> South," & vbCrLf &
                "4 -> NEast, 5 -> NWest," & vbCrLf &
                "6 -> SEast, 7 -> SWest." & vbCrLf &
                "Missing sprites for North/South are ignored."
        ElseIf RadioNeighbor.IsChecked Then
            CurrentSelectedSprite.Type = MultiType.Neighbor
            str = "Use the Assignment field to provide the string name of another Class which should have " &
                "a specific transition on this sprite's image. The associated image should use transparency to " &
                "reveal the base image. Provide a single sprite with no Assignment to use as the base; failure to provide one will throw an exception." & vbCrLf &
                "Use the Property field to assign an optional direction (leave blank for default, East): " & vbCrLf &
                "(0, 1, 2, 3) -> (E, W, N, S) only; compound directions are not supported. " & vbCrLf &
                "For each expected Class that may occupy a space next to this one, provide overlays" &
                "that, should the named Class occupy the relational direction, the specific sprite should be used;" &
                "only one (Eastward) is absolutely necessary, and will be rotated to fit the correct direction. " &
                "Providing specific overlays for a direction will use that when available. The overlay images " &
                "must be square, as does the base; failure to provide square images or images with the same size throws an exception." & vbCrLf &
                "The returned sprite image will attempt to find matching Class names for all four directions; " &
                "overlays are always drawn NS beneath EW." & vbCrLf &
                "You may provide Class names however you'd like, so long as you match the name with your Token's N_Select() call."
        ElseIf RadioAnimated.IsChecked Then
            CurrentSelectedSprite.Type = MultiType.Animation
            str = "The sprites will be displayed in the order they are listed. Use the Assignment field to " &
                "assign a common descriptor for a series of sprites. Sprites with the same Assignment name " &
                "will be played in the order they are found, when the Assignment name is selected. " &
                "Use the Property field to describe the length of time in milliseconds that the sprite should " &
                "remain displayed. Animations always loop; set one sprite with an empty Property field to use as " &
                "the static alternative."
        End If
        TextTypeDescriptor.Text = str
        CurrentSelectedSprite.Update()
    End Sub
    ''' <summary>
    ''' Changes the name to what is new
    ''' </summary>
    Public Sub DetailNameChanged()
        If CurrentSelectedSprite Is Nothing Then Return
        MainPage.IsFileChanged(True)
        CurrentSelectedSprite.ClassName = TextboxClass.Text
        CurrentSelectedSprite.VariantName = TextboxVariant.Text
        CurrentSelectedSprite.Update()
    End Sub

    Private Sub ButtonRemoveSprite_Click(sender As Object, e As RoutedEventArgs)
        If CurrentSelectedSprite Is Nothing Then Return
        Dim sel = SpritesList.SelectedIndex
        Editor.Sprites.Remove(CurrentSelectedSprite.Source)
        SpritesList.Items.Remove(CurrentSelectedSprite)
        ReloadAll()
        LoadMSBData()
    End Sub

    Private Sub ButtonRemoveDetail_Click(sender As Object, e As RoutedEventArgs)
        If CurrentSelectedSprite Is Nothing Then Return
        MainPage.IsFileChanged(True)
        Dim sel = AssignmentList.SelectedIndex
        If sel = -1 Then Return
        AssignmentList.Items.RemoveAt(sel)
        CurrentSelectedSprite.Source.RemoveDetail(sel)
        LoadMSBData()
    End Sub


End Class

Public Class BoundsThumbViewItem
    Inherits BoundsListViewItem
    Public Thumb As BitmapImage
    Public Sub New(id As Integer, rect As Func(Of Integer, Rect), imageStream As IRandomAccessStream)
        MyBase.New(id, rect)
        Thumb = New BitmapImage()
        Thumb.DecodePixelWidth = rect(id).Width
        Thumb.SetSource(imageStream)
        Dim thumbobj = New BoundThumb
        thumbobj.Thumb.Source = Thumb
        thumbobj.TextBoundID.Text = id
        thumbobj.TextBoundInfo.Text = rect(id).ToString
        Me.Content = thumbobj
    End Sub
End Class

''' <summary>
''' Contains information about the bounds and properties to create a sprite from.
''' </summary>
Public Class MultiSpriteBuilder
    Inherits ListViewItem

    ''' <summary>
    ''' the index of any newly inserted detail
    ''' </summary>
    Public WhichIsNew As Integer = -1

    Public Source As MultiSpriteData

    Private Block As New TextBlock
    Private Shared _FontFamily = New FontFamily("Consolas")
    Private Shared _FontSize As Integer = 14
    Private Shared _Foreground = New SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255))

    Public Sub New(spriteDataFromEditor As MultiSpriteData)
        Source = spriteDataFromEditor
        Block.FontFamily = _FontFamily
        Block.FontSize = _FontSize
        Block.Foreground = _Foreground
        Content = Block : Update()
    End Sub

    Public Sub Update()
        Source.VariantName = Source.VariantName.Replace(" ", "").ToUpper
        Source.ClassName = Source.ClassName.Replace(" ", "").ToUpper
        Block.Text = $"[{Source.Type.ToString()(0)}{Source.Bounds.Count}] " &
            $"{Source.ClassName}{If(Source.VariantName <> "", $":{Source.VariantName}", "")}"
    End Sub

    Public Property ClassName As String
        Get
            Return Source.ClassName
        End Get
        Set(value As String)
            Source.ClassName = value
        End Set
    End Property
    Public Property VariantName As String
        Get
            Return Source.VariantName
        End Get
        Set(value As String)
            Source.VariantName = value
        End Set
    End Property
    Public Property Type As MultiType
        Get
            Return Source.Type
        End Get
        Set(value As MultiType)
            Source.Type = value
        End Set
    End Property

End Class

