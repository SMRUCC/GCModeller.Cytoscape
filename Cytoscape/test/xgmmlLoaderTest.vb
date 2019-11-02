Imports SMRUCC.genomics.Visualize.Cytoscape.CytoscapeGraphView

Module xgmmlLoaderTest

    Sub Main()
        Dim g = XGMML.RDFXml.Load("E:\GCModeller\src\interops\visualize\Cytoscape\data\demo.xgmml")
        Dim bend = g.edges.First.graphics.edgeBendHandles

        Pause()
    End Sub
End Module
