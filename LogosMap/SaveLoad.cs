using System.IO;
using System.Text.Json;

namespace LogosMap
{
    class SaveLoad
    {
        public static NodeData ConvertToData(Node node)
        {
            List<ConnectionData> connections = [];

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
                X = node.x,
                Y = node.y,
                Name = node.name,
                Connections = connections,
                StartConnections = startConnections
            };
        }

        public static Node ConvertFromData(NodeData data)
        {
            return new Node
            {
                Id = data.Id,
                x = data.X,
                y = data.Y,
                name = data.Name,
            };
        }

        public static ConnectionData ConvertConnectionToData(Connection connection)
        {
            return new ConnectionData
            {
                StartNodeId = connection.startNode.Id,
                EndNodeId = connection.endNode.Id
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
                foreach (var connection in dataList[i].Connections)
                {
                    MainWindow.nodes[i].connections.Add(new Connection(MainWindow.nodeIds[connection.StartNodeId], MainWindow.nodeIds[connection.EndNodeId]));
                }
            }

            for (int i = 0; i < dataList.Count; i++)
            {
                foreach (var connection in dataList[i].StartConnections)
                {
                    MainWindow.nodes[i].startConnections.Add(new Connection(MainWindow.nodeIds[connection.StartNodeId], MainWindow.nodeIds[connection.EndNodeId]));
                }
            }

            MainWindow.lastId++;

            if (MainWindow.Instance == null)
            {
                return;
            }
        }
    }
}
