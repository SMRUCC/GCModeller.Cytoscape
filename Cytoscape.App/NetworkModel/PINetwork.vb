﻿#Region "Microsoft.VisualBasic::2a6580275d78b4b02647e3e0e124c953, visualize\Cytoscape\Cytoscape.App\NetworkModel\PINetwork.vb"

' Author:
' 
'       asuka (amethyst.asuka@gcmodeller.org)
'       xie (genetics@smrucc.org)
'       xieguigang (xie.guigang@live.com)
' 
' Copyright (c) 2018 GPL3 Licensed
' 
' 
' GNU GENERAL PUBLIC LICENSE (GPL3)
' 
' 
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
' 
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY; without even the implied warranty of
' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
' GNU General Public License for more details.
' 
' You should have received a copy of the GNU General Public License
' along with this program. If not, see <http://www.gnu.org/licenses/>.



' /********************************************************************************/

' Summaries:

'     Module PINetwork
' 
'         Function: __attributes, __edgeModel, BuildModel
' 
' 
' /********************************************************************************/

#End Region

Imports System.Text.RegularExpressions
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Data.Repository
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports SMRUCC.genomics.Assembly.NCBI.GenBank.TabularFormat
Imports SMRUCC.genomics.Assembly.NCBI.GenBank.TabularFormat.ComponentModels
Imports SMRUCC.genomics.ComponentModel.Loci
Imports SMRUCC.genomics.foundation
Imports SMRUCC.genomics.foundation.psidev.XML
Imports SMRUCC.genomics.Visualize.Cytoscape.CytoscapeGraphView.XGMML
Imports SMRUCC.genomics.Visualize.Cytoscape.CytoscapeGraphView.XGMML.File

Namespace NetworkModel.StringDB

    ''' <summary>
    ''' 构建蛋白质互作网络的绘图模型
    ''' </summary>
    ''' 
    <Package("String-Db.Interactions", Category:=APICategories.ResearchTools, Publisher:="xie.guigang@gmail.com")>
    Public Module PINetwork

        <ExportAPI("Build", Info:="Build the protein interaction network cytoscape visualization model file.")>
        Public Function BuildModel(PTT As PTT,
                                   <Parameter("DIR.string-DB")> stringDB As String,
                                   <Parameter("Trim.Confidence")> Optional TrimConfidence As Double = -1,
                                   <Parameter("Trim.Degree")> Optional TrimDegree As Integer = 0) As XGMMLgraph

            Dim Model As XGMMLgraph = XGMMLgraph.CreateObject(
                    Title:=PTT.Title & " - Protein interaction network",
                    Type:="Protein Interaction",
                    Description:=$"Prediction protein interaction network from string-db.org, {PTT.Title}, {PTT.NumOfProducts} proteins.")

            Model.nodes = (From GeneObject As GeneBrief
                           In PTT.GeneObjects.AsParallel
                           Select New XGMMLnode With {
                               .label = GeneObject.Synonym,
                               .attributes = __attributes(GeneObject)}).WriteAddress '使用PTT文件首先生成节点

            Dim Network As New List(Of XGMMLedge)

            For Each iteraction In stringDB.LoadSourceEntryList({"*.xml"})      ' string-db数据库是用来生成网络之中的边的
                For Each itr As psidev.XML.Entry In iteraction.Value.LoadXml(Of EntrySet).Entries
                    Network += From edge As psidev.XML.Interaction
                               In itr.InteractionList
                               Let edgeModel = __edgeModel(edge, Model, itr)
                               Let conf = Val(edgeModel.Value("ConfidenceList-likelihood")?.Value)
                               Where conf >= TrimConfidence
                               Select edgeModel
                Next
                Call Console.Write(".")
            Next

            Model.Edges = Network.WriteAddress

            Dim nodes = Model.nodes.ToDictionary(Function(obj) obj.id,
                                                 Function(obj) New Value(Of Integer)(0))
            Dim index As New GraphIndex(Model)

            For Each edge As XGMMLedge In Model.edges
                nodes(edge.source).Value += 1
                nodes(edge.target).Value += 1
            Next

            For Each node In nodes
                index.GetNode(node.Key).AddAttribute("Degree", node.Value.Value, ATTR_VALUE_TYPE_REAL)
            Next

            If TrimDegree > -1 Then
                Dim LQuery As Integer() =
                    LinqAPI.Exec(Of Integer) <= From x In nodes
                                                Where x.Value.Value >= TrimDegree
                                                Select x.Key
                Model.nodes =
                    LinqAPI.Exec(Of XGMMLnode) <= From node As XGMMLnode
                                                   In Model.nodes
                                                  Where Array.IndexOf(LQuery, node.id) > -1
                                                  Select node
                LQuery =
                    LinqAPI.Exec(Of Integer) <= From x In nodes
                                                Where x.Value.Value < TrimDegree
                                                Select x.Key
                Model.edges =
                    LinqAPI.Exec(Of XGMMLedge) <= From edge As XGMMLedge
                                                  In Model.edges.AsParallel
                                                  Where Not edge.ContainsOneOfNode(LQuery)
                                                  Select edge
            End If

            Return Model
        End Function

        Private Function __edgeModel(edge As psidev.XML.Interaction, Model As XGMMLgraph, itr As Entry) As XGMMLedge
            Dim EdgeModel As New XGMMLedge
            Dim source As String = itr.GetInteractor(edge.ParticipantList.First.InteractorRef).Synonym
            Dim target As String = itr.GetInteractor(edge.ParticipantList.Last.InteractorRef).Synonym
            Dim index As New GraphIndex(Model)

            EdgeModel.source = index.GetNode(source).id
            EdgeModel.target = index.GetNode(target).id
            EdgeModel.label = $"{source}::{target}"

            Dim attrs As New List(Of Attribute)
            attrs += New Attribute With {
                .Type = ATTR_VALUE_TYPE_REAL,
                .name = $"{NameOf(edge.ConfidenceList)}-{edge.ConfidenceList.First.Unit.Names.shortLabel}",
                .Value = edge.ConfidenceList.First.value
            }

            Dim experiment = itr.GetExperiment(edge.ExperimentList.First.value)

            If Not experiment Is Nothing Then
                Dim name As String =
                    If(experiment.Names Is Nothing,
                    experiment.interactionDetectionMethod.Names.shortLabel,
                    experiment.Names.shortLabel)

                attrs += New Attribute With {
                    .Type = ATTR_VALUE_TYPE_STRING,
                    .name = $"{NameOf(edge.ExperimentList)}-{name}",
                    .Value = experiment.Bibref.Xref.primaryRef.db & ": " & experiment.Bibref.Xref.primaryRef.id
                }
            End If

            EdgeModel.attributes = attrs.ToArray

            Return EdgeModel
        End Function

        Private Function __attributes(GeneObject As GeneBrief) As Attribute()
            Dim List As New List(Of Attribute)

            List += New Attribute With {
                .Type = ATTR_VALUE_TYPE_STRING,
                .name = NameOf(GeneBrief.Product),
                .Value = GeneObject.Product
            }
            List += New Attribute With {
                .Type = ATTR_VALUE_TYPE_STRING,
                .name = NameOf(GeneBrief.PID),
                .Value = GeneObject.PID
            }
            List += New Attribute With {
                .Type = ATTR_VALUE_TYPE_STRING,
                .name = NameOf(GeneBrief.COG),
                .Value = Regex.Replace(GeneObject.COG, "COG\d+", "", RegexOptions.IgnoreCase)
            }
            List += New Attribute With {
                .Type = ATTR_VALUE_TYPE_STRING,
                .name = NameOf(NucleotideLocation.Strand),
                .Value = GeneObject.Location.Strand.ToString
            }

            Return List.ToArray
        End Function
    End Module
End Namespace
