namespace LogosMap
{
    class NodeData
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required List<ConnectionData> Connections { get; set; }
        public required List<ConnectionData> StartConnections { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
    }

    class ConnectionData
    {
        public int StartNodeId { get; set; }
        public int EndNodeId { get; set; }
    }
}
