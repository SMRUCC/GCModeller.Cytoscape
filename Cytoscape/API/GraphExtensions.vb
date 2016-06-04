Imports System.Runtime.CompilerServices
Imports LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Cytoscape.CytoscapeGraphView
Imports LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Cytoscape.CytoscapeGraphView.XGMML
Imports Microsoft.VisualBasic.DataVisualization
Imports Microsoft.VisualBasic.DataVisualization.Network.Graph
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic

Namespace API

    Public Module GraphExtensions

        ''' <summary>
        ''' Creates the network graph model from the Cytoscape data model to generates the network layout or visualization 
        ''' </summary>
        ''' <param name="g"></param>
        ''' <returns></returns>
        <Extension>
        Public Function CreateGraph(g As Graph) As NetworkGraph
            Dim nodes As Network.Graph.Node() =
                LinqAPI.Exec(Of Network.Graph.Node) <= From n As XGMML.Node
                                                       In g.Nodes
                                                       Select New Network.Graph.Node(n.label)
            Dim edges As Network.Graph.Edge() =
                LinqAPI.Exec(Of Network.Graph.Edge) <= From edge As XGMML.Edge
                                                       In g.Edges
                                                       Select New Network.Graph.Edge(
                                                           CStr(edge.Id),
                                                           nodes(edge.source),
                                                           nodes(edge.target),
                                                           New EdgeData)
            Dim net As New NetworkGraph() With {
                .nodes = New List(Of Network.Graph.Node)(nodes),
                .edges = New List(Of Network.Graph.Edge)(edges)
            }

            Return net
        End Function
    End Module
End Namespace