using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogosMap.actions
{
    public class AddAction(Node node) : IAction
    {
        public Node Node { get; set; } = node;

        public void Redo()
        {
            MainWindow.Instance?.AddNode(Node);
        }

        public void Undo()
        {
            MainWindow.Instance?.DeleteNode(Node);
        }
    }
}
