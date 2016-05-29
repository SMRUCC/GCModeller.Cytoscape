﻿Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.DocumentFormat.Csv
Imports Microsoft.VisualBasic
Imports ______NETWORK__ = Microsoft.VisualBasic.DataVisualization.Network.FileStream.Network(Of
    Microsoft.VisualBasic.DataVisualization.Network.FileStream.Node,
    Microsoft.VisualBasic.DataVisualization.Network.FileStream.NetworkEdge)
Imports Microsoft.VisualBasic.Linq.Extensions
Imports LANS.SystemsBiology.Assembly.KEGG.DBGET
Imports LANS.SystemsBiology.Assembly.KEGG.Archives.Xml
Imports Microsoft.VisualBasic.DataVisualization.Network
Imports LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Cytoscape.NetworkModel.PfsNET
Imports LANS.SystemsBiology.AnalysisTools.NBCR.Extensions.MEME_Suite.Analysis.GenomeMotifFootPrints
Imports Cytoscape.GCModeller.FileSystem
Imports Cytoscape.GCModeller.FileSystem.KEGG.Directories
Imports LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Cytoscape.NetworkModel.KEGG.ReactionNET
Imports LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Cytoscape.NetworkModel.KEGG
Imports LANS.SystemsBiology.Assembly.KEGG.Archives.Xml.Nodes
Imports LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Cytoscape.DocumentFormat

Partial Module CLI

    <ExportAPI("--mod.regulations",
               Usage:="--mod.regulations /model <KEGG.xml> /footprints <footprints.csv> /out <outDIR> [/pathway /class /type]")>
    <ParameterInfo("/class", True, Description:="This parameter can not be co-exists with /type parameter")>
    <ParameterInfo("/type", True, Description:="This parameter can not be co-exists with /class parameter")>
    Public Function ModuleRegulations(args As CommandLine.CommandLine) As Integer
        Dim Model = args("/model").LoadXml(Of XmlModel)
        Dim Footprints = args("/footprints").LoadCsv(Of PredictedRegulationFootprint)

        Footprints = (From x In Footprints.AsParallel Where Not String.IsNullOrEmpty(x.Regulator) Select x).ToList

        Dim Networks = GeneInteractions.ExportPathwayGraph(Model)
        Dim regulators = Footprints.ToArray(Function(x) x.Regulator).Distinct.ToArray(
            Function(x) New FileStream.Node With {
                .Identifier = x,
                .NodeType = "TF"
            })
        Dim regulations = (From x In Footprints
                           Let regulation = New FileStream.NetworkEdge With {
                               .Confidence = x.Pcc,
                               .FromNode = x.Regulator,
                               .ToNode = x.ORF,
                               .InteractionType = "Regulation"
                           }
                           Select regulation
                           Group regulation By regulation.ToNode Into Group) _
                               .ToDictionary(Function(x) x.ToNode,
                                             Function(x) x.Group.ToArray)
        Dim outDIR As String = FileIO.FileSystem.GetDirectoryInfo(args("/out")).FullName

        If args.GetBoolean("/pathway") Then
            Networks = __pathwayNetwork(Model, Networks)
        End If

        If args.GetBoolean("/class") Then
            Networks = __classNetwork(Model, Networks)
        ElseIf args.GetBoolean("/type") Then
            Networks = __typeNetwork(Model, Networks)
        End If

        For Each kMod In Networks
            Dim Edges = kMod.Value.Nodes.ToArray(Function(x) regulations.TryGetValue(x.Identifier)).MatrixToList
            Dim Path As String = $"{outDIR}/{kMod.Key}/"

            If Edges.IsNullOrEmpty Then
                Continue For
            End If

            Call kMod.Value.Nodes.Add(regulators)
            Call kMod.Value.Edges.Add(Edges)
            Call kMod.Value.Save(Path, Encodings.UTF8)
        Next

        Return 0
    End Function

    ''' <summary>
    ''' 基因表达调控网络细胞表型大分类
    ''' </summary>
    ''' <param name="model"></param>
    ''' <param name="networks"></param>
    ''' <returns></returns>
    Private Function __typeNetwork(model As XmlModel, networks As Dictionary(Of String, ______NETWORK__)) As Dictionary(Of String, ______NETWORK__)
        Call $"Merge {networks.Count} network by type....".__DEBUG_ECHO

        Dim classes = (From x As PwyBriteFunc
                       In model.Pathways
                       Select x
                       Group x By x.Class Into Group) _
                            .ToDictionary(Function(x) x.Class,
                                          Function(x) x.Group.ToArray(
                                          Function(xx) xx.Pathways.ToArray(
                                          Function(xxx) networks.TryGetValue(xxx.EntryId))).MatrixToList)
        Dim dict As Dictionary(Of String, ______NETWORK__) = classes.ToDictionary(Function(x) x.Key,
                                                                                  Function(x) __mergeCommon(x.Value))
        Return dict
    End Function

    ''' <summary>
    ''' 基因表达调控网络按照细胞表型小分类聚合
    ''' </summary>
    ''' <param name="model">KEGG细胞表型分类</param>
    ''' <param name="networks"></param>
    ''' <returns></returns>
    Private Function __classNetwork(model As XmlModel, networks As Dictionary(Of String, ______NETWORK__)) As Dictionary(Of String, ______NETWORK__)
        Call $"Merge {networks.Count} network by class category....".__DEBUG_ECHO

        Dim classes = (From x As PwyBriteFunc
                       In model.Pathways
                       Select x
                       Group x By x.Category Into Group) _
                            .ToDictionary(Function(x) x.Category, elementSelector:=
                                          Function(x) x.Group.ToArray(
                                          Function(xx) xx.Pathways.ToArray(
                                          Function(xxx) networks.TryGetValue(xxx.EntryId))).MatrixToList)
        Dim dict = classes.ToDictionary(Function(x) x.Key,
                                        Function(x) __mergeCommon(x.Value))
        Return dict
    End Function

    Private Function __mergeCommon(source As Generic.IEnumerable(Of ______NETWORK__)) As ______NETWORK__
        Dim Nods = source.ToArray(Function(x) x.Nodes, where:=Function(x) Not x Is Nothing).MatrixToList
        Dim Edges As List(Of FileStream.NetworkEdge) =
            source.ToArray(Function(x) x.Edges, where:=Function(x) Not x Is Nothing).MatrixToList

        Dim __nodes = (From node
                       In (From node As FileStream.Node
                           In Nods
                           Select node
                           Group node By node.Identifier Into Group)
                       Select New FileStream.Node With {
                           .Identifier = node.Identifier,
                           .NodeType = node.Group.ToArray.ToArray(Function(x) x.NodeType).Distinct.ToArray.JoinBy("; ")}).ToArray
        Dim __edges = (From edge As FileStream.NetworkEdge
                       In Edges
                       Select edge,
                           id = edge.GetDirectedGuid
                       Group By id Into Group).ToArray(Function(x) x.Group.First.edge)
        Dim net As ______NETWORK__ = New ______NETWORK__ With {
            .Edges = __edges,
            .Nodes = __nodes
        }
        Return net
    End Function

    ''' <summary>
    ''' 将Module视图转换为Pathway视图
    ''' </summary>
    ''' <param name="model"></param>
    ''' <param name="networks"></param>
    ''' <returns></returns>
    Private Function __pathwayNetwork(model As XmlModel, networks As Dictionary(Of String, ______NETWORK__)) As Dictionary(Of String, ______NETWORK__)
        Dim dict As New Dictionary(Of String, ______NETWORK__)

        For Each ph As bGetObject.Pathway In model.GetAllPathways
            If ph.Modules.IsNullOrEmpty Then
                Continue For
            End If

            Dim LQuery = (From m In ph.Modules
                          Let km = networks.TryGetValue(m.Key)
                          Where Not km Is Nothing
                          Select km).ToArray
            Dim net = __mergeCommon(LQuery)

            Call dict.Add(ph.EntryId, net)
        Next

        Return dict
    End Function

    <ExportAPI("/reaction.NET", Usage:="/reaction.NET [/model <xmlModel.xml> /source <rxn.DIR> /out <outDIR>]")>
    Public Function ReactionNET(args As CommandLine.CommandLine) As Integer
        Dim source As String = TryGetSource(args("/source"), AddressOf GetReactions)
        Dim model As String = args("/model")
        Dim out As String
        If Not String.IsNullOrEmpty(model) Then
            out = model.TrimFileExt & ".ReactionNET/"
            Dim bMods As XmlModel = model.LoadXml(Of XmlModel)
            Dim net As FileStream.Network = ModelNET(bMods, source)
            Return net.Save(out, Encodings.ASCII.GetEncodings).CLICode
        Else
            out = args.GetValue("/out", source & ".ReactionNET/")
            Dim net As FileStream.Network = BuildNET(source)
            Return net.Save(out, Encodings.ASCII.GetEncodings).CLICode
        End If
    End Function

    ''' <summary>
    ''' 基因和模块之间的从属关系，附加调控信息
    ''' </summary>
    ''' <param name="args"></param>
    ''' <returns></returns>
    <ExportAPI("/KEGG.Mods.NET",
               Usage:="/KEGG.Mods.NET /in <mods.xml.DIR> [/out <outDIR> /pathway /footprints <footprints.Csv> /brief /cut 0 /pcc 0]")>
    <ParameterInfo("/brief", True,
                   Description:="If this parameter is represented, then the program just outs the modules, all of the non-pathway genes wil be removes.")>
    Public Function ModsNET(args As CommandLine.CommandLine) As Integer
        Dim inDIR As String = args("/in")
        Dim isPathway As Boolean = args.GetBoolean("/pathway")
        Dim net = If(isPathway,
            LoadPathways(inDIR).BuildNET,
            LoadModules(inDIR).BuildNET)
        Dim out As String = args.GetValue("/out", inDIR & ".modsNET/")
        Dim footprint As String = args("/footprints")
        Dim cut As Double = args.GetValue("/cut", 0.0R)
        Dim nulls As FileStream.Network = Nothing

        If footprint.FileExists Then
            Dim brief As Boolean = args.GetBoolean("/brief")
            Dim footprints As IEnumerable(Of RegulatesFootprints) =
                footprint.LoadCsv(Of RegulatesFootprints)

            Dim pcc As Double = args.GetValue("/pcc", 0R)

            If pcc <> 0R Then
                footprints = (From x In footprints Where Math.Abs(x.Pcc) >= pcc Select x).ToArray
            End If

            Call net.AddFootprints(footprints, brief)
            If brief Then
                Dim LQuery = (From x As FileStream.NetworkEdge
                              In net.Edges
                              Where String.Equals(x.InteractionType, PathwayGene)
                              Select x
                              Group x By x.FromNode Into Group)  ' 代谢途径基因按照模块分组
                Dim rhaves As String() = footprints.ToArray(Function(x) x.ORF).Distinct.ToArray
                Dim Trim = (From m In LQuery
                            Where (From x As FileStream.NetworkEdge In m.Group
                                   Where Array.IndexOf(rhaves, x.ToNode) > -1
                                   Select x).FirstOrDefault Is Nothing
                            Select m).ToArray
                nulls = New FileStream.Network + Trim.ToArray(Function(x) x.Group).MatrixAsIterator ' 添加新的网络节点
                net -= nulls.Edges  ' 删除旧的网络节点
                nulls += net <= nulls.Edges.ToArray(Function(x) {x.FromNode, x.ToNode}).MatrixAsIterator
                net -= nulls.Nodes
            End If
        End If

        If cut <> 0R Then  ' 按照阈值筛选
            net.Edges = (From x In net.Edges Where Math.Abs(x.Confidence) >= cut Select x).ToArray
            out = out & "." & cut
        End If

        If Not nulls Is Nothing Then
            Call nulls.Save(out & "/no-regs/", Encodings.ASCII)
        End If
        Return net.Save(out, Encodings.ASCII).CLICode
    End Function
End Module