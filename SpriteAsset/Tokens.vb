Imports Microsoft.Graphics.Canvas
Imports Windows.Graphics.DirectX
Imports Windows.Graphics.Imaging
Imports Windows.Storage.Streams
''' <summary>
''' Describes information required to retrieve an image from a SpriteBundle.
''' </summary>
Public Class Token
    ''' <summary>
    ''' The Bundle which created this Token, and for which the data in the Token is valid.
    ''' </summary>
    Public ReadOnly Source As SpriteBundle

    Private _AssignmentName = "", _PropertyName = ""
    ''' <summary>
    ''' The Assignment name to seek, as determined by a Selector function.
    ''' </summary>
    Public ReadOnly Property AssignmentName As String
        Get
            Return _AssignmentName
        End Get
    End Property
    ''' <summary>
    ''' The Property name to seek, as determined by a Selector function.
    ''' </summary>
    Public ReadOnly Property PropertyName As String
        Get
            Return _PropertyName
        End Get
    End Property

    Public Sub New(source As SpriteBundle)
        Me.Source = source
    End Sub

    ''' <summary>
    ''' Amount of elapsed time in milliseconds.
    ''' Only used for Animation-type sprites.
    ''' </summary>
    Public A_ElapsedTimeMS As Double = 0
    ''' <summary>
    ''' A list of all known, allowed animation names. 
    ''' Discovered during SpriteBundle formation.
    ''' Do not modify during runtime.
    ''' </summary>
    Public A_AllowedNames As String()

    ''' <summary>
    ''' Increments the internal timer in milliseconds
    ''' for an Animation-type Token.
    ''' </summary>
    Public Sub A_Time(addTimeMS As Double)
        If Source.Type <> MultiType.Animation Then Throw New Exception($"A_ selector can't be used on {Source.Type.ToString()} source")
        A_ElapsedTimeMS += addTimeMS
    End Sub
    ''' <summary>
    ''' Changes the selected animation assignment name
    ''' for an Animation-type Token. Resets time.
    ''' Returns true if the name is valid; false otherwise.
    ''' If the name is invalid, the time is still reset.
    ''' </summary>
    Public Function A_Select(name As String, Optional reset As Double = 0) As Boolean
        If Source.Type <> MultiType.Animation Then Throw New Exception($"A_ selector can't be used on {Source.Type.ToString()} source")
        A_ElapsedTimeMS = reset
        If A_AllowedNames.Contains(name) Then
            _AssignmentName = name
            Return True
        Else
            Return False
        End If
    End Function
    ''' <summary>
    ''' Changes the selected directional assignment name
    ''' for a Directional-type Token. A direction without a
    ''' valid image will cause ApplyToken to return null.
    ''' </summary>
    Public Function D_Select(direction As DirectionalSpriteDirections) As Boolean
        If Source.Type <> MultiType.Directional Then Throw New Exception($"D_ selector can't be used on {Source.Type.ToString()} source")
        _AssignmentName = CType(direction, Integer)
        Return True
    End Function

    ''' <summary>
    ''' A persistent Target that holds this N-Token's copy of the base image.
    ''' If the token is in 'reduced' mode, this evaluates to null.
    ''' </summary>
    Public N_Target As CanvasRenderTarget
    ''' <summary>
    ''' The square size of this image.
    ''' </summary>
    Public N_Size As Size
    ''' <summary>
    ''' The most recent token application result. Automatically set during
    ''' a SpriteBundle.ApplyToken() operation - you can retrieve the correct
    ''' image from this field any time.
    ''' </summary>
    Public N_Result As CanvasBitmap
    ''' <summary>
    ''' When this token is applied, the SpriteBundle detects if it has any overlays.
    ''' If this value is True, that is an indication that no overlays exist that
    ''' match this Token's most recent neighbor selection. This means you can free up
    ''' memory by reducing this Token - however, SpriteBundle will not un-reduce the
    ''' Token if new overlays are required; you are expected to make the method call yourself.
    ''' If you know that this sprite and its neighbors will not be changing Class/Variant names,
    ''' you can safely reduce a candidate.
    ''' </summary>
    Public N_ReductionCandidate As Boolean = False
    ''' <summary>
    ''' Changes the selector based on context. Give the name of the class 
    ''' or some other identifying string for each direction.
    ''' Pass an empty string to ignore a direction.
    ''' Don't apply this token regularly; it takes
    ''' considerable resources and time to render a new neighbor sprite.
    ''' </summary>
    Public Sub N_Select(east As String,
                        west As String,
                        north As String,
                        south As String)
        If Source.Type <> MultiType.Neighbor Then Throw New Exception(
            $"N_ selector can't be used on {Source.Type.ToString()} source")
        Dim assign As String = ""
        Dim proprt As String = ""
        If north <> "" Then
            assign += "2"
            proprt += north + ":"
        End If
        If south <> "" Then
            assign += "3"
            proprt += south + ":"
        End If
        If east <> "" Then
            assign += "0"
            proprt += east + ":"
        End If
        If west <> "" Then
            assign += "1"
            proprt += west + ":"
        End If
        proprt = proprt.TrimEnd(":")
        _AssignmentName = assign
        _PropertyName = proprt
    End Sub

    ''' <summary>
    ''' If True, then this Neighbor sprite assumes it will never have to draw another overlay.
    ''' This saves resources, but requires a N_Reduce(False) call to re-enable overlays, and
    ''' may cause significant lag if many sprites attempt to enable overlays at once. 
    ''' The stored image within N_Result is kept when you Reduce this token; however, when you
    ''' un-Reduce it, the token automatically reapplies itself to retrieve a new image. Ensure
    ''' that the selector is up-to-date before un-reducing.
    ''' </summary>
    Public Sub N_Reduce(reduce As Boolean)
        If reduce Then
            N_Target.Dispose()
            N_Target = Nothing
        Else
            N_Target = New CanvasRenderTarget(CanvasDevice.GetSharedDevice,
                                                    N_Size.Width, N_Size.Height, 96)
            Dim Neighbor_Soft = New SoftwareBitmap(BitmapPixelFormat.Bgra8, N_Size.Width, N_Size.Height, BitmapAlphaMode.Premultiplied)
            N_Result = CanvasBitmap.CreateFromSoftwareBitmap(CanvasDevice.GetSharedDevice, Neighbor_Soft)
            N_Result = Source.ApplyToken(Me)
        End If
    End Sub
End Class

Public Enum DirectionalSpriteDirections
    Invalid = -1
    East = 0
    West = 1
    North = 2
    South = 3
    NEast = 4
    NWest = 5
    SEast = 6
    SWest = 7
End Enum
