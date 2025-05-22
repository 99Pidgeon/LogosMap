using LogosMap.resources.lang;
using SkiaSharp;

namespace LogosMap
{
    public class Node : IEquatable<Node>
    {
        public int Id { get; set; }
        public string name = Strings.Node;
        public List<Connection> connections = [];
        public List<Connection> startConnections = [];
        public SKPoint fromPos;
        public float x;
        public float y;

        public Node(){}

        public Node(int Id, float x, float y)
        {
            this.Id = Id;
            this.x = x;
            this.y = y;
        }

        public void Move(float targetX, float targetY)
        {
            x = targetX;
            y = targetY;
        }

        public bool Equals(Node? other)
        {
            return other != null && Id == other.Id;
        }

        public List<Node> GetChildren()
        {
            List<Node> output = [];

            foreach (Connection connection in startConnections)
            {
                output.Add(connection.endNode);
            }

            return output;
        }
    }
}
