using System;
using System.Drawing;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
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
		private LayoutProvider _layout;
		private static Bitmap _screenshot = null;
		
		private System.Drawing.Point _panStart;			
		private bool _panning = false;		
		private double _panX = 0d;
		private double _panY = 0d;
		private double _deltaX = 0d;
		private double _deltaY = 0d;		
		private double _zoom = 1d;
		
		private static AutoResetEvent _initialized = new AutoResetEvent(false);
		private static AutoResetEvent _screenshotExists = new AutoResetEvent(false);
	
		private static NetworkVisualizer Instance;
		
 
		internal NetworkVisualizer(Network network, LayoutProvider layout, NetworkColorizer colorizer, int width, int height) : base(width, height, GraphicsMode.Default, "NETGen Display")
		{
			Keyboard.KeyDown += new EventHandler<KeyboardKeyEventArgs>(Keyboard_KeyDown);
			Mouse.ButtonDown += new EventHandler<MouseButtonEventArgs>(Mouse_ButtonDown);
			Mouse.ButtonUp += new EventHandler<MouseButtonEventArgs>(Mouse_ButtonUp);
			Mouse.Move += new EventHandler<MouseMoveEventArgs>(Mouse_Move);		
			Mouse.WheelChanged += new EventHandler<MouseWheelEventArgs>(Mouse_WheelChanged);			
			
			_layout = layout;			
			_network = network;
			
			_layout.Init(Width, Height, _network); 
			
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
		
		void Mouse_ButtonDown(object sender, MouseEventArgs e)
		{
			_panning = true;
			_panStart = new Point((int) e.Position.X, (int) e.Position.Y);
		}
		
		void Mouse_ButtonUp(object sender, MouseEventArgs e)
		{
			_panning = false;
			_panX += _deltaX;
			_panY += _deltaY;
			_deltaX = 0d;
			_deltaY = 0d;
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
			GL.Ortho(0, Width, Height, 0, -1, 1);
			GL.Viewport(0, 0, Width, Height);			
		}
 
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			
            GL.MatrixMode(MatrixMode.Projection);
			GL.LoadIdentity();
			GL.Ortho(0, Width, Height, 0, -1, 1);
			GL.Viewport(0, 0, Width, Height);
			
		 	GL.ClearColor(_colorizer.DefaultBackgroundColor);
		}
 
		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			base.OnUpdateFrame(e); 					
 
			Title = "Rendering network at "+ Fps.GetFps(e.Time).ToString() + " fps";
		}
		
		/// <summary>
		/// Updates the layout of the visualized network. 
		/// </summary>
		public static void ComputeLayout()
		{
			Instance._layout.DoLayout();
		}
		

 		[MethodImpl(MethodImplOptions.Synchronized)]
		protected override void OnRenderFrame(FrameEventArgs e)
		{			
			base.OnRenderFrame(e);
 
			// Create an identity matrix, apply orthogonal projection and viewport
			GL.LoadIdentity();
			GL.Ortho(0, Width, Height, 0, -1, 1);
			GL.Viewport(0, 0, Width, Height);		
			
			// Apply panning and zooming state			
			GL.Scale(_zoom, _zoom, _zoom);
			GL.Translate(_panX+_deltaX, _panY+_deltaY, 0);
			
			// Clear the buffer
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			GL.ClearColor(_colorizer.DefaultBackgroundColor);
			
			// Draw the edges
			foreach(Edge edge in _network.Edges)
				DrawEdge(edge, _colorizer[edge]);
			
			// Draw the vertices
			foreach(Vertex v in _network.Vertices)
				DrawVertex(v, _colorizer[v], 5, 2);
			
 
			// Swap screen and backbuffer
			SwapBuffers();
			
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
		void DrawEdge(Edge e, Color c)
		{
			GL.Color3(c);
			GL.Begin(BeginMode.Lines);			
			
			GL.Vertex2(_layout.GetPositionOfNode(e.Source).X, _layout.GetPositionOfNode(e.Source).Y);
			GL.Vertex2(_layout.GetPositionOfNode(e.Target).X, _layout.GetPositionOfNode(e.Target).Y);
			
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
		void DrawVertex(Vertex v, Color c, int segments, int radius)
        {
            GL.Color3(c);
            GL.Begin(BeginMode.TriangleFan);

            for (int i = 0; i < 360; i+=360/segments)
            {
                double degInRad = i * 3.1416/180;
                GL.Vertex2(_layout.GetPositionOfNode(v).X + Math.Cos(degInRad) * radius, _layout.GetPositionOfNode(v).Y+Math.Sin(degInRad) * radius);
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
            if (GraphicsContext.CurrentContext == null)
                throw new GraphicsContextMissingException();
 
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
				Logger.AddMessage(LogEntryType.Warning, "Error while copzing screen buffer to bitmap.");
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
		}
			

	}

}

