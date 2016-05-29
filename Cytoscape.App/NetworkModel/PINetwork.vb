Imports Microsoft.VisualBasic.Scripting.MetaData
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports System.Text.RegularExpressions
Imports LANS.SystemsBiology.Assembly.NCBI.GenBank.TabularFormat
Imports LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Cytoscape.DocumentFormat
Imports LANS.SystemsBiology.Assembly.NCBI.GenBank.TabularFormat.ComponentModels
Imports LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Cytoscape.DocumentFormat.CytoscapeGraphView.DocumentElements
Imports LANS.SystemsBiology.DatabaseServices.StringDB
Imports Microsoft.VisualBasic.ComponentModel
Imports Microsoft.VisualBasic
Imports LANS.SystemsBiology.ComponentModel.Loci

Namespace NetworkModel.StringDB

    ''' <summary>
    ''' 构建蛋白质互作网络的绘图模型
    ''' </summary>
    ''' 
    <[PackageNamespace]("String-Db.Interactions", Category:=APICategories.ResearchTools, Publisher:="xie.guigang@gmail.com")>
    Public Module PINetwork

        <ExportAPI("Build", Info:="Build the protein interaction network cytoscape visualization model file.")>
        Public Function BuildModel(PTT As PTT,
                                   <Parameter("DIR.string-DB")> stringDB As String,
                                   <Parameter("Trim.Confidence")> Optional TrimConfidence As Double = -1,
                                   <Parameter("Trim.Degree")> Optional TrimDegree As Integer = 0) As CytoscapeGraphView.Graph

            Dim Model As CytoscapeGraphView.Graph =
                CytoscapeGraphView.Graph.CreateObject(
                    Title:=PTT.Title & " - Protein interaction network",
                    Type:="Protein Interaction",
                    Description:=$"Prediction protein interaction network from string-db.org, {PTT.Title}, {PTT.NumOfProducts} proteins.")

            Model.Nodes = (From GeneObject As GeneBrief
                           In PTT.GeneObjects.AsParallel
                           Select New Node With {
                               .LabelTitle = GeneObject.Synonym,
                               .Attributes = __attributes(GeneObject)}).ToArray.AddHandle '使用PTT文件首先生成节点

            Dim Network As New List(Of Edge)

            For Each iteraction In stringDB.LoadSourceEntryList({"*.xml"})      ' string-db数据库是用来生成网络之中的边的
                For Each itr As MIF25.Nodes.Entry In iteraction.Value.LoadXml(Of MIF25.EntrySet).Entries
                    Call Network.AddRange((From edge In itr.InteractionList
                                           Let edgeModel = __edgeModel(edge, Model, itr)
                                           Let conf = Val(edgeModel.Value("ConfidenceList-likelihood")?.Value)
                                           Where conf >= TrimConfidence
                                           Select edgeModel).ToArray)
                Next
                Call Console.Write(".")
            Next

            Model.Edges = Network.ToArray.AddHandle

            Dim nodes = Model.Nodes.ToDictionary(Function(obj) obj.IDPointer,
                                                 Function(obj) New Value(Of Integer)(0))
            For Each edge In Model.Edges
                nodes(edge.source).Value += 1
                nodes(edge.target).Value += 1
            Next

            For Each node In nodes
                Model.GetNode(node.Key).AddAttribute("Degree", node.Value.Value, Attribute.ATTR_VALUE_TYPE_REAL)
            Next

            If TrimDegree > -1 Then

                Dim LQuery As Integer() = (From x In nodes Where x.Value.Value >= TrimDegree Select x.Key).ToArray
                Model.Nodes = (From node As Node In Model.Nodes Where Array.IndexOf(LQuery, node.IDPointer) > -1 Select node).ToArray
                LQuery = (From item In nodes Where item.Value.Value < TrimDegree Select item.Key).ToArray
                Model.Edges = (From edge As Edge In Model.Edges.AsParallel Where Not edge.ContainsOneOfNode(LQuery) Select edge).ToArray
            End If

            Return Model
        End Function

        Private Function __edgeModel(edge As MIF25.Nodes.Interaction, Model As CytoscapeGraphView.Graph, itr As MIF25.Nodes.Entry) As Edge
            Dim EdgeModel As Edge = New Edge
            Dim source As String = itr.GetInteractor(edge.ParticipantList.First.InteractorRef).Synonym
            Dim target As String = itr.GetInteractor(edge.ParticipantList.Last.InteractorRef).Synonym

            EdgeModel.source = Model.GetNode(source).IDPointer
            EdgeModel.target = Model.GetNode(target).IDPointer
            EdgeModel.Label = $"{source}::{target}"

            Dim attrs As New List(Of Attribute)
            Call attrs.Add(New Attribute With {
                           .Type = Attribute.ATTR_VALUE_TYPE_REAL,
                           .Name = $"{NameOf(edge.ConfidenceList)}-{edge.ConfidenceList.First.Unit.Names.ShortLabel}",
                           .Value = edge.ConfidenceList.First.value})

            Dim experiment = itr.GetExperiment(edge.ExperimentList.First.value)

            If Not experiment Is Nothing Then
                Call attrs.Add(New Attribute With {
                         .Type = Attribute.ATTR_VALUE_TYPE_STRING,
                         .Name = $"{NameOf(edge.ExperimentList)}-{If(experiment.Names Is Nothing, experiment.interactionDetectionMethod.Names.ShortLabel, experiment.Names.ShortLabel)}",
                         .Value = experiment.Bibref.Xref.PrimaryReference.Db & ": " & experiment.Bibref.Xref.PrimaryReference.Id})
            End If

            EdgeModel.Attributes = attrs.ToArray

            Return EdgeModel
        End Function

        Private Function __attributes(GeneObject As GeneBrief) As Attribute()
            Dim List As New List(Of Attribute)

            List += New Attribute With {
                .Type = Attribute.ATTR_VALUE_TYPE_STRING,
                .Name = NameOf(GeneBrief.Product),
                .Value = GeneObject.Product
            }
            List += New Attribute With {
                .Type = Attribute.ATTR_VALUE_TYPE_STRING,
                .Name = NameOf(GeneBrief.PID),
                .Value = GeneObject.PID
            }
            List += New Attribute With {
                .Type = Attribute.ATTR_VALUE_TYPE_STRING,
                .Name = NameOf(GeneBrief.COG),
                .Value = Regex.Replace(GeneObject.COG, "COG\d+", "", RegexOptions.IgnoreCase)
            }
            List += New Attribute With {
                .Type = Attribute.ATTR_VALUE_TYPE_STRING,
                .Name = NameOf(NucleotideLocation.Strand),
                .Value = GeneObject.Location.Strand.ToString
            }

            Return List.ToArray
        End Function

    End Module
End Namespace