﻿Imports System.Text
Imports System.Xml.Serialization
Imports LANS.SystemsBiology.Assembly
Imports LANS.SystemsBiology.Assembly.MetaCyc.File.DataFiles
Imports LANS.SystemsBiology.Assembly.MetaCyc.File.FileSystem
Imports Microsoft.VisualBasic.ComponentModel
Imports Microsoft.VisualBasic.ComponentModel.Collection.Generic
Imports Microsoft.VisualBasic.DocumentFormat.Csv
Imports Microsoft.VisualBasic
Imports Microsoft.VisualBasic.Language

''' <summary>
''' 整理出代谢途径和相应的基因，对于基因个数少于5的代谢途径，其被合并至其他较大的SuperPathway之中去
''' </summary>
''' <remarks></remarks>
Public Class MetaCycPathways

    Dim MetaCyc As DatabaseLoadder

    Sub New(MetaCyc As DatabaseLoadder)
        Me.MetaCyc = MetaCyc
    End Sub

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks>
    ''' 过程描述：
    ''' 1. 获取所有的代谢途径的数据
    ''' 2. 构建所有的反应对象与基因之间的相互联系
    ''' 3. 根据Reaction-List属性值列表将基因与相应的代谢途径建立联系，最后输出数据
    ''' </remarks>
    Public Function Performance() As Pathway()
        Dim pwys As Pathways = MetaCyc.GetPathways
        Dim rxnGeneLinks As Dictionary(Of String, String()) =
            New AssignGene(MetaCyc).Performance
        Dim pathways As Pathway() = (From pwy As Slots.Pathway
                                     In pwys
                                     Select __generatePwy(pwy, rxnGeneLinks)).ToArray

        For i As Integer = 0 To pathways.Length - 1
            Dim pway = pathways(i)
            If pway.SuperPathway Then
                pway.ContiansSubPathway =
                    LinqAPI.Exec(Of Pathway) <= From pwy As Pathway
                                                In pathways
                                                Where pway.MetaCycBaseType.SubPathways.IndexOf(pwy.Identifier) > -1
                                                Select pwy
            End If
        Next
        Return pathways
    End Function

    Private Function __generatePwy(pwyObj As Slots.Pathway, RxnGeneLinks As Dictionary(Of String, String())) As Pathway
        Dim pathway As New Pathway With {
            .Identifier = pwyObj.Identifier,
            .SuperPathway = pwyObj.Types.IndexOf("Super-Pathways") > -1,
            .MetaCycBaseType = pwyObj
        } ' 实例化一个返回对象
        pathway.ReactionList =
            LinqAPI.Exec(Of Key_strArrayValuePair) <=
                From rxnId As String
                In pwyObj.ReactionList
                Where RxnGeneLinks.ContainsKey(rxnId)
                Select New Key_strArrayValuePair With {
                    .Key = rxnId,
                    .Value = RxnGeneLinks(rxnId)
                }     '获取反应对象列表
        Return pathway
    End Function

    <XmlType("pwy")> Public Class Pathway : Implements sIdEnumerable
        Public Property MetaCycBaseType As Slots.Pathway
        Public Property Identifier As String Implements sIdEnumerable.Identifier
        ''' <summary> 
        ''' 本代谢途径是否为一个超途径
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Property SuperPathway As Boolean
        Public Property ReactionList As Key_strArrayValuePair()
        ''' <summary>
        ''' 本代谢途径所包含的的亚途径
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Property ContiansSubPathway As Pathway()

        Public ReadOnly Property AssociatedGenes As String()
            Get
                Dim List As List(Of String) = New List(Of String)
                For Each rxn In ReactionList
                    Call List.AddRange(rxn.Value)
                Next
                If SuperPathway Then
                    For Each pwy In ContiansSubPathway
                        Call List.AddRange(pwy.AssociatedGenes)
                    Next
                End If
                Return (From s As String In List Select s Distinct Order By s Ascending).ToArray
            End Get
        End Property

        Public Overrides Function ToString() As String
            If SuperPathway Then
                Return String.Format("{0}, {1} reactions and {2} sub-pathways", Identifier, ReactionList.Count, ContiansSubPathway.Count)
            Else
                Return String.Format("{0}, {1} reactions.", Identifier, ReactionList.Count)
            End If
        End Function
    End Class

    Public Shared Function GenerateReport(Pathways As Pathway(), Genes As Genes) As DocumentStream.File
        Dim File As DocumentStream.File = New DocumentStream.File
        Dim sBuilder As StringBuilder = New StringBuilder(1024)
        File.AppendLine(New String() {"UniqueId", "Description", "Is_Super_Pathway?", "Associated_gene_counts", "Associated_gene_list"})
        For Each pwy In Pathways
            Dim row As DocumentStream.RowObject = New DocumentStream.RowObject
            Call row.AddRange(New String() {pwy.Identifier, pwy.MetaCycBaseType.CommonName, pwy.SuperPathway})
            Dim GeneCollection = (From gene In Genes.Takes(pwy.AssociatedGenes) Select gene.Accession1).ToArray  '
            Call row.Add(GeneCollection.Count)
            Call sBuilder.Clear()
            For Each Id As String In GeneCollection
                Call sBuilder.Append(Id & "; ")
            Next
            Call row.Add(sBuilder.ToString)
            Call File.AppendLine(row)
        Next

        Return File
    End Function

    Public Shared Function CreateGeneCollection(Pathways As Pathway(), Genes As Genes, ProteinDomains As DocumentStream.File) As DocumentStream.File
        Dim List As List(Of String) = New List(Of String)

        For Each pwy In Pathways
            List += From gene As Slots.Gene
                    In Genes.Takes(pwy.AssociatedGenes)
                    Select gene.Accession1
        Next

        Dim File As New DocumentStream.File
        Call File.AppendLine(New String() {"AccessionId", "Common_name", "Description", "Pfam_domains", "Sequence"})
        For Each Id As String In List.Distinct
            Call File.AppendLine(ProteinDomains.FindAtColumn(KeyWord:=Id, Column:=0).First)
        Next
        Return File
    End Function
End Class
