' The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

Public NotInheritable Class SpriteBoundDetail
    Inherits UserControl

    Public Sub New()
        InitializeComponent()
        AddHandler TextboxAssignment.TextChanged, Sub()
                                                      RaiseEvent ValuesUpdated()
                                                  End Sub
        AddHandler TextboxProperty.TextChanged, Sub()
                                                    RaiseEvent ValuesUpdated()
                                                End Sub
    End Sub

    Public Property AssignmentValue As String
        Get
            Return TextboxAssignment.Text
        End Get
        Set(value As String)
            TextboxAssignment.Text = value
        End Set
    End Property

    Public Property PropertyValue As String
        Get
            Return TextboxProperty.Text
        End Get
        Set(value As String)
            TextboxProperty.Text = value
        End Set
    End Property

    Public Event ValuesUpdated()
End Class
