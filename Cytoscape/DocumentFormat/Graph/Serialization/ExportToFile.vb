Imports LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Cytoscape.DocumentFormat.CytoscapeGraphView.DocumentElements.Attribute
Imports Microsoft.VisualBasic.DocumentFormat.Csv.StorageProvider.Reflection.Reflector
Imports Microsoft.VisualBasic.DataVisualization.Network.FileStream.Node
Imports Microsoft.VisualBasic.DataVisualization.Network.FileStream.NetworkEdge
Imports Microsoft.VisualBasic.DataVisualization.Network.LDM.Abstract
Imports Microsoft.VisualBasic.DocumentFormat.Csv.StorageProvider.ComponentModels
Imports Microsoft.VisualBasic.DataVisualization.Network
Imports Microsoft.VisualBasic

Namespace DocumentFormat.CytoscapeGraphView.Serialization

    ''' <summary>
    ''' 将网络模型的数据导出至Cytoscape的网络模型文件之中
    ''' </summary>
    ''' <remarks></remarks>
    Public Module ExportToFile

        Public Function Export(Of Edge As INetworkEdge)(NodeList As IEnumerable(Of FileStream.Node), Edges As IEnumerable(Of Edge), Optional Title As String = "NULL") As Graph
            Return Export(Of FileStream.Node, Edge)(NodeList.ToArray, Edges.ToArray, Title)
        End Function

        ''' <summary>
        ''' 对于所有的属性值，Cytoscape之中的数据类型会根据属性值的类型自动映射
        ''' </summary>
        ''' <typeparam name="Node"></typeparam>
        ''' <typeparam name="Edge"></typeparam>
        ''' <param name="NodeList"></param>
        ''' <param name="Edges"></param>
        ''' <param name="Title"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function Export(Of Node As INode, Edge As INetworkEdge)(NodeList As Node(), Edges As Edge(), Optional Title As String = "NULL") As Graph
            Dim Model As Graph = New Graph With {
                    .Label = "0",
                    .ID = "1",
                    .Directed = "1"
            }
            Dim ModelAttributes = New DocumentElements.Attribute() {
                New DocumentElements.Attribute With {
                    .Name = ATTR_SHARED_NAME,
                    .Value = Title,
                    .Type = ATTR_VALUE_TYPE_STRING
                },
                New DocumentElements.Attribute With {
                    .Name = ATTR_NAME,
                    .Value = Title,
                    .Type = ATTR_VALUE_TYPE_STRING
                }
            }
            Dim EdgeSchema = SchemaProvider.CreateObject(GetType(Edge), False)
            Dim interMaps = __mapInterface(EdgeSchema)

            Model.Nodes = __exportNodes(NodeList, GetType(Node).GetDataFrameworkTypeSchema(False))
            Model.Edges = __exportEdges(Of Edge)(Edges,
                                                 Nodes:=Model.Nodes.ToDictionary(Function(item) item.label),
                                                 EdgeTypeMapping:=GetType(Edge).GetDataFrameworkTypeSchema(False),
                                                 Schema:=interMaps)
            Model.Attributes = ModelAttributes
            Model.NetworkMetaData = New DocumentElements.NetworkMetadata With {
                .Title = "GCModeller Exports: " & Title,
                .Description = "http://code.google.com/p/genome-in-code/cytoscape"
            }

            Return Model
        End Function

        Public Function Export(Of Node As FileStream.Node, Edge As FileStream.NetworkEdge)(Network As FileStream.Network(Of Node, Edge), Optional Title As String = "NULL") As Graph
            Return Export(Network.Nodes, Network.Edges, Title)
        End Function

        ''' <summary>
        ''' 属性类型可以进行用户的自定义映射
        ''' </summary>
        ''' <typeparam name="Node"></typeparam>
        ''' <typeparam name="Edge"></typeparam>
        ''' <param name="NodeList"></param>
        ''' <param name="Edges"></param>
        ''' <param name="NodeTypeMapping"></param>
        ''' <param name="EdgeTypeMapping"></param>
        ''' <param name="Title"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function Export(Of Node As FileStream.Node,
                                  Edge As FileStream.NetworkEdge)(
                                  NodeList As Node(),
                                  Edges As Edge(),
                                  NodeTypeMapping As Dictionary(Of String, Type),
                                  EdgeTypeMapping As Dictionary(Of String, Type),
                                  Optional Title As String = "NULL") As Graph
            Dim Model As Graph = New Graph With {
                .Label = "0",
                .ID = "1",
                .Directed = "1"
            }
            Dim ModelAttributes = New DocumentElements.Attribute() {
                New DocumentElements.Attribute With {
                    .Name = ATTR_SHARED_NAME,
                    .Value = Title,
                    .Type = ATTR_VALUE_TYPE_STRING
                },
                New DocumentElements.Attribute With {
                    .Name = ATTR_SHARED_NAME,
                    .Value = Title,
                    .Type = ATTR_VALUE_TYPE_STRING
                }
            }
            Dim EdgeSchema = SchemaProvider.CreateObject(GetType(Edge), False)
            Dim interMaps = __mapInterface(EdgeSchema)

            Model.Nodes = __exportNodes(NodeList, NodeTypeMapping)
            Model.Edges = __exportEdges(Edges, Model.Nodes.ToDictionary(Function(item) item.label), EdgeTypeMapping, interMaps)
            Model.Attributes = ModelAttributes

            Return Model
        End Function

        Const propGET As String = "get_"

        Private Function __mapInterface(schema As SchemaProvider) As Dictionary(Of String, String)
            Dim mapEdge = schema.DeclaringType.GetInterfaceMap(GetType(INetworkEdge))
            Dim mapNodes = schema.DeclaringType.GetInterfaceMap(GetType(I_InteractionModel))
            Dim maps As New Dictionary(Of String, String)

            Dim edgeMaps = (From i As Integer In mapEdge.TargetMethods.Sequence
                            Let [interface] = mapEdge.InterfaceMethods(i)
                            Where InStr([interface].Name, propGET) = 1
                            Select [interface],
                                mMethod = mapEdge.TargetMethods(i)).ToDictionary(Function(x) x.interface.Name.Replace(propGET, ""))
            Dim nodeMaps = (From i As Integer In mapNodes.TargetMethods.Sequence
                            Let [interface] = mapNodes.InterfaceMethods(i)
                            Where InStr([interface].Name, propGET) = 1
                            Select [interface],
                                mMethod = mapNodes.TargetMethods(i)).ToDictionary(Function(x) x.interface.Name.Replace(propGET, ""))

            Dim map = nodeMaps(NameOf(I_InteractionModel.locusId)) : Call maps.Add(REFLECTION_ID_MAPPING_FROM_NODE, __getMap(map.interface, map.mMethod, schema))
            map = nodeMaps(NameOf(I_InteractionModel.Address)) : Call maps.Add(REFLECTION_ID_MAPPING_TO_NODE, __getMap(map.interface, map.mMethod, schema))
            map = edgeMaps(NameOf(INetworkEdge.Confidence)) : Call maps.Add(REFLECTION_ID_MAPPING_CONFIDENCE, __getMap(map.interface, map.mMethod, schema))
            map = edgeMaps(NameOf(INetworkEdge.InteractionType)) : Call maps.Add(REFLECTION_ID_MAPPING_INTERACTION_TYPE, __getMap(map.interface, map.mMethod, schema))

            Return maps
        End Function

        Private Function __getMap([interface] As Reflection.MethodInfo, mMethod As Reflection.MethodInfo, schema As SchemaProvider) As String
            Dim mapName As String = mMethod.Name.Replace(propGET, "")
            Dim mapFiled = schema.GetField(mapName)

            If mapFiled Is Nothing Then
                Return mapName
            Else
                mapName = mapFiled.Name
                Return mapName
            End If
        End Function

        ''' <summary>
        '''
        ''' </summary>
        ''' <returns>输入属性名，然后返回属性的值类型的映射</returns>
        ''' <remarks></remarks>
        Private Function __createTypeMapping(typeMapping As Dictionary(Of String, Type)) As Func(Of String, String)
            If typeMapping.IsNullOrEmpty Then
                Return Function(NULL As String) DocumentElements.Attribute.ATTR_VALUE_TYPE_STRING
            End If

            Dim CytoscapeMapping As Dictionary(Of Type, String) = DocumentElements.Attribute.TypeMapping
            Dim Mapping As Func(Of String, String) = Function(attrKey) __mapping(attrKey, typeMapping, CytoscapeMapping)
            Return Mapping
        End Function

        Private Function __mapping(attrKey As String,
                                   typeMapping As Dictionary(Of String, Type),
                                   cytoscapeMapping As Dictionary(Of Type, String)) As String
            Dim Type As Type = typeMapping.TryGetValue(attrKey)
            If Not Type Is Nothing AndAlso cytoscapeMapping.ContainsKey(Type) Then
                Return cytoscapeMapping(Type)
            Else
                Return DocumentElements.Attribute.ATTR_VALUE_TYPE_STRING
            End If
        End Function

        Private Function __exportNodes(Of Node As INode)(Nodes As Node(), nodeTypeMapping As Dictionary(Of String, Type)) As DocumentElements.Node()
            Dim ChunkBuffer = Nodes.ExportAsPropertyAttributes(False)
            Dim typeMapping = __createTypeMapping(nodeTypeMapping)
            Dim LQuery = (From item In ChunkBuffer.AsParallel
                          Let node_obj = __exportNode(item, __getType:=typeMapping)
                          Select node_obj
                          Group node_obj By node_obj.label Into Group
                          Order By label Ascending).ToArray
            Return (From x In LQuery Select x.Group.First).ToArray.AddHandle.ToArray  '生成节点数据并去除重复
        End Function

        Private Function __exportNode(dict As Dictionary(Of String, String), __getType As Func(Of String, String)) As DocumentElements.Node
            Dim ID As String = dict(REFLECTION_ID_MAPPING_IDENTIFIER)
            Dim attrs As List(Of DocumentElements.Attribute) = New List(Of DocumentElements.Attribute)

            attrs += New DocumentElements.Attribute With {
                .Name = ATTR_SHARED_NAME,
                .Value = ID,
                .Type = ATTR_VALUE_TYPE_STRING
            }
            attrs += New DocumentElements.Attribute With {
                .Name = ATTR_NAME,
                .Value = ID,
                .Type = ATTR_VALUE_TYPE_STRING
            }
            Call dict.Remove(REFLECTION_ID_MAPPING_IDENTIFIER)

            attrs += (From item As KeyValuePair(Of String, String)
                      In dict
                      Select New DocumentElements.Attribute With {
                          .Name = item.Key,
                          .Value = item.Value,
                          .Type = __getType(item.Key)}).ToArray

            Dim Node As DocumentElements.Node =
                New DocumentElements.Node With {
                    .label = ID,
                    .Attributes = attrs.ToArray
            }

            Return Node
        End Function

        Private Function __exportEdges(Of Edge As INetworkEdge)(
                                          Edges As Edge(),
                                          Nodes As Dictionary(Of String, DocumentElements.Node),
                                          EdgeTypeMapping As Dictionary(Of String, Type),
                                          Schema As Dictionary(Of String, String)) As DocumentElements.Edge()

            Dim ChunkBuffer = __mapNodes(Edges.ExportAsPropertyAttributes(False), Schema)
            Dim TypeMapping = __createTypeMapping(EdgeTypeMapping)
            Dim LQuery = (From item As Dictionary(Of String, String)
                          In ChunkBuffer
                          Select __exportEdge(item, Nodes, TypeMapping)).ToArray.AddHandle(offset:=Nodes.Count)
            Return LQuery
        End Function

        Private Function __mapNodes(ByRef buffer As List(Of Dictionary(Of String, String)), Schema As Dictionary(Of String, String)) As List(Of Dictionary(Of String, String))
            For Each dict In buffer
                For Each map In Schema
                    If Not dict.ContainsKey(map.Key) Then
                        If dict.ContainsKey(map.Value) Then
                            Dim value = dict(map.Value)
                            Call dict.Add(map.Key, value)
                        End If
                    End If
                Next
            Next

            Return buffer
        End Function

        Private Function __exportEdge(dict As Dictionary(Of String, String), Nodes As Dictionary(Of String, DocumentElements.Node), __getType As Func(Of String, String)) As DocumentElements.Edge
            Dim nodeName As String = dict(REFLECTION_ID_MAPPING_FROM_NODE)
            Dim fromNode As DocumentElements.Node = Nodes.TryGetValue(nodeName)

            If fromNode Is Nothing Then
                Call $"fromNode '{nodeName}' could not be found in the node list!".__DEBUG_ECHO
                fromNode = New DocumentElements.Node With {
                    .label = nodeName,
                    .id = Nodes.Count
                }
                Call Nodes.Add(nodeName, fromNode)
                Call $"INSERT this absence node into network...".__DEBUG_ECHO
            Else
                nodeName = dict(REFLECTION_ID_MAPPING_TO_NODE)
            End If

            Dim toNode As DocumentElements.Node = Nodes.TryGetValue(nodeName)
            If toNode Is Nothing Then
                Call $"toNode '{nodeName}' could not be found in the node list!".__DEBUG_ECHO
                toNode = New DocumentElements.Node With {
                    .label = nodeName,
                    .id = Nodes.Count
                }
                Call Nodes.Add(nodeName, toNode)
                Call $"INSERT this absence node into network...".__DEBUG_ECHO
            End If

            Dim InteractionType As String = dict.TryGetValue(REFLECTION_ID_MAPPING_INTERACTION_TYPE)
            InteractionType = If(String.IsNullOrEmpty(InteractionType), "interact", InteractionType)

            Dim Node As DocumentElements.Edge =
                New DocumentElements.Edge With {
                    .Label = String.Format("{0} ({1}) {2}", fromNode.label, InteractionType, toNode.label),
                    .source = fromNode.id,
                    .target = toNode.id
            }
            Dim attrs As List(Of DocumentElements.Attribute) =
                New List(Of DocumentElements.Attribute)
            attrs += New DocumentElements.Attribute With {
                .Name = ATTR_SHARED_NAME,
                .Value = Node.Label,
                .Type = ATTR_VALUE_TYPE_STRING
            }
            attrs += New DocumentElements.Attribute With {
                .Name = ATTR_NAME,
                .Value = Node.Label,
                .Type = ATTR_VALUE_TYPE_STRING
            }
            attrs += New DocumentElements.Attribute With {
                .Name = ATTR_SHARED_INTERACTION,
                .Value = InteractionType,
                .Type = ATTR_VALUE_TYPE_STRING
            }
            Call dict.Remove(REFLECTION_ID_MAPPING_FROM_NODE)
            Call dict.Remove(REFLECTION_ID_MAPPING_TO_NODE)
            Call dict.Remove(REFLECTION_ID_MAPPING_INTERACTION_TYPE)
            Call attrs.AddRange((From item As KeyValuePair(Of String, String) In dict
                                 Select New DocumentElements.Attribute With {
                                     .Name = item.Key,
                                     .Value = item.Value,
                                     .Type = __getType(item.Key)}).ToArray)
            Node.Attributes = attrs.ToArray
            Return Node
        End Function
    End Module
End Namespace