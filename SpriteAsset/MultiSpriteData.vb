Imports Newtonsoft.Json.Linq
''' <summary>
''' Contains data about a multisprite. Used to spawn a usable sprite object.
''' </summary>
Public Class MultiSpriteData
    Public ClassName As String = "Object"
    Public VariantName As String = ""
    Public Type As MultiType = MultiType.Single
    Public Bounds As New List(Of Integer)
    Public BoundAssignment As New List(Of AssignmentPropertyPair)

    Public Function Serialize() As JObject
        Dim sprData As New JObject
        sprData.Add("ClassName", ClassName)
        sprData.Add("VariantName", VariantName)
        sprData.Add("Type", Type.ToString)

        Dim bds As New JArray
        For Each i In Bounds
            bds.Add(i)
        Next
        sprData.Add("Bounds", bds)

        Dim bas As New JArray
        For Each i In BoundAssignment
            bas.Add(i.Serialize())
        Next
        sprData.Add("BoundAssignment", bas)
        Return sprData
    End Function

    Public Sub New()

    End Sub

    Public Sub New(data As JObject)
        ClassName = data("ClassName")
        VariantName = data("VariantName")
        Type = [Enum].Parse(GetType(MultiType), data("Type"))
        Dim bds As JArray = data("Bounds")
        Bounds.AddRange(bds.Select(Function(obj As Object) As Integer
                                       Return Integer.Parse(obj.ToString)
                                   End Function))
        Dim bas As JArray = data("BoundAssignment")
        BoundAssignment.AddRange(bas.Select(Function(obj As Object) As AssignmentPropertyPair
                                                Return New AssignmentPropertyPair(CType(obj, JObject))
                                            End Function))
    End Sub
    ''' <summary>
    ''' Adds a new bound and its assignment/property pair at the given index.
    ''' </summary>
    Public Sub AddNewDetail(index As Integer, BoundID As Integer, APPair As AssignmentPropertyPair)
        Bounds.Insert(index, BoundID)
        BoundAssignment.Insert(index, APPair)
    End Sub
    ''' <summary>
    ''' Removes the bounds and its assignment/property pair at the given index.
    ''' </summary>
    Public Sub RemoveDetail(index As Integer)
        Bounds.RemoveAt(index)
        BoundAssignment.RemoveAt(index)
    End Sub
End Class
Public Structure AssignmentPropertyPair
    Public Assignment As String
    Public [Property] As String

    Public Sub New(assignment As String, [property] As String)
        Me.Assignment = assignment
        Me.Property = [property]
    End Sub

    Public Function Serialize() As JObject
        Dim data As New JObject
        data.Add("Assignment", Assignment)
        data.Add("Property", [Property])
        Return data
    End Function

    Public Sub New(data As JObject)
        Me.Assignment = data("Assignment")
        Me.Property = data("Property")
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If TypeOf obj IsNot AssignmentPropertyPair Then Return False
        Return Assignment = DirectCast(obj, AssignmentPropertyPair).Assignment AndAlso
            [Property] = DirectCast(obj, AssignmentPropertyPair).Property
    End Function
End Structure
Public Enum MultiType
    [Single]
    Directional
    Neighbor
    Animation
End Enum
