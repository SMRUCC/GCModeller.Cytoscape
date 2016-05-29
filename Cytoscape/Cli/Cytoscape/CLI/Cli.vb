Imports System.Drawing
Imports LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Cytoscape.DocumentFormat.CytoscapeGraphView
Imports LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Cytoscape.DocumentFormat.Visualization
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports Microsoft.VisualBasic.Serialization

<PackageNamespace("Cytoscape.CLI", Category:=APICategories.CLI_MAN,
                  Description:="This is a lite version of the cytoscape program from cytoscape.org in GCModeller",
                  Publisher:="xie.guigang@gcmodeller.org")>
Module CLI

    <ExportAPI("-Draw", Usage:="-draw /network <net_file> /parser <xgmml/cyjs> [-size <width,height> -out <out_image> /style <style_file> /style_parser <vizmap/json>]",
               Info:="Drawing a network image visualization based on the generate network layout from the officials cytoscape software.")>
    Public Function DrawingInvoke(argvs As CommandLine.CommandLine) As Integer
        Dim Size As Size = argvs.GetObject(Of Size)("-size", AddressOf getSize)
        Dim Output As String = argvs("-out")
        Dim Style As LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Cytoscape.DocumentFormat.Visualization.VizMap =
            argvs.GetObject(Of VizMap)("/style", argvs.GetObject(Of Func(Of String, VizMap))("/style_parser", AddressOf getStyleParser))

        Dim NetworkGraph = argvs.GetObject(Of Graph)("/network", argvs.GetObject(Of Func(Of String, Graph))("/parser", AddressOf getNetworkParser))
        Dim res As Image = Nothing

        If Style Is Nothing Then
            Call $"{NameOf(Style)} data is nothing, irnored of the drawing styles...".__DEBUG_ECHO
            res = LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Cytoscape.DocumentFormat.CytoscapeGraphView.GraphDrawing.InvokeDrawing(NetworkGraph, Size)
        Else

        End If

        If String.IsNullOrEmpty(Output) Then
            Output = argvs("/network") & ".png"
        End If

        Call res.Save(Output, Imaging.ImageFormat.Png)

        Return 0
    End Function

    Private Function getNetworkParser(name As String) As Func(Of String, LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Cytoscape.DocumentFormat.CytoscapeGraphView.Graph)
        If String.Equals(name, "xgmml", StringComparison.OrdinalIgnoreCase) Then
            Return Function(path As String) LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Cytoscape.DocumentFormat.CytoscapeGraphView.Graph.Load(path)
        ElseIf String.Equals(name, "cyjs", StringComparison.OrdinalIgnoreCase) Then
            Return AddressOf CLI.cyjsAsGraph
        Else
            Call $"Network file parser ""{name}"" was not recognized!".__DEBUG_ECHO
            Return Nothing
        End If
    End Function

    Private Function cyjsAsGraph(cyjs As String) As LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Cytoscape.DocumentFormat.CytoscapeGraphView.Graph
        Dim jsonText As String = IO.File.ReadAllText(cyjs)
        Dim json As Cyjs.Cyjs = (jsonText).LoadObject(Of Cyjs.Cyjs)
        Return json.ToGraphModel
    End Function

    Private Function getStyleParser(name As String) As Func(Of String, VizMap)
        If String.Equals(name, "vizmap", StringComparison.OrdinalIgnoreCase) Then
            Return Function(path As String) path.LoadXml(Of LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Cytoscape.DocumentFormat.Visualization.VizMap)(ThrowEx:=False)
        ElseIf String.Equals(name, "json", StringComparison.OrdinalIgnoreCase)
        Else
            Call $"Network style file parser ""{name}"" was not recognized!".__DEBUG_ECHO
            Return Nothing
        End If

        Throw New NotImplementedException
    End Function

End Module
