using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogosMap.actions
{
    public class Actions : List<IAction>
    {
        public void Undo()
        {
            foreach (IAction action in this)
            {
                action.Undo();
            }
        }

        public void Redo()
        {
            foreach (IAction action in this)
            {
                action.Redo();
            }
        }
    }
}
