using System;
using System.Drawing;
using System.Threading;
using System.Collections.Generic;

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
		private ILayoutProvider _layout;
		
		private System.Drawing.Point _panStart;			
		private bool _panning = false;		
		private double _panX = 0d;
		private double _panY = 0d;
		private double _deltaX = 0d;
		private double _deltaY = 0d;		
		private double _zoom = 1d;
	
 
		internal NetworkVisualizer(Network network, ILayoutProvider layout, NetworkColorizer colorizer, int width, int height) : base(width, height, GraphicsMode.Default, "NETGen Display")
		{
			Keyboard.KeyDown += new EventHandler<KeyboardKeyEventArgs>(Keyboard_KeyDown);
			Mouse.ButtonDown += new EventHandler<MouseButtonEventArgs>(Mouse_ButtonDown);
			Mouse.ButtonUp += new EventHandler<MouseButtonEventArgs>(Mouse_ButtonUp);
			Mouse.Move += new EventHandler<MouseMoveEventArgs>(Mouse_Move);		
			Mouse.WheelChanged += new EventHandler<MouseWheelEventArgs>(Mouse_WheelChanged);			

			
			if (colorizer == null)
				_colorizer = new NetworkColorizer();
			else
				_colorizer = colorizer;
			
			_network = network;
			_layout = layout;		

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
 
			Title = "Rendering at "+ Fps.GetFps(e.Time).ToString() + " fps";
		}

 
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
			
			// Apply the layout if necessary
			if(!_layout.IsLaidout())
				_layout.DoLayout(Width, Height, _network);
			
			// Draw the edges
			foreach(Edge edge in _network.Edges)
				DrawEdge(edge, _colorizer[edge]);
			
			// Draw the vertices
			foreach(Vertex v in _network.Vertices)
				DrawVertex(v, _colorizer[v], 10);
			
 
			// Swap screen and backbuffer
			SwapBuffers();
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
		void DrawVertex(Vertex v, Color c, int segments)
        {
            GL.Color3(c);
            GL.Begin(BeginMode.TriangleFan);

            for (int i = 0; i < 360; i+=360/segments)
            {
                double degInRad = i * 3.1416/180;
                GL.Vertex2(_layout.GetPositionOfNode(v).X + Math.Cos(degInRad) * 3, _layout.GetPositionOfNode(v).Y+Math.Sin(degInRad) * 3);
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
		public static void Start(Network network, ILayoutProvider layout, NetworkColorizer colorizer = null, int width=800, int height=600)
		{			
			// The actual rendering needs to be done in a separate thread placed in the single thread appartment state
			_mainThread = new Thread(new ThreadStart(new Action(delegate() {				
					NetworkVisualizer p =  new NetworkVisualizer(network, layout, colorizer, width, height);
					p.Run(80f);
            })));
						
            _mainThread.SetApartmentState(ApartmentState.STA);
            _mainThread.Name = "STA Thread for NETGen Visualizer";
			
			// Fire up the thread
            _mainThread.Start();
			
		}
	}

}

