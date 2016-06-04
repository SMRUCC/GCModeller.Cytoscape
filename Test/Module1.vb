Imports LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Cytoscape.API
Imports LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Cytoscape.CytoscapeGraphView
Imports LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Cytoscape.CytoscapeGraphView.XGMML
Imports Microsoft.VisualBasic
Imports Microsoft.VisualBasic.DataVisualization.Network.Graph

Module Module1

    Function Main() As Integer

        '     Dim mm As New GraphAttribute With {.RDF = New InnerRDF With {.meta = New NetworkMetadata}, .Name = RandomDouble()}
        '  Dim gf As New Graph With {.Attributes = {mm, New GraphAttribute With {.Name = Now.ToString}}}
        '   Call gf.SaveAsXml("x:\11223.xml")

        '  Dim fdsfs = Graph.Load("x:\11223.xml")

        Dim g = Graph.Load("F:\GCModeller\GCI Project\DataVisualization\Cytoscape\test.cytoscape.xgmml")
        Dim net As NetworkGraph = g.CreateGraph

        '   Call g.Save("x:\gggggg\ddddd.xml")

        Dim xml As Text.Xml.XmlDoc = Text.Xml.XmlDoc.FromXmlFile("F:\GCModeller\GCI Project\DataVisualization\Cytoscape\test.cytoscape.xgmml")
        xml.xmlns.xsd = "22333333"
        xml.xmlns.xsi = "@#"
        xml.xmlns.Set("ggy", "oK!")
        Call xml.Save("x:\dddd.xml", Encodings.UTF8)
    End Function
End Module
