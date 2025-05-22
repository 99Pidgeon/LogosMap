using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogosMap.actions
{
    public interface IAction
    {
        public void Undo();
        public void Redo();
    }
}
