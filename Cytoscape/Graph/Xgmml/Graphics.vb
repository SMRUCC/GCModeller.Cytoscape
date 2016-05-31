Imports System.Xml.Serialization

Namespace CytoscapeGraphView.XGMML

    Public Class Graphics : Inherits AttributeDictionary

        Public ReadOnly Property ScaleFactor As Double
            Get
                Dim attr = Me("NETWORK_SCALE_FACTOR")
                If attr Is Nothing Then
                    Return 1
                Else
                    Return Val(attr.Value)
                End If
            End Get
        End Property

        Public Shared Function DefaultValue() As Graphics
            Dim attrs As Attribute() = {
                Attribute.StringValue([NameOf].ATTR_NETWORK_GRAPHICS_BACKGROUND_PAINT, "#EBE8E1"),
                Attribute.StringValue([NameOf].ATTR_NETWORK_GRAPHICS_NETWORK_DEPTH, "0.0")
            }

            Return New Graphics With {
                .Attributes = attrs
            }
        End Function
    End Class

    Public Class GraphAttribute : Inherits Attribute

        ''' <summary>
        ''' RDF的描述数据
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        <XmlElement("rdf-RDF")> Public Property RDF As InnerRDF
    End Class
End Namespace