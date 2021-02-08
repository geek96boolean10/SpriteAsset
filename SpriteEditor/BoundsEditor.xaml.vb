' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409
Imports SpriteAsset
Imports Microsoft.Graphics.Canvas
Imports System.Numerics
''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class BoundsEditor
    Inherits Page

    Private Ticker As New DispatcherTimer()

    Private Image As CanvasBitmap
    Private ZoomLevel As Double = 5
    Private PanBy As Vector2 = Vector2.Zero
    Private NextID As Integer = 0

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
        Me.MainPage = e.Parameter
    End Sub

    Public Sub ReloadBounds()
        If Editor Is Nothing Then Return
        BoundsList.Items.Clear()
        Dim bd As List(Of Rect) = Nothing
        Dim id As List(Of Integer) = Nothing
        Dim c = Editor.GetAllBounds(id, bd)
        For i = 0 To c - 1
            Dim newitem = New BoundsListViewItem(id(i), AddressOf Editor.GetBounds)
            AddHandler newitem.DoubleTapped, Sub()
                                                 PanToSelection(newitem.Bounds)
                                             End Sub
            BoundsList.Items.Add(newitem)
            BoundsList.SelectedItem = newitem
        Next
    End Sub

    Private Sub Canvas_CreateResources(sender As UI.Xaml.CanvasControl, args As UI.CanvasCreateResourcesEventArgs)
        Ticker.Interval = New TimeSpan(0, 0, 0, 0, 1000 / 60) ' 60fps max
        AddHandler Ticker.Tick, AddressOf Tick
        Ticker.Start()
    End Sub

    Private Sub Tick()
        Canvas.Invalidate()
    End Sub

    Private boundsRectBrush As Brushes.CanvasSolidColorBrush
    Private showCurrentPixel As Boolean = True
    Private Sub Canvas_Draw(sender As UI.Xaml.CanvasControl, args As UI.Xaml.CanvasDrawEventArgs)
        If Editor Is Nothing Then Return
        Dim Zoom As Matrix3x2 = Matrix3x2.CreateScale(ZoomLevel)
        Dim Pan As Matrix3x2 = Matrix3x2.CreateTranslation(PanBy)
        Dim Transform = Matrix3x2.Multiply(Zoom, Pan)

        Dim session = args.DrawingSession
        session.Antialiasing = CanvasAntialiasing.Aliased
        session.Transform = Transform

        ' Draw the spritesheet image
        session.DrawImage(Editor.Image, Editor.Image.Bounds, Editor.Image.Bounds, 1, interpolation:=CanvasImageInterpolation.NearestNeighbor)

        Dim boundIDs As List(Of Integer) = Nothing
        Dim boundRects As List(Of Rect) = Nothing
        Dim count = Editor.GetAllBounds(boundIDs, boundRects)

        ' Draw the known bounds within the Editor
        For i = 0 To count - 1
            Dim rectColor = Windows.UI.Color.FromArgb(100, 50, 50, 255)
            ' If rect is selected, use different color
            If SelectedBounds IsNot Nothing AndAlso boundRects(i).Equals(SelectedBounds.Bounds) Then
                rectColor = Windows.UI.Color.FromArgb(120, 50, 255, 255)
            End If
            session.DrawRectangle(boundRects(i), rectColor, 1)
            ' If mouseover, then highlight and show ID number
            If boundRects(i).Contains(CursorCurrentPixel.ToPoint) Then
                session.FillRectangle(boundRects(i), Windows.UI.Color.FromArgb(60, 128, 128, 128))
                session.DrawText(boundIDs(i), New Vector2(boundRects(i).X + 2, boundRects(i).Y + 2), Windows.UI.Colors.White,
                                 New Text.CanvasTextFormat() With {.FontFamily = "Consolas", .FontSize = 24 / ZoomLevel})
            End If
        Next

        ' Show current mouse pixel bounds
        If showCurrentPixel Then
            session.FillRectangle(New Rect(Math.Floor(CursorCurrentPixel.X), Math.Floor(CursorCurrentPixel.Y), 1, 1), Windows.UI.Color.FromArgb(50, 0, 200, 255))
        End If

        ' Show working autobounds
        If AutoBoundsWorking Then
            session.FillRectangle(AutoBounds, Windows.UI.Color.FromArgb(100, 200, 80, 40))
            session.DrawText(AutoBounds.ToString, New Vector2(AutoBounds.X + 1, AutoBounds.Y + 1), Windows.UI.Colors.White,
                                 New Text.CanvasTextFormat() With {.FontFamily = "Consolas", .FontSize = 24 / ZoomLevel})
        End If

        ' Show some info
        TextZoomPan.Text = $"Pan: {Math.Floor(PanBy.X):F2}, {Math.Floor(PanBy.Y):F2} | Zoom: {ZoomLevel:F1}x "
        TextCursorPosition.Text = $"Cursor: {Math.Floor(CursorCurrentPixel.X),4}, {Math.Floor(CursorCurrentPixel.Y),4}"
    End Sub

    Private CursorCurrentPixel As Vector2

    Private Sub Canvas_PointerWheelChanged(sender As Object, e As PointerRoutedEventArgs)
        Dim pt = e.GetCurrentPoint(Canvas)
        ' change zoom level but also offset the pan based on center of field
        ' panby describes onscreen-location of pixel (0,0) of the bitmap

        Dim delta = e.GetCurrentPoint(Canvas).Properties.MouseWheelDelta / 120.0 / 5 ' deltas of 0.2 per notch
        Dim deltaSign = delta / Math.Abs(delta)
        If ZoomLevel >= 4 Then delta = deltaSign * 0.5 ' deltas of 0.5 per notch
        If ZoomLevel >= 16 Then delta = deltaSign * 1
        If ZoomLevel <= 1 Then delta = deltaSign * 0.1   ' deltas of 0.1 per notch

        ZoomLevel += delta
        If ZoomLevel <= 0.5 Then ' no smaller than 1/2
            ZoomLevel = 0.5
            delta = 0
        ElseIf ZoomLevel >= 32 Then ' hard lock to 32x
            ZoomLevel = 32
            delta = 0
        End If

        PanBy -= delta * CursorCurrentPixel
    End Sub

    Private mouseIsDown As Boolean = False
    Private mouseDownSource As Point
    Private beforeMouseDownPan As Vector2
    Private Sub Canvas_PointerMoved(sender As Object, e As PointerRoutedEventArgs)
        Dim pt = e.GetCurrentPoint(Canvas)
        CursorCurrentPixel = (New Vector2(pt.Position.X, pt.Position.Y) - PanBy) / ZoomLevel

        If Not mouseIsDown AndAlso pt.Properties.IsRightButtonPressed Then ' right-click was just pressed
            mouseIsDown = True
            mouseDownSource = pt.Position
            beforeMouseDownPan = PanBy
        ElseIf mouseIsDown AndAlso pt.Properties.IsRightButtonPressed Then ' right-click is held down
            PanBy = (beforeMouseDownPan + New Vector2(pt.Position.X - mouseDownSource.X,
                                 pt.Position.Y - mouseDownSource.Y)) '/ ZoomLevel
        ElseIf mouseIsDown AndAlso Not pt.Properties.IsRightButtonPressed Then ' right-click was released
            mouseIsDown = False
        End If

        ' If autobounds in progress, update
        If AutoBoundsWorking Then
            AutoBounds.Width = Math.Max(0, Math.Ceiling(CursorCurrentPixel.X - AutoBounds.X))
            AutoBounds.Height = Math.Max(0, Math.Ceiling(CursorCurrentPixel.Y - AutoBounds.Y))
        End If
    End Sub

    Private SelectedBounds As BoundsListViewItem
    Private Sub ButtonAddBounds_Click(sender As Object, e As RoutedEventArgs)
        If Editor Is Nothing Then
            MainPage.LoadBitmap(Nothing, Nothing)
            If Editor Is Nothing Then 'didn't pick a file
                Return
            End If
        End If
        AddNewBounds()
    End Sub

    Private Sub AddNewBounds()
        AddNewBounds(New Rect(0, 0, 16, 16))
    End Sub

    Private Sub AddNewBounds(at As Rect)
        MainPage.IsFileChanged(True)
        Dim newID = Editor.NextID
        Dim newB = at
        If Editor.AddBounds(newID, newB) Then
            Dim newitem = New BoundsListViewItem(newID, AddressOf Editor.GetBounds)
            AddHandler newitem.DoubleTapped, Sub()
                                                 PanToSelection(newitem.Bounds)
                                             End Sub
            BoundsList.Items.Add(newitem)
            BoundsList.SelectedItem = newitem
        End If
        MainPage.CheckBoundCount()
    End Sub

    Private invalidNumber As New SolidColorBrush(Windows.UI.Colors.Maroon)
    Private acceptableNumber As New SolidColorBrush(Windows.UI.Color.FromArgb(104, 255, 255, 255))
    Private Sub ButtonUpdateBounds_Click(sender As Object, e As RoutedEventArgs)
        If SelectedBounds Is Nothing Then Return
        MainPage.IsFileChanged(True)
        Dim newBounds = Editor.GetBounds(SelectedBounds.BoundsIdentifier)
        If Not Integer.TryParse(TextBoundsX.Text, newBounds.X) Then TextBoundsX.Background = invalidNumber Else TextBoundsX.Background = acceptableNumber
        If Not Integer.TryParse(TextBoundsY.Text, newBounds.Y) Then TextBoundsY.Background = invalidNumber Else TextBoundsY.Background = acceptableNumber
        If Not UInteger.TryParse(TextBoundsW.Text, newBounds.Width) Then TextBoundsW.Background = invalidNumber Else TextBoundsW.Background = acceptableNumber
        If Not UInteger.TryParse(TextBoundsH.Text, newBounds.Height) Then TextBoundsH.Background = invalidNumber Else TextBoundsH.Background = acceptableNumber
        Editor.AddBounds(SelectedBounds.BoundsIdentifier, newBounds)
        SelectedBounds.RefreshContent()
    End Sub

    Public Sub ResetRemovalSelection()
        removeReferences.Clear()
        ButtonRemoveBoundsConfirm.IsEnabled = False
        ButtonRemoveBoundsConfirm.Opacity = 0
        TextRemoveBounds.Text = ""
    End Sub

    Dim removeReferences As New List(Of MultiSpriteData)
    Private Sub ButtonRemoveBounds_Click(sender As Object, e As RoutedEventArgs)
        If SelectedBounds Is Nothing Then Return
        ' check if the bounds is referenced
        For Each msd In Editor.Sprites
            If msd.Bounds.Contains(SelectedBounds.BoundsIdentifier) Then
                removeReferences.Add(msd)
            End If
        Next
        If removeReferences.Count > 0 Then
            Dim s = $"Bounds {SelectedBounds.BoundsIdentifier} is used {removeReferences.Count} time(s). " &
                        "It will be removed from:" & vbCrLf
            For Each ref In removeReferences
                s += "> " & ref.ClassName + If(ref.VariantName <> "", ":" & ref.VariantName, "") & vbCrLf
            Next
            s += "Confirm removal?"
            TextRemoveBounds.Text = s
            ButtonRemoveBoundsConfirm.IsEnabled = True
            ButtonRemoveBoundsConfirm.Opacity = 1
        Else 'no uses
            ButtonRemoveBoundsConfirm_Click(Nothing, Nothing)
        End If
    End Sub

    Private Sub ButtonRemoveBoundsConfirm_Click(sender As Object, e As RoutedEventArgs)
        If SelectedBounds Is Nothing Then Return
        MainPage.IsFileChanged(True)
        ' remove
        Editor.RemoveBounds(SelectedBounds.BoundsIdentifier)
        BoundsList.Items.Remove(SelectedBounds)
        For Each msd In removeReferences
            Dim indexOfSelectedBounds = msd.Bounds.IndexOf(SelectedBounds.BoundsIdentifier)
            While indexOfSelectedBounds <> -1 ' make sure we remove all
                msd.RemoveDetail(indexOfSelectedBounds)
                indexOfSelectedBounds = msd.Bounds.IndexOf(SelectedBounds.BoundsIdentifier)
            End While
        Next
        ResetRemovalSelection()
    End Sub

    Private Sub Canvas_PointerPressed(sender As Object, e As PointerRoutedEventArgs)
        If Editor Is Nothing Then Return
        Dim pt = e.GetCurrentPoint(Canvas)
        If pt.Properties.IsLeftButtonPressed Then
            Dim bounds As List(Of Rect) = Nothing
            Dim ids As List(Of Integer) = Nothing
            Dim count = Editor.GetAllBounds(ids, bounds)
            For i = 0 To count - 1
                If bounds(i).Contains(CursorCurrentPixel.ToPoint) Then
                    Dim ii = i
                    Dim newSelect = BoundsList.Items.First(Function(enter As Object) As Boolean
                                                               If TypeOf enter Is BoundsListViewItem Then
                                                                   Return DirectCast(enter, BoundsListViewItem).BoundsIdentifier = ids(ii)
                                                               End If
                                                               Return False
                                                           End Function)
                    If newSelect IsNot Nothing Then
                        BoundsList.SelectedItem = newSelect
                        Exit For
                    End If
                End If
            Next
        End If
    End Sub

    Private Sub BoundsList_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        ResetRemovalSelection()
        SelectedBounds = BoundsList.SelectedItem()
        If SelectedBounds IsNot Nothing Then
            TextBoundsX.Text = SelectedBounds.Bounds.X
            TextBoundsY.Text = SelectedBounds.Bounds.Y
            TextBoundsW.Text = SelectedBounds.Bounds.Width
            TextBoundsH.Text = SelectedBounds.Bounds.Height
        End If
    End Sub

    Private Sub ButtonResetZoomPan_Click(sender As Object, e As RoutedEventArgs)
        PanBy = Vector2.Zero
        ZoomLevel = 5
    End Sub

    Private Sub ButtonTogglePixel_Click(sender As Object, e As RoutedEventArgs)
        showCurrentPixel = Not showCurrentPixel
    End Sub

    Private Sub PanToSelection(targetBounds As Rect)
        Dim targetMid = New Vector2(targetBounds.X + targetBounds.Width / 2,
                                    targetBounds.Y + targetBounds.Height / 2) * ZoomLevel
        Dim canvasMid = New Vector2(Canvas.ActualWidth / 2, Canvas.ActualHeight / 2)
        PanBy = -1 * (targetMid - canvasMid)
    End Sub

    Private AutoBounds As Rect
    Private AutoBoundsWorking As Boolean = False
    ''' <summary>
    ''' Start a new bounds at cursor position. After that, left-click to accept bounds at new location.
    ''' </summary>
    Private Sub Canvas_DoubleTapped(sender As Object, e As DoubleTappedRoutedEventArgs)
        AutoBoundsWorking = True
        AutoBounds = New Rect(Math.Floor(CursorCurrentPixel.X), Math.Floor(CursorCurrentPixel.Y), 1, 1)
    End Sub
    Private Sub Canvas_Tapped(sender As Object, e As TappedRoutedEventArgs)
        If AutoBoundsWorking Then
            AutoBoundsWorking = False
            If AutoBounds.Width = 0 OrElse AutoBounds.Height = 0 Then
                Return
            Else
                AddNewBounds(AutoBounds)
                AutoBounds = Nothing
            End If
        End If
    End Sub
End Class


Public Class BoundsListViewItem
    Inherits ListViewItem
    Private Shared TextColor As New SolidColorBrush(Windows.UI.Colors.White)
    ''' <summary>
    ''' The ID of the bounds within the complex sheet.
    ''' </summary>
    Public BoundsIdentifier As Integer
    Private BoundsLink As Func(Of Integer, Rect)
    Public Sub New(id As Integer, rect As Func(Of Integer, Rect))
        BoundsIdentifier = id
        BoundsLink = rect
        Content = New TextBlock() With {.Text = Me.ToString(), .Foreground = TextColor}
    End Sub
    Public ReadOnly Property Bounds As Rect
        Get
            Return BoundsLink(BoundsIdentifier)
        End Get
    End Property
    Public Overrides Function ToString() As String
        Return BoundsIdentifier & ": " & Bounds.ToString
    End Function
    Public Sub RefreshContent()
        DirectCast(Content, TextBlock).Text = Me.ToString
    End Sub

End Class
