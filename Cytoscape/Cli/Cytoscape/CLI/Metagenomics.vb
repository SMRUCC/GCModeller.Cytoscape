﻿Imports LANS.SystemsBiology.InteractionModel.Network.BLAST.BBHAPI
Imports LANS.SystemsBiology.NCBI.Extensions
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.DocumentFormat.Csv
Imports Microsoft.VisualBasic.Linq.Extensions
Imports Microsoft.VisualBasic

Partial Module CLI

    <ExportAPI("/bbh.Trim.Indeitites",
               Usage:="/bbh.Trim.Indeitites /in <bbh.csv> [/identities <0.3> /out <out.csv>]")>
    Public Function BBHTrimIdentities(args As CommandLine.CommandLine) As Integer
        Dim inFile As String = args("/in")
        Dim identities As Double = args.GetValue("/identities", 0.3)
        Dim out As String = args.GetValue("/out", inFile.TrimFileExt & $".identities.{identities}.csv")

        Using IO As New DocumentStream.Linq.WriteStream(Of BBH)(out)
            Dim reader As New DocumentStream.Linq.DataStream(inFile)
            Call reader.ForEachBlock(Of BBH)(Sub(data0)
                                                 data0 = (From x In data0.AsParallel Where x.Identities >= identities Select x).ToArray
                                                 Call IO.Flush(data0)
                                             End Sub)
            Return 0
        End Using
    End Function

    <ExportAPI("/BBH.Simple", Usage:="/BBH.Simple /in <sbh.csv> [/evalue <evalue: 1e-5> /out <out.bbh.csv>]")>
    Public Function SimpleBBH(args As CommandLine.CommandLine) As Integer
        Dim inFile As String = args("/in")
        Dim out As String = args.GetValue("/out ", inFile.TrimFileExt & ".bbh.simple.Csv")
        Dim evalue As Double = args.GetValue("/evalue", 0.00001)
        Dim lstSBH As New List(Of LocalBLAST.Application.BBH.BestHit)

        Using read As New DocumentStream.Linq.DataStream(inFile)
            Call read.ForEachBlock(Of LocalBLAST.Application.BBH.BestHit)(
                invoke:=Sub(block As LocalBLAST.Application.BBH.BestHit()) Call lstSBH.AddRange((From x In block.AsParallel Where x.evalue <= evalue Select x).ToArray),
                blockSize:=51200 * 2)
        End Using

        Dim simpleBBHArray = BBHHits(lstSBH)
        Using IO As New DocumentStream.Linq.WriteStream(Of BBH)(out)
            Dim buffer = simpleBBHArray.Split(102400)

            For Each block In buffer
                Call IO.Flush(block)
            Next

            Return 0
        End Using
    End Function

    <ExportAPI("/BLAST.Network", Usage:="/BLAST.Network /in <inFile> [/out <outDIR> /type <default:blast_out; values: blast_out, sbh, bbh> /dict <dict.xml>]")>
    Public Function GenerateBlastNetwork(args As CommandLine.CommandLine) As Integer
        Dim inFile As String = args("/in")
        Dim out As String = args.GetValue("/out", inFile.TrimFileExt)
        Dim type As String = args.GetValue("/type", "blast_out").ToLower
        Dim method As LANS.SystemsBiology.InteractionModel.Network.BLAST.BuildFromSource
        If LANS.SystemsBiology.InteractionModel.Network.BLAST.BuildMethods.ContainsKey(type) Then
            method = LANS.SystemsBiology.InteractionModel.Network.BLAST.BuildMethods(type)
        Else
            method = AddressOf LANS.SystemsBiology.InteractionModel.Network.BLAST.BuildFromBlastOUT
        End If

        Dim dict As String = args("/dict")
        Dim locusDict As Dictionary(Of String, String) = __loadDict(dict)
        Dim network = method(source:=inFile, locusDict:=locusDict)
        Return network.Save(out, Encodings.UTF8).CLICode
    End Function

    Private Function __loadDict(xml As String) As Dictionary(Of String, String)
        If Not xml.FileExists Then Return New Dictionary(Of String, String)

        Dim locusList As LANS.SystemsBiology.InteractionModel.Network.BLAST.LDM.LocusDict() =
            xml.LoadXml(Of LANS.SystemsBiology.InteractionModel.Network.BLAST.LDM.LocusDict())

        If locusList Is Nothing Then Return New Dictionary(Of String, String)

        Return LANS.SystemsBiology.InteractionModel.Network.BLAST.LDM.LocusDict.CreateDictionary(locusList)
    End Function

    <ExportAPI("/BLAST.Network.MetaBuild", Usage:="/BLAST.Network.MetaBuild /in <inDIR> [/out <outDIR> /dict <dict.xml>]")>
    Public Function MetaBuildBLAST(args As CommandLine.CommandLine) As Integer
        Dim inDIR As String = args("/in")
        Dim out As String = args.GetValue("/out", inDIR & ".MetaBuild")
        Dim dict As String = args("/dict")
        Dim locusDict As Dictionary(Of String, String) = __loadDict(dict)
        Dim network = LANS.SystemsBiology.InteractionModel.Network.BLAST.MetaBuildFromBBH(inDIR, locusDict)
        Return network.Save(out, Encodings.UTF8).CLICode
    End Function

    <ExportAPI("/MAT2NET", Usage:="/MAT2NET /in <mat.csv> [/out <net.csv> /cutoff 0]")>
    Public Function MatrixToNetwork(args As CommandLine.CommandLine) As Integer
        Dim inFile As String = args("/in")
        Dim out As String = args.GetValue("/out", inFile.TrimFileExt & ".network.Csv")
        Dim Csv = DocumentFormat.Csv.DocumentStream.File.Load(Path:=inFile)
        Dim ids As String() = Csv.First.Skip(1).ToArray
        Dim net As New List(Of DataVisualization.Network.FileStream.NetworkEdge)
        Dim cutoff As Double = args.GetDouble("/cutoff")

        For Each row As DocumentStream.RowObject In Csv.Skip(1)
            Dim from As String = row.First
            Dim values As Double() = row.Skip(1).ToArray(Function(x) Val(x))

            For i As Integer = 0 To ids.Length - 1
                Dim n As Double = values(i)

                If n <> 0R AndAlso Not Double.IsNaN(n) AndAlso n <= cutoff Then
                    Dim edge As New DataVisualization.Network.FileStream.NetworkEdge With {
                        .FromNode = from,
                        .Confidence = n,
                        .ToNode = ids(i)
                    }
                    Call net.Add(edge)
                End If
            Next
        Next

        Return net.SaveTo(out).CLICode
    End Function
End Module
