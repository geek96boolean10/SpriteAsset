Imports System.Numerics
Imports Microsoft.Graphics.Canvas
Imports SpriteAsset

''' <summary>
''' Indicates the class accepts timing information.
''' </summary>
Public Interface ITakesTime
    Sub [Step](ms As Double)
End Interface

''' <summary>
''' A helpful class that provides layer sorting, offset coordinates, and so on. Used in place
''' of manually creating and managing separate CanvasRenderTargets and DrawingSessions with differing
''' origins and scales.
''' </summary>
Public Class SceneSession
    Implements ITakesTime

    Private Layers As New SortedDictionary(Of Integer, SingleLayer)
    Private Transform As Matrix3x2
    Private Inverse As Matrix3x2

    ''' <summary>
    ''' This constructor should only be ever called once; to draw a new scene,
    ''' use the Refresh method instead.
    ''' </summary>
    Public Sub New()
        Transform = Matrix3x2.Identity
        Matrix3x2.Invert(Transform, Inverse)
    End Sub

    ''' <summary>
    ''' Begins a new draw of the scene. Follow this method call with subsequent drawing methods by
    ''' accessing different layers. <!--End the scene's drawing by calling the 'Finish' method.-->
    ''' Note that layers that pre-process their own content, like SpriteLayer, will
    ''' immediately draw their content to their render targets.
    ''' </summary>
    ''' <param name="primary">
    ''' The CanvasDrawingSession given by the Canvas object for this frame.
    ''' All layers will be painted onto this session.
    ''' </param>
    ''' <param name="primaryBounds">
    ''' The Bounds of the Canvas object's supposed drawn area.
    ''' </param>
    Public Sub Start(primary As CanvasDrawingSession, primaryBounds As Rect)
        primary.Transform = Transform
        For Each kv In Layers
            Dim l = kv.Value
            l.Reset()
            primary.DrawImage(l.Render,
                              primaryBounds.X + l.Offset.X,
                              primaryBounds.Y + l.Offset.Y,
                              sourceRectangle:=l.Bounds,
                              opacity:=1,
                              interpolation:=CanvasImageInterpolation.NearestNeighbor)
        Next
    End Sub

    ''' <summary>
    ''' Ends the current draw of the scene.
    ''' </summary>
    <Obsolete> Public Sub Finish()
        For Each l In Layers
            'l.Session.Dispose()
        Next
    End Sub

    ''' <summary>
    ''' Sets the transform to be applied for every Refresh() call.
    ''' </summary>=
    Public Sub SetTransform(matrix As Matrix3x2)
        Transform = matrix
        If Not Matrix3x2.Invert(Transform, Inverse) Then
            Throw New Exception
        End If
    End Sub

    ''' <summary>
    ''' Access layers by their index or by using the DefaultLayers enum.
    ''' Layer() returns the entire layer; primary use is to utilize its DoOff method.
    ''' </summary>
    Public ReadOnly Property Layer(index As Integer) As SingleLayer
        Get
            Return Layers(index)
        End Get
    End Property
    ''' <summary>
    ''' Access layers by their index or by using the DefaultLayers enum.
    ''' Session() only returns the drawing session of the given layer.
    ''' </summary>
    Public ReadOnly Property Session(index As Integer) As CanvasDrawingSession
        Get
            Return Layers(index).Session
        End Get
    End Property

    ''' <summary>
    ''' Appends a layer to the scene. Layers may not be reordered afterwards.
    ''' Higher values for index are rendered later. Any index may be inserted at,
    ''' but an exception is thrown if an index already exists.
    ''' Refuses to accept null layers.
    ''' </summary>
    Public Sub AddLayer(index As Integer, layer As SingleLayer)
        If layer Is Nothing Then Return
        If Layers.ContainsKey(index) Then
            Throw New Exception("Layer " & index & " already exists!")
        End If
        Layers.Add(index, layer)
    End Sub

    ''' <summary>
    ''' Attempts to click on an object by seeking through the known layers, 
    ''' starting at the highest value and going in reverse.
    ''' </summary>
    Public Sub TryClick(mouse As Vector2, button As Clickable.MouseButton)
        Dim corrected As New Vector2(
            mouse.X * Inverse.M11 + mouse.Y * Inverse.M21 + 1 * Inverse.M31,
            mouse.X * Inverse.M12 + mouse.Y * Inverse.M22 + 1 * Inverse.M32
        )
        'Console.WriteLine($"Mouse {mouse} corrected {corrected}")
        For Each l In Layers.Reverse
            Dim attempt = l.Value.TryClick(corrected, button)
            If Not attempt Then
                Continue For
            Else
                Exit For
            End If
        Next
    End Sub

    Public Sub [Step](ms As Double) Implements ITakesTime.Step
        For Each lay In Layers
            Dim layer = lay.Value
            If TypeOf layer Is ITakesTime Then
                DirectCast(layer, ITakesTime).Step(ms)
            End If
        Next
    End Sub

    Public Enum DefaultLayers
        SpaceTiles = 0
        UI = 100
    End Enum
End Class

''' <summary>
''' A layer consisting of its own RenderTarget and DrawingSession, with additional details
''' as to how it should be rendered onto the final scene. Click bounds can be enabled.
''' </summary>
Public Class SingleLayer

    Public Offset As Vector2
    Protected RenderTarget As CanvasRenderTarget
    Protected DrawingSession As CanvasDrawingSession
    Protected BoundRect As Rect
    Protected InternalOrigin As Vector2
    Protected ClickBounds As New List(Of Clickable)
    Protected SourceUnit As Integer

    ''' <summary>
    ''' A layer of precisely the given size, at an optional offset.
    ''' The offset is applied on the entire layer when drawn on the main session.
    ''' </summary>
    Public Sub New(size As Size, unit As Integer, Optional offset As Vector2 = Nothing)
        SourceUnit = unit
        Me.Offset = offset
        RenderTarget = New CanvasRenderTarget(
            CanvasDevice.GetSharedDevice(forceSoftwareRenderer:=False), size.Width, size.Height, 96)
        DrawingSession = RenderTarget.CreateDrawingSession()
        DrawingSession.Antialiasing = CanvasAntialiasing.Aliased
        BoundRect = New Rect(0, 0, size.Width, size.Height)
        InternalOrigin = New Vector2(0, 0)
    End Sub

    ''' <summary>
    ''' A layer with the given inner size, with a border around the edge of the given expansion.
    ''' When drawn, the inner size is used as the origin, plus the optional offset. Expanded areas
    ''' are still drawn to the session, unless disabled.
    ''' </summary>
    Public Sub New(actualSize As Size, expansion As UInteger, unit As Integer, Optional offset As Vector2 = Nothing, Optional hideExpanse As Boolean = False)
        Me.New(
            New Size(
                actualSize.Width + 2 * expansion,
                actualSize.Height + 2 * expansion),
            unit,
            New Vector2(-expansion, -expansion) + offset)
        If hideExpanse Then
            ' replace default bounding
            BoundRect = New Rect(expansion, expansion, actualSize.Width, actualSize.Height)
            ' also alter offset to remove the expansion
            Me.Offset += New Vector2(expansion, expansion)
        End If
        InternalOrigin = New Vector2(expansion, expansion)
    End Sub

    ''' <summary>
    ''' Must be called each frame, prior to painting this layer, to commit
    ''' the session changes to the render target.
    ''' Forces the creation of a new DrawingSession after first Disposing
    ''' of the previous instance.
    ''' </summary>
    Public Overridable Sub Reset()
        DrawingSession.Dispose()
        DrawingSession = RenderTarget.CreateDrawingSession()
        DrawingSession.Antialiasing = CanvasAntialiasing.Aliased
        DrawingSession.Clear(New Vector4(0, 0, 0, 0))
    End Sub
    ''' <summary>
    ''' Retrieves the current session that allows modification of this Layer.
    ''' Commit all changes with Reset().
    ''' </summary>
    Public Overridable ReadOnly Property Session() As CanvasDrawingSession
        Get
            Return DrawingSession
        End Get
    End Property
    ''' <summary>
    ''' Retrieves the image that should be painted onto the session.
    ''' Call Reset() before calling Render() to ensure all DrawingSession changes
    ''' are commited to this RenderTarget.
    ''' </summary>
    Public Overridable ReadOnly Property Render() As CanvasRenderTarget
        Get
            Return RenderTarget
        End Get
    End Property

    ''' <summary>
    ''' Returns the rect that defines the drawable area of this layer,
    ''' ignoring any pixels outside of this Rect.
    ''' </summary>
    Public ReadOnly Property Bounds() As Rect
        Get
            Return BoundRect
        End Get
    End Property

    ''' <summary>
    ''' Indicates if there is at least once clickable object in this layer.
    ''' </summary>
    Public ReadOnly Property Clickable() As Boolean
        Get
            Return ClickBounds.Count > 0
        End Get
    End Property

    ''' <summary>
    ''' Returns a new Vector2 that obeys the internal offset
    ''' of the layer, if an expansion border is present.
    ''' </summary>
    Public Function DoOff(x As Double, y As Double) As Vector2
        Return New Vector2(x, y) + InternalOrigin
    End Function
    Public Function DoOff(ini As Vector2) As Vector2
        Return ini + InternalOrigin
    End Function

    ''' <summary>
    ''' Adds a bounds to the layer. Do not repeatedly add the same bounds; do it once
    ''' and maintain it with its associated object as much as possible.
    ''' </summary>
    Public Sub AddClickBounds(bounds As Clickable)
        ClickBounds.Add(bounds)
    End Sub
    Public Sub RemoveClickBounds(bounds As Clickable)
        ClickBounds.Remove(bounds)
    End Sub

    Public Sub ClearClickBounds()
        ClickBounds.Clear()
    End Sub

    ''' <summary>
    ''' Attempts to find a Clickable boundary that corresponds to the primary session's
    ''' coordinates. Returns True if a clickable has responded.
    ''' </summary>
    Public Function TryClick(point As Vector2, button As Clickable.MouseButton) As Boolean
        If Not Clickable Then Return False
        For Each cl In ClickBounds
            If cl.ClickBounds.Contains(point.ToPoint) Then
                Dim attempt = cl.Click(button)
                If Not attempt Then
                    Continue For
                Else
                    Return True
                End If
            End If
        Next
        Return False
    End Function

End Class


