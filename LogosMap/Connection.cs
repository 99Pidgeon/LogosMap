namespace LogosMap
{
    public class Connection(Node startNode, Node endNode) : IEquatable<Connection>
    {
        public Node startNode = startNode;
        public Node endNode = endNode;

        public bool Equals(Connection? other)
        {
            return other != null && other.startNode.Id == startNode.Id && other.endNode.Id == endNode.Id;
        }
    }
}
