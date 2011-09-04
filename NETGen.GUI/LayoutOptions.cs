using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NETGen.Visualization;

namespace NETGen.GUI
{
	/// <summary>
	/// A class that ca be used to set network layouting options in the NETGen NetworkDisplay
	/// </summary>
    public sealed class LayoutOptions
    {
        private Dictionary<string, ILayoutProvider> layouts;

        internal delegate void ItemAddedDelegate(string name);
        internal event ItemAddedDelegate ItemAdded;

		/// <summary>
		/// Initializes a new instance of the <see cref="NETGen.GUI.LayoutOptions"/> class.
		/// </summary>
        public LayoutOptions()
        {
            layouts = new Dictionary<string, ILayoutProvider>();
        }
		
		/// <summary>
		/// Gets or sets layouting options that shall be available in the NetworkDisplay menu
		/// </summary>
		/// <param name='name'>
		/// The name of the layout options as it shall be displayed in the menu
		/// </param>
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
		
		/// <summary>
		/// Gets the names of the layout options
		/// </summary>
		/// <value>
		/// The layout names.
		/// </value>
        public IEnumerable<string> LayoutNames
        {
            get
            {
                return layouts.Keys.ToArray();
            }
        }
    }
}
