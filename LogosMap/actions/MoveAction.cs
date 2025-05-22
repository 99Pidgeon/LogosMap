using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogosMap.actions
{
    public class MoveAction(Node node, SKPoint from, SKPoint to) : IAction
    {
        public Node Node { get; set; } = node;
        public SKPoint From { get; set; } = from;
        public SKPoint To { get; set; } = to;

        public void Redo()
        {
            Node.Move(To.X, To.Y);
        }

        public void Undo()
        {
            Node.Move(From.X, From.Y);
        }
    }
}
