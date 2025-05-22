using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogosMap.actions
{
    public class DeleteAction(Node node) : IAction
    {
        public Node Node { get; set; } = node;

        public void Redo()
        {
            MainWindow.Instance?.DeleteNode(Node);
        }

        public void Undo()
        {
            MainWindow.Instance?.AddNode(Node);
        }
    }
}
