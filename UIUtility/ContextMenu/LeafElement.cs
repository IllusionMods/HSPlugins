using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UILib.ContextMenu
{
    public class LeafElement : AContextMenuElement
    {
        public object parameter;
        public Action<object> onClick;
    }
}
