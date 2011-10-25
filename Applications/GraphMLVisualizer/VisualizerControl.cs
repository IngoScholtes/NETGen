using System;
using System.Data.Linq;
using NETGen.Core;
using NETGen.Visualization;
using NETGen.Layouts.FruchtermanReingold;
using NETGen.Layouts.RandomLayout;
using Gtk;

namespace NETGenVisualizer
{
	
	/// <summary>
	/// Provides a graphical user interface to functions of the NETGen framework
	/// </summary> 
	public partial class VisualizerControl : Gtk.Window
	{
		Network n = null;
		NetworkColorizer colorizer;
		NETGen.Dynamics.CEF.CEFPlayer player;
		
		System.Drawing.Color ConvertGTKColor(Gdk.Color c)
		{
			return System.Drawing.Color.FromArgb(
				(byte) (c.Red>>8),
				(byte) (c.Green>>8),
				(byte) (c.Blue>>8));
		}
		
		public VisualizerControl () : 
				base(Gtk.WindowType.Toplevel)
		{
			this.Build ();
			
			colorizer = new	NetworkColorizer();
			this.fileChooserNetworkFile.FileSet += HandleFileChooserNetworkFilehandleFileSet;
			this.btnComputeLayout.Clicked+= HandleBtnComputeLayouthandleClicked;
			this.btnChoseDefaultNodeColor.ColorSet += HandleBtnChoseDefaultNodeColorhandleColorSet;
			this.btnChoseEdgeColor.ColorSet += HandleBtnChoseEdgeColorhandleColorSet;
			this.btnSearch.Clicked += HandleBtnSearchhandleClicked;
			this.fileChooserExportPDF.FileSet+= HandleFileChooserExportPDFhandleFileSet;
			this.fileChooserExportBitmap.FileSet += HandleFileChooserExportBitmaphandleFileSet;
			this.fileChooserHighlightNodes.FileSet += HandleFileChooserHighlightNodeshandleFileSet;
			this.btnApplyNodeSize.Clicked += HandleBtnApplyNodeSizehandleClicked;
			this.btnApplyEdgeWidth.Clicked += HandleBtnApplyEdgeWidthhandleClicked;			
			this.fileChooserCEFFile.FileSet += HandleFileChooserCEFFilehandleFileSet;
			this.btnRunCEF.Clicked += HandlebtnRunCEFhandleClicked;
		}

		void HandlebtnRunCEFhandleClicked (object sender, EventArgs e)
		{
			player.Run();
		}

		void HandleFileChooserCEFFilehandleFileSet (object sender, EventArgs e)
		{
			// Load network evolution from cuttlefish cef file
			player = new NETGen.Dynamics.CEF.CEFPlayer(this.fileChooserCEFFile.Filename, n, colorizer);
			
			// On each evolution step, recompute layout and save current image
			player.OnStep+= new DiscreteDynamics.StepHandler( delegate(long time) {
				NetworkVisualizer.Layout.DoLayout();
				NetworkVisualizer.SaveCurrentImage(string.Format("frame_{0000}.bmp", time));
				Logger.AddMessage(LogEntryType.AppMsg, string.Format("Time {0000}: {1} Nodes, {2} Edges", time, n.VertexCount, n.EdgeCount));
			});
		}

		void HandleBtnComputeLayouthandleClicked (object sender, EventArgs e)
		{		
			LayoutProvider layout = null;
			string expr = "new "+this.entry4.Text+";";
			try
			{
				Mono.CSharp.Evaluator.Init(new string[] {});
				Mono.CSharp.Evaluator.LoadAssembly("NETGen.Core.dll");
				Mono.CSharp.Evaluator.LoadAssembly("NETGen.Layout.FruchtermanReingold.dll");
				Mono.CSharp.Evaluator.LoadAssembly("NETGen.Layout.Random.dll");
				Mono.CSharp.Evaluator.LoadAssembly("NETGen.Layout.Radial.dll");
				Mono.CSharp.Evaluator.Run("using System;");
				Mono.CSharp.Evaluator.Run("using NETGen.Core;");
				Mono.CSharp.Evaluator.Run("using NETGen.Layouts.FruchtermanReingold;");
				Mono.CSharp.Evaluator.Run("using NETGen.Layouts.RandomLayout;");
				Mono.CSharp.Evaluator.Run("using NETGen.Layouts.Radial;");
				
				layout = (LayoutProvider) Mono.CSharp.Evaluator.Evaluate(expr);
			}
			catch(Exception ex)
			{
				Logger.AddMessage(LogEntryType.Error, ex.Message);
			}
			if(layout!=null)
			{
				NetworkVisualizer.Layout = layout;
				NetworkVisualizer.Layout.DoLayoutAsync();
			}
		}

		void HandleBtnApplyEdgeWidthhandleClicked (object sender, EventArgs e)
		{
			string expr = "new Func<Edge, float>(e => {return "+this.entryNodeSizeExpression.Text+";});";
			try
			{
				Mono.CSharp.Evaluator.Init(new string[] {});
				Mono.CSharp.Evaluator.LoadAssembly("NETGen.Core.dll");
				Mono.CSharp.Evaluator.Run("using System;");
				Mono.CSharp.Evaluator.Run("using NETGen.Core;");
				
				NetworkVisualizer.ComputeEdgeWidth = (Func<Edge,float>) Mono.CSharp.Evaluator.Evaluate(expr);				
			}
			catch(Exception ex)
			{
				Logger.AddMessage(LogEntryType.Error, ex.Message);
			}
		}

		void HandleBtnApplyNodeSizehandleClicked (object sender, EventArgs e)
		{			
			string expr = "new Func<Vertex, float>(v => {return "+this.entryNodeSizeExpression.Text+";});";
			try
			{
				Mono.CSharp.Evaluator.Init(new string[] {});
				Mono.CSharp.Evaluator.LoadAssembly("NETGen.Core.dll");
				Mono.CSharp.Evaluator.Run("using System;");
				Mono.CSharp.Evaluator.Run("using NETGen.Core;");
				
				NetworkVisualizer.ComputeNodeSize = (Func<Vertex,float>) Mono.CSharp.Evaluator.Evaluate(expr);				
			}
			catch(Exception ex)
			{
				Logger.AddMessage(LogEntryType.Error, ex.Message);
			}
		}

		void HandleFileChooserHighlightNodeshandleFileSet (object sender, EventArgs e)
		{
			string[] lines = System.IO.File.ReadAllLines(this.fileChooserHighlightNodes.Filename);
			foreach(string s in lines)
			{
				Vertex v = n.SearchVertex(s);
				if(v!=null)
					colorizer[v] = ConvertGTKColor(this.btnChoseHighlightNodeColor.Color);
			}
		}

		void HandleFileChooserExportBitmaphandleFileSet (object sender, EventArgs e)
		{
			NetworkVisualizer.SaveCurrentImage(this.fileChooserExportBitmap.Filename);
		}

		void HandleFileChooserExportPDFhandleFileSet (object sender, EventArgs e)
		{
			PDFExporter.CreatePDF(this.fileChooserExportPDF.Filename, n, NetworkVisualizer.Layout, colorizer);
		}

		void HandleBtnSearchhandleClicked (object sender, EventArgs e)
		{
			Vertex v = n.SearchVertex(this.entrySearchNode.Text);
			if (v!=null)
				NetworkVisualizer.SelectedVertex = v;
		}

		void HandleBtnChoseEdgeColorhandleColorSet (object sender, EventArgs e)
		{
			colorizer.DefaultEdgeColor = ConvertGTKColor(this.btnChoseEdgeColor.Color);
		}

		void HandleBtnChoseDefaultNodeColorhandleColorSet (object sender, EventArgs e)
		{
			colorizer.DefaultVertexColor = ConvertGTKColor(this.btnChoseDefaultNodeColor.Color);
		}

		void HandleFileChooserNetworkFilehandleFileSet (object sender, EventArgs e)
		{
			if (this.fileChooserNetworkFile.Filename.EndsWith("cxf"))			
				n = Network.LoadFromCXF(this.fileChooserNetworkFile.Filename);
			else if (this.fileChooserNetworkFile.Filename.EndsWith("graphml"))
				n = Network.LoadFromGraphML(this.fileChooserNetworkFile.Filename);
			else
				n = Network.LoadFromEdgeFile(this.fileChooserNetworkFile.Filename);
			NetworkVisualizer.Start(n, new NETGen.Layouts.RandomLayout.RandomLayout(), colorizer, 800, 600);
		}
	}
}

