using System.IO;
using System.Text.Json;

namespace LogosMap
{
    class SaveLoad
    {
        public static NodeData ConvertToData(Node node)
        {
            List<ConnectionData> connections = new List<ConnectionData>();

            for (int i = 0; i < node.connections.Count; i++)
            {
                connections.Add(ConvertConnectionToData(node.connections[i]));
            }

            List<ConnectionData> startConnections = new List<ConnectionData>();

            for (int i = 0; i < node.startConnections.Count; i++)
            {
                startConnections.Add(ConvertConnectionToData(node.startConnections[i]));
            }

            return new NodeData
            {
                Id = node.Id,
                x = node.x,
                y = node.y,
                name = node.name,
                connections = connections,
                startConnections = startConnections
            };
        }

        public static Node ConvertFromData(NodeData data)
        {
            return new Node
            {
                Id = data.Id,
                x = data.x,
                y = data.y,
                name = data.name,
            };
        }

        public static ConnectionData ConvertConnectionToData(Connection connection)
        {
            return new ConnectionData
            {
                startNodeId = connection.startNode.Id,
                endNodeId = connection.endNode.Id
            };
        }

        public static void SaveMindMap(string path)
        {
            var dataList = MainWindow.nodes.Select(c => ConvertToData(c)).ToList();
            var json = JsonSerializer.Serialize(dataList, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        public static void LoadMindMap(string path)
        {
            if (!File.Exists(path)) return;

            var json = File.ReadAllText(path);
            var dataList = JsonSerializer.Deserialize<List<NodeData>>(json);

            if(dataList == null)
            {
                return;
            }

            MainWindow.nodes.Clear();
            MainWindow.nodeIds.Clear();
            foreach (var data in dataList)
            {
                if (MainWindow.lastId < data.Id)
                { 
                    MainWindow.lastId = data.Id;
                }

                Node nodeToAdd = ConvertFromData(data);

                MainWindow.nodes.Add(nodeToAdd);
                MainWindow.nodeIds.Add(data.Id, nodeToAdd);
            }

            for (int i = 0; i < dataList.Count; i++)
            {
                foreach (var connection in dataList[i].connections)
                {
                    MainWindow.nodes[i].connections.Add(new Connection(MainWindow.nodeIds[connection.startNodeId], MainWindow.nodeIds[connection.endNodeId]));
                }
            }

            for (int i = 0; i < dataList.Count; i++)
            {
                foreach (var connection in dataList[i].startConnections)
                {
                    MainWindow.nodes[i].startConnections.Add(new Connection(MainWindow.nodeIds[connection.startNodeId], MainWindow.nodeIds[connection.endNodeId]));
                }
            }

            MainWindow.lastId++;

            if (MainWindow.Instance == null)
            {
                return;
            }

            MainWindow.Instance.skCanvas.InvalidateVisual(); // 다시 그리기
        }
    }
}
