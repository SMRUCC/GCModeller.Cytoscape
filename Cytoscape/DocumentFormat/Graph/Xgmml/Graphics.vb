Imports System.Xml.Serialization
Imports LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Cytoscape.DocumentFormat.CytoscapeGraphView.DocumentElements

Namespace DocumentFormat.CytoscapeGraphView

    Public Class Graphics : Inherits DocumentElements.AttributeDictionary

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