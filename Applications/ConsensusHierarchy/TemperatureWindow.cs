using System;

namespace ConsensusHierarchy
{	
	
	public partial class TemperatureWindow : Gtk.Window
	{
		public double Temperature { get
			{
				return hscale1.Value;
			}
		}
		
		public TemperatureWindow () : 
				base(Gtk.WindowType.Toplevel)
		{
			this.Build ();
		}
	}
}

