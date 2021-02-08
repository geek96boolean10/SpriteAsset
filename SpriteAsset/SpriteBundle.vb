
Imports System.Numerics
Imports Microsoft.Graphics.Canvas
Imports Windows.Graphics.DirectX
Imports Windows.Graphics.Imaging
Imports Windows.Storage.Streams
''' <summary>
''' A collection that contains several images that can all be used to represent a sprite
''' under different circumstances. Retrieve a Token object from the SpriteBundle to give
''' access to the SpriteBundle's library to a Sprite.
''' </summary>
Public Class SpriteBundle
    Private Source As MultiSpriteData
    ''' <summary>
    ''' Image data for each assignment.
    ''' </summary>
    Private Reference As New Dictionary(Of AssignmentPropertyPair, CanvasBitmap)
    ''' <summary>
    ''' Guaranteed order as-saved of the assignments.
    ''' </summary>
    Private Indexed As New List(Of AssignmentPropertyPair)
    Public ReadOnly Property Type As MultiType
        Get
            Return Source.Type
        End Get
    End Property
    ''' <summary>
    ''' Always upper-case name of the associated class.
    ''' </summary>
    Public ReadOnly Property ClassName As String
        Get
            Return Source.ClassName
        End Get
    End Property
    ''' <summary>
    ''' Always upper-case variant, if any, of the class.
    ''' </summary>
    Public ReadOnly Property VariantName As String
        Get
            Return Source.VariantName
        End Get
    End Property

    ''' <summary>
    ''' Instantiates a new Bundle, converting the Bounds stored within a 
    ''' MultiSpriteData object to their equivalent bitmap images on the given sprite sheet image.
    ''' </summary>
    Public Sub New(spriteData As MultiSpriteData, sheet As ComplexSheet)
        Source = spriteData
        For i = 0 To Source.Bounds.Count - 1
            Dim boundsID = Source.Bounds(i)
            Dim bounds = sheet.GetBounds(boundsID)
            Dim assign = Source.BoundAssignment(i)
            If bounds.Width = 0 OrElse bounds.Height = 0 Then
                Throw New Exception(
                    $"An invalid Bounds was found: {sheet.BitmapName} -> " &
                    $"{Source.ClassName}:{Source.VariantName}, bound ID {boundsID}." &
                    "Check your save file!")
            End If
            Dim soft As New SoftwareBitmap(BitmapPixelFormat.Bgra8,
                                           bounds.Width, bounds.Height, BitmapAlphaMode.Premultiplied)
            Dim cbit = CanvasBitmap.CreateFromSoftwareBitmap(CanvasDevice.GetSharedDevice, soft)
            cbit.CopyPixelsFromBitmap(sheet.Image, 0, 0,
                                      bounds.Left, bounds.Top, bounds.Width, bounds.Height)

            Indexed.Add(assign)
            Reference.Add(assign, cbit)
        Next

        If Indexed.Count < 1 Then Throw New Exception("Sprites cannot have no images.")

        Select Case Type
            Case MultiType.Single
                SetupSingle()
            Case MultiType.Animation
                SetupAnimator()
            Case MultiType.Directional
                SetupDirectional()
            Case MultiType.Neighbor
                SetupNeighbor()
        End Select

    End Sub

#Region "SingleMode"
    Private SingleBitmap As CanvasBitmap
    Private Sub SetupSingle()
        SingleBitmap = Reference(Indexed(0))
    End Sub
#End Region

#Region "AnimationMode"
    Private Animation_Assignments As Dictionary(Of String, AnimationSequence)
    ''' <summary>
    ''' Builds necessary variable values to determine the image via animation.
    ''' </summary>
    Private Sub SetupAnimator()
        Animation_Assignments = New Dictionary(Of String, AnimationSequence)
        For i = 0 To Indexed.Count - 1
            Dim assign As AssignmentPropertyPair = Indexed(i)
            Dim sequence As AnimationSequence = Nothing
            If Animation_Assignments.ContainsKey(assign.Assignment) Then
                sequence = Animation_Assignments(assign.Assignment)
            Else
                sequence = New AnimationSequence(assign.Assignment)
                Animation_Assignments.Add(sequence.Name, sequence)
            End If

            Dim duration As Double

            If assign.Property = "" Then assign.Property = 0 ' no static for the time being

            If Not Double.TryParse(assign.Property, duration) Then
                Debug.WriteLine($"Invalid property '{assign.Property}' as double for animation in {Source.ClassName}:{Source.VariantName}")
                duration = 0
            End If
            sequence.Append(Reference(assign), duration)
        Next
    End Sub
#End Region

#Region "DirectionalMode"
    Private Direction_Assignments As New Dictionary(Of DirectionalSpriteDirections, CanvasBitmap)
    Private Sub SetupDirectional()
        Direction_Assignments = New Dictionary(Of DirectionalSpriteDirections, CanvasBitmap)
        ' make all of them
        For i = 0 To Indexed.Count - 1
            Dim assign As AssignmentPropertyPair = Indexed(i)
            Dim image As CanvasBitmap = Reference(assign)
            ' assignment field is direction number, ignore property
            Dim dir As DirectionalSpriteDirections
            If Not [Enum].TryParse(GetType(DirectionalSpriteDirections), assign.Assignment, dir) Then
                Debug.WriteLine($"Invalid property '{assign.Property}' as direction for directional in {Source.ClassName}:{Source.VariantName}")
                dir = DirectionalSpriteDirections.Invalid
            End If
            If dir = DirectionalSpriteDirections.Invalid Then
                Continue For
            End If
            Direction_Assignments(dir) = image
        Next
        ' check satisfactions
        If Not Direction_Assignments.ContainsKey(DirectionalSpriteDirections.East) Then
            Throw New Exception($"No valid East provided in {Source.ClassName}:{Source.VariantName} - one MUST be provided.")
        ElseIf Not Direction_Assignments.ContainsKey(DirectionalSpriteDirections.West) Then
            Throw New Exception($"No valid West provided in {Source.ClassName}:{Source.VariantName} - one MUST be provided.")
        End If
        If Not (Direction_Assignments.ContainsKey(DirectionalSpriteDirections.NEast) AndAlso
                Direction_Assignments.ContainsKey(DirectionalSpriteDirections.SEast) AndAlso
                Direction_Assignments.ContainsKey(DirectionalSpriteDirections.NWest) AndAlso
                Direction_Assignments.ContainsKey(DirectionalSpriteDirections.SWest)) Then
            ' not all four compound were given, default them to east/west
            Direction_Assignments(DirectionalSpriteDirections.NEast) = Direction_Assignments(DirectionalSpriteDirections.East)
            Direction_Assignments(DirectionalSpriteDirections.SEast) = Direction_Assignments(DirectionalSpriteDirections.East)
            Direction_Assignments(DirectionalSpriteDirections.NWest) = Direction_Assignments(DirectionalSpriteDirections.West)
            Direction_Assignments(DirectionalSpriteDirections.SWest) = Direction_Assignments(DirectionalSpriteDirections.West)
        End If
    End Sub
#End Region

#Region "NeighborMode"
    Private Neighbor_Assignments As Dictionary(Of String, NeighborOverlayBitmap)
    Private Neighbor_BaseSize As Size
    Private Neighbor_Base As CanvasBitmap
    ''' <summary>
    ''' A SoftwareBitmap used as the basis for all created tokens.
    ''' </summary>
    Private Neighbor_Soft As SoftwareBitmap
    Private Sub SetupNeighbor()
        Neighbor_Assignments = New Dictionary(Of String, NeighborOverlayBitmap)
        For i = 0 To Indexed.Count - 1
            Dim assign As AssignmentPropertyPair = Indexed(i)
            Dim image As CanvasBitmap = Reference(assign)

            If assign.Assignment = "" Then
                Neighbor_Base = image
                Neighbor_BaseSize = New Size(image.Bounds.Width, image.Bounds.Height)
                Continue For
            End If

            Dim dir As DirectionalSpriteDirections
            If Not [Enum].TryParse(GetType(DirectionalSpriteDirections), assign.Property, dir) Then
                Debug.WriteLine($"Invalid property '{assign.Property}' as direction for neighbor in {Source.ClassName}:{Source.VariantName}")
                dir = DirectionalSpriteDirections.Invalid
            End If
            If dir = DirectionalSpriteDirections.Invalid Then
                Continue For
            End If
            If Not Neighbor_Assignments.ContainsKey(assign.Assignment) Then
                Dim nbit = New NeighborOverlayBitmap(assign.Assignment, image.Bounds, image, dir)
                Neighbor_Assignments.Add(nbit.Name, nbit)
            Else
                Dim nbit As NeighborOverlayBitmap = Neighbor_Assignments(assign.Assignment)
                nbit.Append(dir, image)
            End If
        Next
        If Neighbor_Base Is Nothing Then Throw New Exception($"No assignment-less base given for neighbor in {Source.ClassName}:{Source.VariantName}")
        Neighbor_Soft = New SoftwareBitmap(BitmapPixelFormat.Bgra8, Neighbor_BaseSize.Width, Neighbor_BaseSize.Height, BitmapAlphaMode.Premultiplied)
    End Sub

#End Region

    ''' <summary>
    ''' Gets a formatted string with the full name of this bundle.
    ''' Always upper-case.
    ''' </summary>
    Public Function GetName() As String
        Dim str = ClassName
        If VariantName <> "" Then
            str += ":" & VariantName
        End If
        Return str
    End Function

#Region "TokenStuff"
    ''' <summary>
    ''' Creates a token that stores some state information that can be used to
    ''' retrieve the correct image from the bundle.
    ''' If Reduce is true and the SpriteBundle's type is of Neighbor, then the resultant Tokens
    ''' will assume they never have overlays. This saves resources, but re-enabling overlays afterwards
    ''' may consume significant time and resources.
    ''' </summary>
    Public Function CreateToken(Optional reduce As Boolean = False) As Token
        Dim t As New Token(Me)
        Select Case Type
            Case MultiType.Single
                ' no setup required
            Case MultiType.Animation
                t.A_AllowedNames = Animation_Assignments.Keys.ToArray
                t.A_Select(t.A_AllowedNames(0))
            Case MultiType.Directional
                t.D_Select(DirectionalSpriteDirections.East)
            Case MultiType.Neighbor
                t.N_Size = Neighbor_BaseSize
                If reduce Then
                    ' don't create a rendertarget!
                Else
                    t.N_Target = New CanvasRenderTarget(CanvasDevice.GetSharedDevice,
                                                        Neighbor_BaseSize.Width, Neighbor_BaseSize.Height, 96)
                End If
                'Dim Neighbor_Soft = New SoftwareBitmap(BitmapPixelFormat.Bgra8, Neighbor_BaseSize.Width, Neighbor_BaseSize.Height, BitmapAlphaMode.Premultiplied)
                t.N_Result = CanvasBitmap.CreateFromSoftwareBitmap(CanvasDevice.GetSharedDevice, Neighbor_Soft)
                t.N_Select("", "", "", "")
        End Select
        Return t
    End Function

    ''' <summary>
    ''' Applies a token created by this Bundle to retrieve the correct image.
    ''' If no valid image matches the Token details, null is returned.
    ''' </summary>
    Public Function ApplyToken(t As Token) As CanvasBitmap
        If t.Source IsNot Me Then
#If DEBUG Then
            Throw New Exception("Can't apply a token that wasn't created by this bundle.")
#Else
            Console.Writeline("Token does not belong to this Bundle.")
            Return Nothing
#End If
        End If
        Select Case Type
            Case MultiType.Single
                Return SingleBitmap
            Case MultiType.Animation
                ' token class limits assignment names automatically.
                Return Animation_Assignments(t.AssignmentName).Retrieve(t.A_ElapsedTimeMS)
            Case MultiType.Directional
                Dim dir As DirectionalSpriteDirections =
                    [Enum].Parse(GetType(DirectionalSpriteDirections), CType(dir, Integer))
                If Direction_Assignments.ContainsKey(dir) Then
                    Return Direction_Assignments(t.AssignmentName)
                Else
                    Return Nothing
                End If
            Case MultiType.Neighbor
                If t.N_Target Is Nothing Then ' we're in reduced mode
                    t.N_Result = Neighbor_Base
                Else
                    Dim sesh = t.N_Target.CreateDrawingSession()
                    sesh.Clear(New Vector4(0, 0, 0, 0))
                    Dim nearby = t.PropertyName.Split(":")
                    sesh.DrawImage(Neighbor_Base)
                    For i = 0 To t.AssignmentName().Length - 1
                        Dim dir As String = t.AssignmentName()(i) + ""
                        ' guaranteed to be a valid number
                        Dim direc As DirectionalSpriteDirections =
                            [Enum].Parse(GetType(DirectionalSpriteDirections), CType(dir, Integer))
                        Dim clas As String = nearby(i)
                        If Not Neighbor_Assignments.ContainsKey(clas) Then
                            ' no valid overlay for this type, skip
                            Continue For
                        End If
                        Dim nbit = Neighbor_Assignments(clas)
                        Dim overlay = nbit.Retrieve(direc)
                        sesh.DrawImage(overlay)
                    Next
                    ' finalize the drawing into n_result
                    sesh.Dispose()
                    ' the assumption here is that n_result is a unique bitmap
                    ' to Neighbor_base and won't affect it
                    t.N_Result.CopyPixelsFromBitmap(t.N_Target)
                End If
                Return t.N_Result
            Case Else
                Throw New Exception("Invalid SpriteBundle type!")
        End Select
    End Function
End Class
#End Region

''' <summary>
''' A timeline of images that describes a single CLASS:VARIANT's assigned animation name.
''' </summary>
Public Class AnimationSequence
    Public Name As String
    Private BoundIDs As New List(Of CanvasBitmap)
    Private DurationsMS As New List(Of Double)
    Public LengthMS As Double
    Public Sub New(name As String)
        Me.Name = name
    End Sub
    Public Sub Append(image As CanvasBitmap, durationMS As Double)
        BoundIDs.Add(image)
        DurationsMS.Add(durationMS)
        LengthMS += durationMS
    End Sub
    Public Function Retrieve(ByRef elapsedMS As Double) As CanvasBitmap
        elapsedMS = elapsedMS Mod LengthMS
        Dim sum As Double = 0
        For i = 0 To DurationsMS.Count - 1
            Dim duration = DurationsMS(i)
            If sum + duration > elapsedMS Then
                Return BoundIDs(i)
            Else
                sum += duration
            End If
        Next
        Return BoundIDs(0)
    End Function
End Class

''' <summary>
''' A class that returns a stored CanvasBitmap overlay associated with a single,
''' specific class based on a given direction. The overlay should be drawn atop
''' a base image.
''' </summary>
Public Class NeighborOverlayBitmap
    Private RenderTarget As CanvasRenderTarget
    Private EditingSession As CanvasDrawingSession
    ''' <summary>
    ''' The Class name that this Bitmap draws for.
    ''' </summary>
    Public Name As String
    Private BaseBounds As Rect
    ''' <summary>
    ''' If one exists, it's returned; otherwise, a rotated version should be made.
    ''' </summary>
    Private SpecificDirection As New Dictionary(Of DirectionalSpriteDirections, CanvasBitmap)

    ''' <param name="name">The Class(+Variant) name associated with this overlay image.</param>
    Public Sub New(name As String, baseBounds As Rect, first As CanvasBitmap,
                   Optional dir As DirectionalSpriteDirections = DirectionalSpriteDirections.East)
        Me.Name = name
        Me.BaseBounds = baseBounds
        RenderTarget = New CanvasRenderTarget(CanvasDevice.GetSharedDevice,
                                              baseBounds.Width, baseBounds.Height, 96)
        'EditingSession = RenderTarget.CreateDrawingSession() ' don't create until it's needed
        SpecificDirection.Add(dir, first)
        ' ensure an East default exists
        Init(dir, first)
    End Sub
    ''' <summary>
    ''' Generates an East image from whatever was given.
    ''' </summary>
    Private Sub Init(dir As DirectionalSpriteDirections, first As CanvasBitmap)
        If dir <> DirectionalSpriteDirections.East Then
            Dim turned = Turn(dir, DirectionalSpriteDirections.East, first)
            Append(DirectionalSpriteDirections.East, turned)
        End If
    End Sub
    ''' <summary>
    ''' Makes a sprite with the given orientation from the East default.
    ''' </summary>
    Private Sub Make(dir As DirectionalSpriteDirections)
        Dim turned = Turn(DirectionalSpriteDirections.East, dir,
                                SpecificDirection(DirectionalSpriteDirections.East))
        Append(dir, turned)
    End Sub
    Public Sub Append(dir As DirectionalSpriteDirections, bmp As CanvasBitmap)
        If bmp.Bounds <> BaseBounds Then Throw New Exception("All images must be the same size for Directional")
        SpecificDirection(dir) = bmp
    End Sub

    Public Function Retrieve(dir As DirectionalSpriteDirections) As CanvasBitmap
        If SpecificDirection.ContainsKey(dir) Then
            Return SpecificDirection(dir)
        Else
            Make(dir)
            Return Retrieve(dir)
        End If
    End Function

    ''' <summary>
    ''' Rotates a CanvasBitmap.
    ''' Do not call this repeatedly; save the response as much as possible, or
    ''' provide a saved file instead.
    ''' </summary>
    Public Function Turn(fromDir As DirectionalSpriteDirections,
                                toDir As DirectionalSpriteDirections,
                                input As CanvasBitmap) As CanvasBitmap
        EditingSession = RenderTarget.CreateDrawingSession()
        Dim transform As Matrix3x2 = Nothing
        Dim center As New Vector2(input.Bounds.Width / 2, input.Bounds.Height / 2)
        ' assuming rotation is clockwise
        Select Case fromDir
            Case DirectionalSpriteDirections.East
                Select Case toDir
                    Case DirectionalSpriteDirections.West
                        transform = Matrix3x2.CreateRotation(Math.PI, center)
                    Case DirectionalSpriteDirections.North
                        transform = Matrix3x2.CreateRotation(-Math.PI / 2, center)
                    Case DirectionalSpriteDirections.South
                        transform = Matrix3x2.CreateRotation(Math.PI / 2, center)
                    Case Else
                        Throw New NotSupportedException
                End Select
            Case DirectionalSpriteDirections.West
                Select Case toDir
                    Case DirectionalSpriteDirections.East
                        transform = Matrix3x2.CreateRotation(Math.PI, center)
                    Case Else
                        Throw New NotSupportedException
                End Select
            Case DirectionalSpriteDirections.North
                Select Case toDir
                    Case DirectionalSpriteDirections.East
                        transform = Matrix3x2.CreateRotation(Math.PI / 2, center)
                    Case Else
                        Throw New NotSupportedException
                End Select
            Case DirectionalSpriteDirections.South
                Select Case toDir
                    Case DirectionalSpriteDirections.East
                        transform = Matrix3x2.CreateRotation(-Math.PI / 2, center)
                    Case Else
                        Throw New NotSupportedException
                End Select
        End Select
        EditingSession.Clear(New Vector4(0, 0, 0, 0))
        EditingSession.Transform = transform
        EditingSession.DrawImage(input)
        EditingSession.Dispose() ' commit
        Dim soft = New SoftwareBitmap(BitmapPixelFormat.Bgra8, input.Bounds.Width, input.Bounds.Height, BitmapAlphaMode.Premultiplied)
        Dim returnable = CanvasBitmap.CreateFromSoftwareBitmap(CanvasDevice.GetSharedDevice, soft)
        returnable.CopyPixelsFromBitmap(RenderTarget)
        Return returnable
    End Function
End Class