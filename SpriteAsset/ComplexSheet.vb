Imports Microsoft.Graphics.Canvas
Imports Newtonsoft.Json.Linq

''' <summary>
''' A bitmap that is not a single static image. Provides tools to interpret
''' a spritesheet bitmap in a variety of formats. Associated with a single bitmap file,
''' and when processed, produces a number of Sprite objects that can be used.
''' </summary>
Public Class ComplexSheet

    Public ReadOnly BitmapName As String
    Private _nextIDValue As Integer = 0
    Public Property Image As CanvasBitmap
    Public Property ImageBounds As Rect
    ''' <summary>
    ''' A collection of bounds that each represent a different complete image.
    ''' </summary>
    Private Bounds As New Dictionary(Of Integer, Rect)
    ''' <summary>
    ''' All known multisprites found in this sheet.
    ''' </summary>
    Public Sprites As New List(Of MultiSpriteData)

    ''' <summary>
    ''' Initializes a sprite with the given base filename.
    ''' </summary>
    Public Sub New(filename As String, image As CanvasBitmap)
        BitmapName = filename.Substring(filename.LastIndexOf("\") + 1)
        Me.Image = image
        ImageBounds = image.Bounds
    End Sub

    Public Sub New(data As JObject)
        BitmapName = data("FileName")
        _nextIDValue = data("NextIDValue")
        ImageBounds = ArrayToRect(data("ImageBounds"))
        Dim boundsarray As JArray = data("Bounds")
        For Each bound In boundsarray
            Dim id As Integer = bound("ID")
            Dim rectj As JArray = bound("Rect")
            Dim rect = ArrayToRect(rectj)
            Bounds.Add(id, rect)
        Next
        Dim format As Windows.Graphics.DirectX.DirectXPixelFormat =
            [Enum].Parse(
                GetType(Windows.Graphics.DirectX.DirectXPixelFormat),
                data("ImageFormat"))
        Image = CanvasBitmap.CreateFromBytes(
            CanvasDevice.GetSharedDevice,  'Convert.FromBase64String(data("ImageData")),
            CType(data("ImageData"), Byte()),
            CType(ImageBounds.Width, Integer),
            CType(ImageBounds.Height, Integer),
            format)

        Dim spriteData As JArray = data("Sprites")
        For Each elem In spriteData
            Dim d = New MultiSpriteData(elem)
            Sprites.Add(d)
        Next
    End Sub

    Public Function Serialize() As JObject
        Dim data As New JObject
        data.Add("FileName", BitmapName)
        data.Add("NextIDValue", _nextIDValue)
        Dim boundsarray As New JArray
        For Each b In Bounds
            Dim bn As New JObject
            bn.Add("ID", b.Key)
            bn.Add("Rect", RectToArray(b.Value))
            boundsarray.Add(bn)
        Next
        data.Add("Bounds", boundsarray)
        data.Add("ImageFormat", Image.Format.ToString)
        data.Add("ImageData", Image.GetPixelBytes) ' Convert.ToBase64String(Image.GetPixelBytes()))
        data.Add("ImageBounds", RectToArray(ImageBounds))

        Dim spriteData As New JArray
        For Each s In Sprites
            Dim sprData = s.Serialize()
            spriteData.Add(sprData)
        Next
        data.Add("Sprites", spriteData)
        Return data
    End Function

    Public Shared Function RectToArray(rect As Rect) As JArray
        Dim r As New JArray
        r.Add(rect.X)
        r.Add(rect.Y)
        r.Add(rect.Width)
        r.Add(rect.Height)
        Return r
    End Function
    Public Shared Function ArrayToRect(rectj As JArray) As Rect
        Dim rect = New Rect(rectj(0), rectj(1), rectj(2), rectj(3))
        Return rect
    End Function

    ''' <summary>
    ''' Retrieves the next ID in sequence that's guaranteed to be free.
    ''' </summary>
    ''' <remarks>You get 2 billion IDs. Don't waste 'em!</remarks>
    Public ReadOnly Property NextID As Integer
        Get
            _nextIDValue += 1
            Return _nextIDValue - 1
        End Get
    End Property

    Public Function CreateSprite() As MultiSpriteData
        Dim msd As New MultiSpriteData()
        Sprites.Add(msd)
        Return msd
    End Function

    ''' <summary>
    ''' Adds a new bounds or sets a known bounds to the collection.
    ''' Returns true if successful. Fails if:
    ''' 1) the bounds are zero; 2) the bounds exceed the canvas.
    ''' </summary>
    Public Function AddBounds(id As Integer, bounds As Rect) As Boolean
        If bounds.Width = 0 OrElse bounds.Height = 0 Then Return False
        If Not Me.ImageBounds.Contains(New Point(bounds.X, bounds.Y)) OrElse
            Not Me.ImageBounds.Contains(New Point(bounds.Right - 1, bounds.Bottom - 1)) Then
            Return False
        End If
        If Not Me.Bounds.ContainsKey(id) Then
            Me.Bounds.Add(id, bounds)
        Else
            Me.Bounds(id) = bounds
        End If
        Return True
    End Function

    ''' <summary>
    ''' Returns separate lists for the IDs and Bounds (each pair exists at the same index). 
    ''' Also returns the number of bounds in the list.
    ''' </summary>
    Public Function GetAllBounds(ByRef IDs As List(Of Integer), ByRef Bounds As List(Of Rect)) As Integer
        IDs = New List(Of Integer)
        Bounds = New List(Of Rect)
        For Each k In Me.Bounds.Keys
            IDs.Add(k)
            Bounds.Add(Me.Bounds(k))
        Next
        Return Me.Bounds.Count
    End Function

    Public Function GetBounds(id As Integer) As Rect
        If Bounds.ContainsKey(id) Then
            Return Bounds(id)
        Else
            Return New Rect(0, 0, 0, 0)
        End If
    End Function

    Public Function RemoveBounds(id As Integer) As Boolean
        Return Bounds.Remove(id)
    End Function

    Public ReadOnly Property BoundsCount As Integer
        Get
            Return Bounds.Count
        End Get
    End Property

End Class