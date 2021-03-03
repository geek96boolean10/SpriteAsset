Imports Microsoft.Graphics.Canvas
Imports Windows.Foundation

''' <summary>
''' A base class that provides the minimum interactions for a sprite.
''' </summary>
Public MustInherit Class Sprite
    ''' <summary>
    ''' The token that represents what sprites this object has access to.
    ''' If the token needs to access a different sprite variant, create a new token
    ''' to replace this one - accessing a SpriteBundle with an unfamiliar Token will
    ''' throw an exception!
    ''' </summary>
    Public Overridable Property Token As Token
    ''' <summary>
    ''' Retrieves the image that should be drawn in.
    ''' </summary>
    Public MustOverride Property Image As CanvasBitmap
    ''' <summary>
    ''' A value from 0.0 to 1.0 determining the opacity of the sprite when a SpriteLayer draws it.
    ''' Default is 1.
    ''' </summary>
    Public Property Opacity As Single = 1
    ''' <summary>
    ''' Retrieves the Clickable link, if any. Returns null if
    ''' the sprite is not selectable.
    ''' </summary>
    Public MustOverride Property Clickable As Clickable
    ''' <summary>
    ''' Returns a Rect specifying the position and size that this sprite
    ''' should be painted on the layer. 
    ''' Additionally updates the click bounds of this Sprite.
    ''' </summary>
    ''' <param name="unit">
    ''' The unit size to scale by.
    ''' </param>
    Public MustOverride ReadOnly Property IntendedRect(unit As Integer) As Rect
    ''' <summary>
    ''' Forces the sprite to retrieve its image from the original source
    ''' with a token, rather than allowing a cached image to be used.
    ''' </summary>
    Public MustOverride Sub Invalidate()

End Class

''' <summary>
''' Defines a boundary in which a click should be registered. When added to a SimpleLayer,
''' if a mousepress is within the given bounds, the given Action should be triggered. Alternatively,
''' an event-driven mode is available.
''' Ensure that the units given by ClickBounds align properly with the canvas.
''' </summary>
Public Class Clickable
    Private AlertMode As ClickAlertMode
    Public ClickBounds As Rect
    Private Left_Action As Action
    Private Right_Action As Action
    Public Event Clicked(sender As Clickable, button As MouseButton)

    ''' <summary>
    ''' Creates a clickable that invokes the given action any time it is clicked.
    ''' Note that this means the clicked event is not fired for this object.
    ''' Pass in Nothing if a particular button press should be ignored.
    ''' </summary>
    ''' <param name="Bound">The bounds of the screen that should trigger this Clickable.
    ''' When used with an ESprite, the Bounds are automatically updated with the sprite
    ''' location.</param>
    Public Sub New(Bound As Rect, LeftAction As Action, RightAction As Action)
        AlertMode = ClickAlertMode.Action
        ClickBounds = Bound
        Left_Action = LeftAction
        Right_Action = RightAction
    End Sub

    ''' <summary>
    ''' Creates a clickable that uses a certain alert mode. Note that you cannot
    ''' use this constructor to create an Action-based alert mode.
    ''' </summary>
    ''' <param name="Bound">The bounds of the screen that should trigger this Clickable.
    ''' When used with an ESprite, the Bounds are automatically updated with the sprite
    ''' location.</param>
    Public Sub New(Bound As Rect, Mode As ClickAlertMode)
        If Mode = ClickAlertMode.Action Then
            Throw New Exception("Don't create a clickable by specifying the Action mode - use the other constructor instead.")
        End If
        AlertMode = Mode
    End Sub

    ''' <summary>
    ''' Notifies this clickable that it has been clicked.
    ''' Returns true if the action has been handled; false to continue searching
    ''' for another Clickable.
    ''' In Action mode, the click is considered ignored if no handler is attached.
    ''' </summary>
    Public Function Click(button As MouseButton) As Boolean
        Select Case AlertMode
            Case ClickAlertMode.Ignore
                Return False
            Case ClickAlertMode.Halt
                'do nothing but block by returning true
            Case ClickAlertMode.Action
                If button = MouseButton.Left Then
                    If Left_Action Is Nothing Then
                        Return False
                    End If
                    Left_Action.Invoke()
                ElseIf button = MouseButton.Right Then
                    If Right_Action Is Nothing Then
                        Return False
                    End If
                    Right_Action.Invoke()
                End If
            Case ClickAlertMode.Events
                RaiseEvent Clicked(Me, button)
        End Select
        Return True
    End Function

    Public Enum ClickAlertMode
        ''' <summary>
        ''' Does not respond.
        ''' </summary>
        Ignore
        ''' <summary>
        ''' Does not respond to clicks, but blocks further searching.
        ''' </summary>
        Halt
        ''' <summary>
        ''' Invokes a method.
        ''' </summary>
        Action
        ''' <summary>
        ''' Raises an event.
        ''' </summary>
        Events
    End Enum

    Public Enum MouseButton
        None
        Left
        Right
    End Enum

    ''' <summary>
    ''' A static clickable that Halts any incoming clicks. This cannot be changed.
    ''' Use for any controls that should cause modal behavior or block clicks from passing through.
    ''' </summary>
    Public Shared Opaque As New Clickable(Nothing, ClickAlertMode.Halt)

End Class
