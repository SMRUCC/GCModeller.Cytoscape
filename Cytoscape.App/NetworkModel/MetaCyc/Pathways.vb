Imports Microsoft.VisualBasic.DocumentFormat.Csv.Extensions
Imports Microsoft.VisualBasic

Namespace NetworkModel

    Public Class Pathways

        Protected _MetaCyc As LANS.SystemsBiology.Assembly.MetaCyc.File.FileSystem.DatabaseLoadder

        Sub New(MetaCyc As LANS.SystemsBiology.Assembly.MetaCyc.File.FileSystem.DatabaseLoadder)
            _MetaCyc = MetaCyc
        End Sub

        ''' <summary>
        ''' 导出代谢途径的网络
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function Export(Dir As String) As Integer
            Dim Edges As Microsoft.VisualBasic.DataVisualization.Network.FileStream.NetworkEdge() = Nothing, Nodes As Pathways.Pathway() = Nothing
            Call Export(Edges, Nodes)

            Call Edges.SaveTo(String.Format("{0}/Edges.csv", Dir), False)
            Call Nodes.SaveTo(String.Format("{0}/Nodes.csv", Dir), False)

            Return 0
        End Function

        Protected Sub Export(ByRef Edges As Microsoft.VisualBasic.DataVisualization.Network.FileStream.NetworkEdge(), ByRef Nodes As Pathways.Pathway())
            Dim Pathways = _MetaCyc.GetPathways
            Dim Network As List(Of Microsoft.VisualBasic.DataVisualization.Network.FileStream.NetworkEdge) =
                New List(Of Microsoft.VisualBasic.DataVisualization.Network.FileStream.NetworkEdge)
            Dim NodeList As List(Of Pathway) = New List(Of Pathway)

            Dim EnyzmeAnalysis As MetaCycPathways = New MetaCycPathways(Me._MetaCyc)
            Dim AnalysisResult = EnyzmeAnalysis.Performance
            Dim GeneObjects = _MetaCyc.GetGenes

            For Each Pwy In Pathways
                Dim LQuery = (From pwyItem
                    In Pwy.PathwayLinks
                              Select New LANS.SystemsBiology.Assembly.MetaCyc.Schema.Metabolism.PathwayLink(pwyItem)).ToArray  'interaction list

                If LQuery.IsNullOrEmpty Then
                    Call Network.Add(New Microsoft.VisualBasic.DataVisualization.Network.FileStream.NetworkEdge With {.FromNode = Pwy.Identifier})
                Else
                    Call Network.AddRange(GenerateLinks(LQuery, Pwy.Identifier))
                End If

                If Not Pwy.InPathway.IsNullOrEmpty Then
                    Call Network.AddRange((From Id As String
                                           In Pwy.InPathway
                                           Select New Microsoft.VisualBasic.DataVisualization.Network.FileStream.NetworkEdge With {
                                               .FromNode = Id.ToUpper,
                                               .InteractionType = "Contains",
                                               .ToNode = Pwy.Identifier}).ToArray)
                End If

                Dim AssociatedEnzymes = AnalysisResult.GetItem(Pwy.Identifier).AssociatedGenes
                AssociatedEnzymes = (From GeneObject In GeneObjects.Takes(AssociatedEnzymes) Select GeneObject.Accession1 Distinct Order By Accession1 Ascending).ToArray

                Call NodeList.Add(New Pathway With {.Identifier = Pwy.Identifier, .GeneObjects = AssociatedEnzymes, .EnzymeCounts = AssociatedEnzymes.Count,
                                                    .SuperPathway = Not Pwy.SubPathways.IsNullOrEmpty,
                                                    .ReactionCounts = Pwy.ReactionList.Count, .CommonName = Pwy.CommonName})
            Next

            Edges = Network.ToArray
            Nodes = NodeList.ToArray
        End Sub

        Public Class Pathway : Implements Microsoft.VisualBasic.ComponentModel.Collection.Generic.sIdEnumerable

            Public Property Identifier As String Implements Microsoft.VisualBasic.ComponentModel.Collection.Generic.sIdEnumerable.Identifier
            Public Property ReactionCounts As Integer
            Public Property EnzymeCounts As Integer
            Public Property CommonName As String
            Public Property GeneObjects As String()
            Public Property SuperPathway As Boolean

            Public Overrides Function ToString() As String
                Return Identifier
            End Function
        End Class

        Public Function GenerateLinks(pwy As LANS.SystemsBiology.Assembly.MetaCyc.Schema.Metabolism.PathwayLink(),
                                      UniqueId As String) As Microsoft.VisualBasic.DataVisualization.Network.FileStream.NetworkEdge()
            Dim EdgeList As List(Of Microsoft.VisualBasic.DataVisualization.Network.FileStream.NetworkEdge) =
                New List(Of Microsoft.VisualBasic.DataVisualization.Network.FileStream.NetworkEdge)

            For Each Link In pwy
                Dim LQuery = (From item In Link.LinkedPathways
                              Let iter As String = If(item.LinkType = LANS.SystemsBiology.Assembly.MetaCyc.Schema.Metabolism.PathwayLink.PathwaysLink.LinkTypes.NotSpecific,
                                  "interact_with",
                                  item.LinkType.ToString)
                              Select New Microsoft.VisualBasic.DataVisualization.Network.FileStream.NetworkEdge With {
                                  .FromNode = UniqueId,
                                  .InteractionType = iter,
                                  .ToNode = item.Id.Replace("|", "").ToUpper}).ToArray
                Call EdgeList.AddRange(LQuery)
            Next

            Return EdgeList.ToArray
        End Function
    End Class
End Namespace