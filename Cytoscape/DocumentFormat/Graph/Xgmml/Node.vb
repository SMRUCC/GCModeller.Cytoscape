Imports System.Drawing
Imports System.Xml.Serialization
Imports Microsoft.VisualBasic.ComponentModel
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Linq

Namespace DocumentFormat.CytoscapeGraphView.DocumentElements

    <XmlType("node")>
    Public Class Node : Inherits AttributeDictionary
        Implements IAddressHandle

        ''' <summary>
        ''' 当前的这个节点在整个网络的节点列表之中的位置
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        <XmlAttribute> Public Property id As Integer Implements IAddressHandle.Address
        <XmlAttribute> Public Property label As String
        <XmlElement("graphics")> Public Property Graphics As NodeGraphics

        Public ReadOnly Property Location As Point
            Get
                Return New Point(Graphics.x, Graphics.y)
            End Get
        End Property

        Public Overrides Function ToString() As String
            Dim array As String() = Attributes.ToArray(AddressOf Scripting.ToString)
            Return String.Format("{0} ""{1}""  ==> {2}", id, label, String.Join("; ", array))
        End Function

        Public Class NodeGraphics : Inherits AttributeDictionary
            <XmlAttribute("outline")> Public Property Outline As String
            <XmlAttribute> Public Property z As String
            <XmlAttribute("type")> Public Property Type As String
            <XmlAttribute("fill")> Public Property Fill As String
            <XmlAttribute> Public Property x As Double
            <XmlAttribute("width")> Public Property Width As Double
            <XmlAttribute> Public Property w As Double
            <XmlAttribute> Public Property h As Double
            <XmlAttribute> Public Property y As Double

            Public ReadOnly Property NODE_LABEL_FONT_SIZE As Integer
                Get
                    Dim attr = Me.Value("NODE_LABEL_FONT_SIZE")
                    If attr Is Nothing OrElse String.IsNullOrEmpty(attr.Value) Then
                        Return Math.Min(w, h)
                    Else
                        Return Val(attr.Value)
                    End If
                End Get
            End Property

            Public ReadOnly Property FillColor As Color
                Get
                    Dim Hex As String = Mid(Fill, 2)
                    Dim alpha = Me("NODE_TRANSPARENCY")
                    Dim r = CytoscapeColor.HexToARGB(Hex, If(alpha Is Nothing, 255, Val(alpha.Value)))
                    Return r
                End Get
            End Property

            Public Function GetLabelFont(Scale As Double) As Font
                Dim fName = Me("NODE_LABEL_FONT_FACE")
                Dim size As Integer = NODE_LABEL_FONT_SIZE * Scale

                If Not fName Is Nothing Then
                    Return New Font(fName.Value.Split("."c).FirstOrDefault, size)
                Else
                    Return New Font(FontFace.MicrosoftYaHei, size)
                End If
            End Function

            Public ReadOnly Property LabelColor As Color
                Get
                    Dim clattr = Me("NODE_LABEL_COLOR")
                    Dim clattrAlpha = Me("NODE_LABEL_TRANSPARENCY")

                    If clattr Is Nothing Then
                        Return Color.Black
                    End If

                    Return CytoscapeColor.HexToARGB(Mid(clattr.Value, 2), If(clattrAlpha Is Nothing, 255, Val(clattrAlpha.Value)))
                End Get
            End Property

        End Class

#Region "IDisposable Support"
        Private disposedValue As Boolean ' To detect redundant calls

        ' IDisposable
        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not Me.disposedValue Then
                If disposing Then
                    ' TODO: dispose managed state (managed objects).
                End If

                ' TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
                ' TODO: set large fields to null.
            End If
            Me.disposedValue = True
        End Sub

        ' TODO: override Finalize() only if Dispose( disposing As Boolean) above has code to free unmanaged resources.
        'Protected Overrides Sub Finalize()
        '    ' Do not change this code.  Put cleanup code in Dispose( disposing As Boolean) above.
        '    Dispose(False)
        '    MyBase.Finalize()
        'End Sub

        ' This code added by Visual Basic to correctly implement the disposable pattern.
        Public Sub Dispose() Implements IDisposable.Dispose
            ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub
#End Region
    End Class
End Namespace
