using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NETGen.Visualization;

namespace NETGen.GUI
{
    public sealed class LayoutOptions
    {
        private Dictionary<string, ILayoutProvider> layouts;

        internal delegate void ItemAddedDelegate(string name);
        internal event ItemAddedDelegate ItemAdded;

        public LayoutOptions()
        {
            layouts = new Dictionary<string, ILayoutProvider>();
        }

        public ILayoutProvider this[string name]
        {
            get
            {
                return layouts.ContainsKey(name) ? layouts[name] : null;
            }
            set
            {
                bool added = false;
                if (!layouts.ContainsKey(name))
                    added = true;
                layouts[name] = value;
                if (added && ItemAdded != null)
                    ItemAdded(name);
            }
        }

        public IEnumerable<string> LayoutNames
        {
            get
            {
                return layouts.Keys.ToArray();
            }
        }
    }
}
