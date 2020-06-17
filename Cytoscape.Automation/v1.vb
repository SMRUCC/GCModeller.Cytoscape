﻿Imports System.Net
Imports System.Threading
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.MIME.application.json
Imports Microsoft.VisualBasic.Serialization.JSON
Imports SMRUCC.genomics.Visualize.Cytoscape.CytoscapeGraphView.Cyjs
Imports SMRUCC.genomics.Visualize.Cytoscape.Tables

Public Class v1 : Inherits cyREST

    ReadOnly api$

    Sub New(port As Integer, Optional host$ = "localhost")
        Call MyBase.New

        api = $"http://{host}:{port}/v1"

        Call MyBase.virtualFilesystem.DriverRun
        Call Thread.Sleep(500)
    End Sub

    ''' <summary>
    ''' GET list of layout algorithms
    ''' </summary>
    ''' <returns></returns>
    Public Overrides Function layouts() As String()
        Dim url As String = $"{api}/apply/layouts"
        Dim json As String = url.GET

        Return json.LoadJSON(Of String())
    End Function

    Public Overrides Function networksNames() As String()
        Throw New NotImplementedException()
    End Function

    Public Overrides Function putNetwork(network As [Variant](Of Cyjs, SIF()), Optional collection As String = Nothing, Optional title As String = Nothing) As NetworkReference()
        Dim format As formats = If(network Like GetType(Cyjs), formats.json, formats.egdeList)
        Dim query As New Dictionary(Of String, String) From {
            {"collection", collection},
            {"title", title},
            {"source", "url"},
            {"format", format.ToString}
        }
        Dim url As String = $"{api}/networks?{query.BuildUrlData(escaping:=True, stripNull:=True)}"
        Dim text As String = If(format = formats.json, JSONSerializer.GetJson(New Upload.CyjsUpload(network.VA)), SIF.ToText(network.VB))
        Dim file As New FileReference With {
            .ndex_uuid = 12345,
            .source_location = virtualFilesystem.addUploadData(text, If(format = formats.json, "json", "txt")),
            .source_method = "GET"
        }
        Dim refJson As String = JSONSerializer.GetJson({file})

        Using request As New WebClient
            request.Headers.Add("Content-Type", "application/json")
            text = request.UploadString(url, "POST", refJson)
        End Using

        Return text.LoadJSON(Of NetworkReference())
    End Function

    Public Overrides Function applyLayout(networkId As Integer, Optional algorithmName As String = "force-directed") As String
        Dim url = $"{api}/v1/apply/layouts/{algorithmName}/{networkId}"
        Return url.GET
    End Function
End Class
