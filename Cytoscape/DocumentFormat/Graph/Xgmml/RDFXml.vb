﻿Imports System.Text
Imports System.Text.RegularExpressions
Imports Microsoft.VisualBasic.Text.Xml

Namespace DocumentFormat.CytoscapeGraphView

    Module RDFXml

        Public Function TrimRDF(xml As String) As String
            Dim sb As New StringBuilder(xml)

            Call sb.Replace(" cy:", " cy-")
            Call sb.Replace("rdf:", "rdf-")
            Call sb.Replace("<dc:", "<dc-")
            Call sb.Replace("/dc:", "/dc-")

            xml = sb.ToString

            Return xml
        End Function

        Const XGMML As String = "http://www.cs.rpi.edu/XGMML"
        Const dc As String = "http://purl.org/dc/elements/1.1/"
        Const xlink As String = "http://www.w3.org/1999/xlink"
        Const rdf As String = "http://www.w3.org/1999/02/22-rdf-syntax-ns#"
        Const cy As String = "http://www.cytoscape.org"

        Public Function WriteXml(xml As String, encoding As Encoding, path As String) As Boolean
            Dim doc As New XmlDoc(xml)

            doc.encoding = XmlEncodings.UTF8
            doc.standalone = True
            doc.version = "1.0"
            doc.xmlns.xmlns = XGMML
            doc.xmlns.xsd = ""
            doc.xmlns.xsi = ""
            doc.xmlns.Set(NameOf(dc), dc)
            doc.xmlns.Set(NameOf(xlink), xlink)
            doc.xmlns.Set(NameOf(rdf), rdf)
            doc.xmlns.Set(NameOf(cy), cy)

            Dim sb As New StringBuilder(doc.ToString)

            Call sb.Replace(" cy-", " cy:")
            Call sb.Replace("rdf-", "rdf:")
            Call sb.Replace("<dc-", "<dc:")
            Call sb.Replace("/dc-", "/dc:")

            Return sb.SaveTo(path, encoding)
        End Function
    End Module
End Namespace