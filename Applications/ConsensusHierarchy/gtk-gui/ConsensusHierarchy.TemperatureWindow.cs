
// This file has been generated by the GUI designer. Do not modify.
namespace ConsensusHierarchy
{
	public partial class TemperatureWindow
	{
		private global::Gtk.Fixed fixed1;
		private global::Gtk.HScale hscale1;
		
		protected virtual void Build ()
		{
			global::Stetic.Gui.Initialize (this);
			// Widget ConsensusHierarchy.TemperatureWindow
			this.Name = "ConsensusHierarchy.TemperatureWindow";
			this.Title = global::Mono.Unix.Catalog.GetString ("TemperatureWindow");
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			// Container child ConsensusHierarchy.TemperatureWindow.Gtk.Container+ContainerChild
			this.fixed1 = new global::Gtk.Fixed ();
			this.fixed1.Name = "fixed1";
			this.fixed1.HasWindow = false;
			// Container child fixed1.Gtk.Fixed+FixedChild
			this.hscale1 = new global::Gtk.HScale (null);
			this.hscale1.WidthRequest = 350;
			this.hscale1.CanFocus = true;
			this.hscale1.Name = "hscale1";
			this.hscale1.Adjustment.Lower = 0.0001D;
			this.hscale1.Adjustment.Upper = 2D;
			this.hscale1.Adjustment.PageIncrement = 10D;
			this.hscale1.Adjustment.StepIncrement = 0.5D;
			this.hscale1.Adjustment.Value = 0.006D;
			this.hscale1.DrawValue = true;
			this.hscale1.Digits = 5;
			this.hscale1.ValuePos = ((global::Gtk.PositionType)(2));
			this.fixed1.Add (this.hscale1);
			global::Gtk.Fixed.FixedChild w1 = ((global::Gtk.Fixed.FixedChild)(this.fixed1 [this.hscale1]));
			w1.X = 2;
			w1.Y = 4;
			this.Add (this.fixed1);
			if ((this.Child != null)) {
				this.Child.ShowAll ();
			}
			this.DefaultWidth = 365;
			this.DefaultHeight = 52;
			this.Show ();
		}
	}
}
