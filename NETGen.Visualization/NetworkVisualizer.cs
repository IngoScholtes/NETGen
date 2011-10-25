using System;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics.OpenGL;

using NETGen.Core;

namespace NETGen.Visualization
{
	internal static class Fps
	{
		static double _time = 0.0, _frames = 0.0;
		static int _fps = 0;
 
		public static int GetFps(double time) {
			_time += time;
			if (_time < 1.0) {
				_frames++;
				return _fps;
			}
			else {
				_fps = (int)_frames;
				_time = 0.0;
				_frames = 0.0;
				return _fps;
			}
		}
	}
	
	/// <summary>
	/// Network visualizer.
	/// </summary>
	public class NetworkVisualizer : GameWindow
	{	
		private static Thread _mainThread;
		
		private Network _network;		
		private NetworkColorizer _colorizer;		
		private static LayoutProvider _layout;
		private static Bitmap _screenshot = null;
		private bool screenshot = false;
		
		private System.Drawing.Point _panStart;			
		private bool _panning = false;		
		private double _panX = 0d;
		private double _panY = 0d;
		private double _deltaX = 0d;
		private double _deltaY = 0d;		
		private double _zoom = 1d;
		
		private bool _drawMarker = false;
		
		private static AutoResetEvent _initialized = new AutoResetEvent(false);
		private static AutoResetEvent _screenshotExists = new AutoResetEvent(false);		
	
		private static NetworkVisualizer Instance;
		
		public static Vertex SelectedVertex = null;
		
		public static Func<Vertex, float> ComputeNodeSize;
		public static Func<Edge, float> ComputeEdgeWidth;
		
		double[] matView = new double[16];
		double[] matProj = new double[16];
		int[] viewport = new int[4];
		
		public static LayoutProvider Layout { 
			get { return _layout; } 
			set { value.Init(Instance.Width, Instance.Height, Instance._network);				
				 _layout = value;
			}
		}

 
		internal NetworkVisualizer(Network network, LayoutProvider layout, NetworkColorizer colorizer, int width, int height) : base(width, height, OpenTK.Graphics.GraphicsMode.Default, "NETGen Display")
		{
			Keyboard.KeyDown += new EventHandler<KeyboardKeyEventArgs>(Keyboard_KeyDown);
			Mouse.ButtonDown += new EventHandler<MouseButtonEventArgs>(Mouse_ButtonDown);
			Mouse.ButtonUp += new EventHandler<MouseButtonEventArgs>(Mouse_ButtonUp);
			Mouse.Move += new EventHandler<MouseMoveEventArgs>(Mouse_Move);		
			Mouse.WheelChanged += new EventHandler<MouseWheelEventArgs>(Mouse_WheelChanged);	
			
			ComputeNodeSize = new Func<Vertex, float>(v => {
				return 2f;
			});
			
			ComputeEdgeWidth = new Func<Edge, float>( e => {
				return 0.05f;
			});
			_network = network;
			
			_layout = layout;
			_layout.Init(Width, Height, network);
			
			if (colorizer == null)
				_colorizer = new NetworkColorizer();
			else
				_colorizer = colorizer;
		}
 
		void Keyboard_KeyDown(object sender, KeyboardKeyEventArgs e)
		{
			if (e.Key == Key.Escape)
				Exit();
		}
		
		void Mouse_ButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (e.Button == MouseButton.Left)
			{
				_panning = true;
				_panStart = new Point((int) e.Position.X, (int) e.Position.Y);
			}
			else if (e.Button == MouseButton.Right)
				_drawMarker = true;
		}
		
		void Mouse_ButtonUp(object sender, MouseButtonEventArgs e)
		{			
			if (e.Button == MouseButton.Left)
			{
				_panning = false;
				_panX += _deltaX;
				_panY += _deltaY;
				_deltaX = 0d;
				_deltaY = 0d;
			}
			else if (e.Button == MouseButton.Right)
			{
				
				SelectedVertex = GetVertexFromPosition(e.Position);						
				_drawMarker = false;
			}		
		}
		
		[MethodImpl(MethodImplOptions.Synchronized)]
		OpenTK.Vector3 ScreenToWorld(OpenTK.Vector3 screencoord)
		{
			OpenTK.Vector3 worldcoord = new OpenTK.Vector3();
			screencoord.Y = Height-screencoord.Y;
			screencoord.Z = 0;
			OpenTK.Graphics.Glu.UnProject(screencoord, matView, matProj, viewport, out worldcoord);
			return worldcoord;
		}
		
		Vertex GetVertexFromPosition(System.Drawing.Point position)
		{			
			OpenTK.Vector3 screencoord = new OpenTK.Vector3(position.X, position.Y, 0);				
			Console.WriteLine("Clicked at " + position.X + "," + position.Y);
			OpenTK.Vector3 worldcoord = ScreenToWorld(screencoord);
			
			Vertex selected = null;
			double dist = double.MaxValue;
			
			Vector3 clickPos = new Vector3(worldcoord.X, worldcoord.Y, 0d);
			foreach(Vertex v in _network.Vertices.ToArray())
			{
				Vector3 p = Layout.GetPositionOfNode(v);										
				p.Z = 0d;
				
				if (Vector3.Distance(p, clickPos)<dist && Vector3.Distance(p, clickPos)<2)
				{
					dist = Vector3.Distance(p, clickPos);
					selected = v;
				}
			}
			if (selected!=null)
				Console.WriteLine(selected.Label + " at distance " + dist);
			return selected;
		}
		
		void Mouse_WheelChanged(object sender, MouseWheelEventArgs e)
		{
			_zoom += e.DeltaPrecise/4f;
		}
		
		void Mouse_Move(object sender, MouseEventArgs e)
		{
			if(_panning)
			{
				_deltaX = e.X - _panStart.X;
				_deltaY = e.Y - _panStart.Y;				
			}
		}
		
		protected override void OnResize(EventArgs e)
		{
			
			base.OnResize(e);
			
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadIdentity();
			GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);
			GL.Ortho(0, Width, Height, 0, -1, 1);
			GL.Viewport(0, 0, Width, Height);
			
			// Store matrices for unprojecting ... 
			GL.GetDouble(GetPName.ModelviewMatrix, matView);
			GL.GetDouble(GetPName.ProjectionMatrix, matProj);
			GL.GetInteger(GetPName.Viewport, viewport);
		}
 
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			
            GL.MatrixMode(MatrixMode.Projection);
			GL.LoadIdentity();
			GL.Ortho(0, Width, Height, 0, -1, 1);
			GL.Viewport(0, 0, Width, Height);
			
			// Store matrices for unprojecting ... 
			GL.GetDouble(GetPName.ModelviewMatrix, matView);
			GL.GetDouble(GetPName.ProjectionMatrix, matProj);
			GL.GetInteger(GetPName.Viewport, viewport);
			
		 	GL.ClearColor(_colorizer.DefaultBackgroundColor);
		}
 
		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			base.OnUpdateFrame(e); 					
 
			Title = "Rendering network at "+ Fps.GetFps(e.Time).ToString() + " fps";
		}

 		[MethodImpl(MethodImplOptions.Synchronized)]
		protected override void OnRenderFrame(FrameEventArgs e)
		{			
			base.OnRenderFrame(e);
 
			// Create an identity matrix, apply orthogonal projection and viewport			
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadIdentity();
			GL.Ortho(0, Width, Height, 0, -1, 1);
			GL.Viewport(0, 0, Width, Height);		
			
			// Apply panning and zooming state			
			GL.Scale(_zoom, _zoom, _zoom);
			GL.Translate(_panX+_deltaX, _panY+_deltaY, 0);
			
			// Store matrices for unprojecting ... 
			GL.GetDouble(GetPName.ModelviewMatrix, matView);
			GL.GetDouble(GetPName.ProjectionMatrix, matProj);
			GL.GetInteger(GetPName.Viewport, viewport);
			
			// Clear the buffer
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			GL.ClearColor(_colorizer.DefaultBackgroundColor);		
			
			// Draw the edges
			foreach(Edge edge in _network.Edges)
				DrawEdge(edge, _colorizer[edge], ComputeEdgeWidth(edge));
			
			if(SelectedVertex != null)
				foreach(Edge edge in SelectedVertex.Edges)
					DrawEdge(edge, Color.Red, ComputeEdgeWidth(edge), true);
			
			// Draw the vertices
			foreach(Vertex v in _network.Vertices)
				DrawVertex(v, _colorizer[v], 10, ComputeNodeSize(v));
			
			if(SelectedVertex != null)
				DrawVertex(SelectedVertex, Color.Red, 10, ComputeNodeSize(SelectedVertex), true);
			
			if(_drawMarker)
				DrawMarker(Color.Red, 10, 2);
			
			// Swap screen and backbuffer
			SwapBuffers();
			
			if(screenshot)
				GrabImage();
		}
		
		/// <summary>
		/// Draws an edge as a simple line between two node positions
		/// </summary>
		/// <param name='e'>
		/// The edge to paint
		/// </param>
		/// <param name='c'>
		/// The color to use for the edge
		/// </param>
		void DrawEdge(Edge e, Color c, float width, bool drawselected = false)
		{
			if ( !drawselected && SelectedVertex!=null && (e.Source == SelectedVertex || e.Target == SelectedVertex))
				return;
			
			GL.Color3(c);
			GL.LineWidth(width);
			GL.Begin(BeginMode.Lines);
			
			GL.Vertex2(Layout.GetPositionOfNode(e.Source).X, Layout.GetPositionOfNode(e.Source).Y);
			GL.Vertex2(Layout.GetPositionOfNode(e.Target).X, Layout.GetPositionOfNode(e.Target).Y);
			
			GL.End();
		}
		
		void DrawMarker(Color c, int segments, int radius)
		{
			OpenTK.Vector3 pos = ScreenToWorld(new OpenTK.Vector3(Mouse.X, Mouse.Y, 0));
			GL.Color3(c);
            GL.Begin(BeginMode.TriangleFan);

            for (int i = 0; i < 360; i+=360/segments)
            {
                double degInRad = i * 3.1416/180;
                GL.Vertex2(pos.X + Math.Cos(degInRad) * radius, pos.Y+Math.Sin(degInRad) * radius);
            }
			GL.End();
		}
		
		/// <summary>
		/// Draws a vertex as a simple circle made up from a configurable number of triangle segments
		/// </summary>
		/// <param name='v'>
		/// The vertex to draw
		/// </param>
		/// <param name='c'>
		/// The Color to use for the vertex
		/// </param>
		/// <param name='segments'>
		/// The number of triangle segments to use. A higher number will look more prety but will take more time to render
		/// </param>
		void DrawVertex(Vertex v, Color c, int segments, double radius, bool drawselected = false)
        {
			if(!drawselected && SelectedVertex !=null && v == SelectedVertex)
				return;
			
        	GL.Color3(c);
            GL.Begin(BeginMode.TriangleFan);

            for (int i = 0; i < 360; i+=360/segments)
            {
                double degInRad = i * 3.1416/180;
                GL.Vertex2(Layout.GetPositionOfNode(v).X + Math.Cos(degInRad) * radius, Layout.GetPositionOfNode(v).Y+Math.Sin(degInRad) * radius);
            }
			GL.End();
		}
 
		/// <summary>
		/// Creates a new instance of a Networkvisualizer which renders the specified network in real-time
		/// </summary>
		/// <param name='n'>
		/// N.
		/// </param>
		/// <param name='layout'>
		/// Layout.
		/// </param>
		public static void Start(Network network, LayoutProvider layout, NetworkColorizer colorizer = null, int width=800, int height=600)
		{			
			// The actual rendering needs to be done in a separate thread placed in the single thread appartment state
			_mainThread = new Thread(new ThreadStart(new Action(delegate() {				
					Instance =  new NetworkVisualizer(network, layout, colorizer, width, height);
					_initialized.Set();
					Instance.Run(80f);
            })));
						
            _mainThread.SetApartmentState(ApartmentState.STA);
            _mainThread.Name = "STA Thread for NETGen Visualizer";
			
			// Fire up the thread
            _mainThread.Start();
			_initialized.WaitOne();
		}
		
		[MethodImpl(MethodImplOptions.Synchronized)]
	    private static void GrabImage()
        {
            if (OpenTK.Graphics.GraphicsContext.CurrentContext == null)
                throw new OpenTK.Graphics.GraphicsContextMissingException();
 
            if(_screenshot==null)
				_screenshot = new Bitmap(Instance.ClientSize.Width, Instance.ClientSize.Height);
			
			try {
				lock(_screenshot)
				{
					 System.Drawing.Imaging.BitmapData data =
		             _screenshot.LockBits(Instance.ClientRectangle, System.Drawing.Imaging.ImageLockMode.WriteOnly, 
							System.Drawing.Imaging.PixelFormat.Format32bppArgb);
					
		            GL.ReadPixels(0, 0, Instance.ClientSize.Width, Instance.ClientSize.Height,PixelFormat.Bgra,
						PixelType.UnsignedByte, data.Scan0);
					
		            _screenshot.UnlockBits(data);
				}
				_screenshotExists.Set();
			}
			catch {
				Logger.AddMessage(LogEntryType.Warning, "Error while copying screen buffer to bitmap.");
			}

        }
		
		/// <summary>
		/// Saves the last rendered image to a bitmap file. If the path to an existing file is given, the file will be overwritten. This call will block until there is a rendered screenshot available. 
		/// </summary>
		/// <param name='filename'>
		/// The filename of the saved image.
		/// </param>
		public static void SaveCurrentImage(string filename)
		{
			Instance.screenshot = true;
			
			// Wait until there is a screenshot
			_screenshotExists.WaitOne();
			
			if(_screenshot != null && filename != null)
			{
				lock(_screenshot)
					_screenshot.Save(filename);
				Logger.AddMessage(LogEntryType.Info, "Network image has been written to  file");
			}
			else
			{
				Logger.AddMessage(LogEntryType.Warning, "Could not save network image");
			}
			
			Instance.screenshot = false;
			_screenshotExists.Reset();
			
		}
			

	}

}

