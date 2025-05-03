namespace LogosMap
{
    class NodeData
    {
        public int Id { get; set; }
        public required string name { get; set; }
        public required List<ConnectionData> connections { get; set; }
        public required List<ConnectionData> startConnections { get; set; }
        public float x { get; set; }
        public float y { get; set; }
    }

    class ConnectionData
    {
        public int startNodeId { get; set; }
        public int endNodeId { get; set; }
    }
}
