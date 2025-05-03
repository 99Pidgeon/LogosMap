namespace LogosMap
{
    public class Node
    {
        public int Id { get; set; }
        public string name = "노드";
        public List<Connection> connections = [];
        public List<Connection> startConnections = [];
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
    }
}
