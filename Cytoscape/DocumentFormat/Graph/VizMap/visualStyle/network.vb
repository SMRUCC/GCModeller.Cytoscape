Imports System.Xml.Serialization
Imports LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Cytoscape.DocumentFormat.Visualization.visualProperty

Namespace DocumentFormat.Visualization.DocumentNodes

    Public Class network : Inherits visualNode
    End Class

    Public MustInherit Class visualNode
        <XmlElement("visualProperty")> Public Property visualPropertys As visualProperty.visualProperty()
        <XmlElement("dependency")> Public Property dependency As dependency()
    End Class
End Namespace