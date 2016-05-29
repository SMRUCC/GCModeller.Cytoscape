Imports System.Drawing
Imports System.Xml.Serialization
Imports Microsoft.VisualBasic
Imports Microsoft.VisualBasic.ComponentModel.Collection.Generic
Imports Microsoft.VisualBasic.DocumentFormat.RDF
Imports Microsoft.VisualBasic.Language

Namespace DocumentFormat.CytoscapeGraphView.DocumentElements

    ''' <summary>
    ''' 一个网络之中的对象所具备有的属性值
    ''' </summary>
    ''' <remarks></remarks>
    <XmlType("att")>
    Public Class Attribute : Implements sIdEnumerable

        <XmlAttribute("name")> Public Property Name As String Implements sIdEnumerable.Identifier
        <XmlAttribute("value")> Public Property Value As String
        <XmlAttribute("type")> Public Property Type As String

        <XmlAttribute("cy_hidden")> Public Property Hidden As String
        <XmlAttribute("cy_directed")> Public Property Directed As String
        <XmlAttribute("cy-type")> Public Property cyType As String
        <XmlAttribute("cy-elementType")> Public Property elementType As String

        Public Const ATTR_NAME_NETWORK_METADATA As String = "networkMetadata"

        Public Const ATTR_SHARED_NAME As String = "shared name"
        Public Const ATTR_NAME As String = "name"
        Public Const ATTR_VALUE_TYPE_STRING As String = "string"
        Public Const ATTR_VALUE_TYPE_BOOLEAN As String = "boolean"
        Public Const ATTR_VALUE_TYPE_REAL As String = "real"
        Public Const ATTR_VALUE_TYPE_INTEGER As String = "integer"
        Public Const ATTR_VALUE_TYPE_LIST As String = "list"
        Public Const ATTR_SHARED_INTERACTION As String = "shared interaction"

        Public Const ATTR_NETWORK_GRAPHICS_HEIGHT As String = "NETWORK_HEIGHT"
        Public Const ATTR_NETWORK_GRAPHICS_SCALE_FACTOR As String = "NETWORK_SCALE_FACTOR"
        Public Const ATTR_NETWORK_GRAPHICS_CENTER_X_LOCATION As String = "NETWORK_CENTER_X_LOCATION"
        Public Const ATTR_NETWORK_GRAPHICS_NETWORK_DEPTH As String = "NETWORK_DEPTH"
        Public Const ATTR_NETWORK_GRAPHICS_CENTER_Z_LOCATION As String = "NETWORK_CENTER_Z_LOCATION"
        Public Const ATTR_NETWORK_GRAPHICS_NODE_SELECTION As String = "NETWORK_NODE_SELECTION"
        Public Const ATTR_NETWORK_GRAPHICS_EDGE_SELECTION As String = "NETWORK_EDGE_SELECTION"
        Public Const ATTR_NETWORK_GRAPHICS_TITLE As String = "NETWORK_TITLE"
        Public Const ATTR_NETWORK_GRAPHICS_BACKGROUND_PAINT As String = "NETWORK_BACKGROUND_PAINT"
        Public Const ATTR_NETWORK_GRAPHICS_CENTER_Y_LOCATION As String = "NETWORK_CENTER_Y_LOCATION"
        Public Const ATTR_NETWORK_GRAPHICS_NETWORK_WIDTH As String = "NETWORK_WIDTH"

        Public Shared ReadOnly Property TypeMapping As IReadOnlyDictionary(Of Type, String) =
            New Dictionary(Of Type, String) From {
 _
            {GetType(String), ATTR_VALUE_TYPE_STRING},
            {GetType(Boolean), ATTR_VALUE_TYPE_BOOLEAN},
            {GetType(Integer), ATTR_VALUE_TYPE_INTEGER},
            {GetType(Double), ATTR_VALUE_TYPE_REAL}
        }

        Public Overrides Function ToString() As String
            Return $"({Type}) {Name} = {Value}"
        End Function

        Public Overloads Shared Operator +(lst As List(Of Attribute), x As Attribute) As List(Of Attribute)
            Call lst.Add(x)
            Return lst
        End Operator
    End Class

    Public MustInherit Class AttributeDictionary

        Dim _innerHash As Dictionary(Of Attribute)

        <XmlElement("att")> Public Property Attributes As Attribute()
            Get
                If _innerHash.IsNullOrEmpty Then
                    Return New Attribute() {}
                End If
                Return _innerHash.Values.ToArray
            End Get
            Set(value As Attribute())
                If value.IsNullOrEmpty Then
                    _innerHash = New Dictionary(Of Attribute)
                Else
                    _innerHash = value.ToDictionary
                End If
            End Set
        End Property

        ''' <summary>
        ''' 属性值不存在则返回空值
        ''' </summary>
        ''' <param name="Name"></param>
        ''' <returns></returns>
        Default Public ReadOnly Property Value(Name As String) As Attribute
            Get
                If _innerHash.ContainsKey(Name) Then
                    Return _innerHash(Name)
                Else
                    Return Nothing
                End If
            End Get
        End Property

        Public Function AddAttribute(Name As String, value As String, Type As String) As Boolean
            Dim attr As Attribute

            If _innerHash.ContainsKey(Name) Then
                attr = _innerHash(Name)
            Else
                attr = New Attribute With {.Name = Name}
                Call _innerHash.Add(Name, attr)
            End If

            attr.Value = value
            attr.Type = Type

            Return True
        End Function

        Public Function SetAttribute(Name As String, Value As String) As Boolean
            If _innerHash.ContainsKey(Name) Then
                _innerHash(Name).Value = Value
            Else
                Dim attr As New Attribute With {
                    .Value = Value,
                    .Name = Name,
                    .Type = Attribute.ATTR_VALUE_TYPE_STRING
                }
                Call _innerHash.Add(Name, attr)
            End If

            Return True
        End Function

        Public Overrides Function ToString() As String
            Dim array As String() =
                LinqAPI.Exec(Of String) <= From attr As Attribute
                                           In _innerHash.Values
                                           Let strValue As String = attr.ToString
                                           Select strValue
            Return String.Join("; ", array)
        End Function
    End Class
End Namespace