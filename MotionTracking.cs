//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.BodyBasics
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Windows;
	using System.Windows.Media;
	using System.Windows.Media.Imaging;
	using Microsoft.Kinect;

	/// <summary>
	/// Interaction logic for MainWindow
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		/// <summary>
		/// Radius of drawn hand circles
		/// </summary>
		private const double HandSize = 30;
		string wchStep = "";
		decimal startRightWrist = 0;
		decimal startLeftKnee = 0;
		decimal startRightFoot = 0;
		decimal startRightAnkle = 0;
		decimal startLeftFoot = 0;
		decimal currentLeftKnee = 0;
		decimal currentRightWrist = 0;
		decimal chkPoint = 0;

		/// <summary>
		/// Thickness of drawn joint lines
		/// </summary>
		private const double JointThickness = 3;

		/// <summary>
		/// Thickness of clip edge rectangles
		/// </summary>
		private const double ClipBoundsThickness = 10;

		/// <summary>
		/// Constant for clamping Z values of camera space points from being negative
		/// </summary>
		private const float InferredZPositionClamp = 0.1f;

		/// <summary>
		/// Brush used for drawing hands that are currently tracked as closed
		/// </summary>
		private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

		/// <summary>
		/// Brush used for drawing hands that are currently tracked as opened
		/// </summary>
		private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

		/// <summary>
		/// Brush used for drawing hands that are currently tracked as in lasso (pointer) position
		/// </summary>
		private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

		/// <summary>
		/// Brush used for drawing joints that are currently tracked
		/// </summary>
		private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

		/// <summary>
		/// Brush used for drawing joints that are currently inferred
		/// </summary>        
		private readonly Brush inferredJointBrush = Brushes.Yellow;

		/// <summary>
		/// Pen used for drawing bones that are currently inferred
		/// </summary>        
		private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

		/// <summary>
		/// Drawing group for body rendering output
		/// </summary>
		private DrawingGroup drawingGroup;

		/// <summary>
		/// Drawing image that we will display
		/// </summary>
		private DrawingImage imageSource;

		/// <summary>
		/// Active Kinect sensor
		/// </summary>
		private KinectSensor kinectSensor = null;

		/// <summary>
		/// Coordinate mapper to map one type of point to another
		/// </summary>
		private CoordinateMapper coordinateMapper = null;

		/// <summary>
		/// Reader for body frames
		/// </summary>
		private BodyFrameReader bodyFrameReader = null;

		/// <summary>
		/// Array for the bodies
		/// </summary>
		private Body[] bodies = null;

		/// <summary>
		/// definition of bones
		/// </summary>
		private List<Tuple<JointType, JointType>> bones;

		/// <summary>
		/// Width of display (depth space)
		/// </summary>
		private int displayWidth;

		/// <summary>
		/// Height of display (depth space)
		/// </summary>
		private int displayHeight;

		/// <summary>
		/// List of colors for each body tracked
		/// </summary>
		private List<Pen> bodyColors;

		/// <summary>
		/// Current status text to display
		/// </summary>
		private string statusText = null;

		/// <summary>
		/// Initializes a new instance of the MainWindow class.
		/// </summary>
		public MainWindow()
		{
			// one sensor is currently supported
			this.kinectSensor = KinectSensor.GetDefault();

			// get the coordinate mapper
			this.coordinateMapper = this.kinectSensor.CoordinateMapper;

			// get the depth (display) extents
			FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

			// get size of joint space
			this.displayWidth = frameDescription.Width;
			this.displayHeight = frameDescription.Height;

			// open the reader for the body frames
			this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

			// a bone defined as a line between two joints
			this.bones = new List<Tuple<JointType, JointType>>();

			// Torso
			this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
			this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
			this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
			this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
			this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
			this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
			this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
			this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

			// Right Arm
			this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
			this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
			this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
			this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
			this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

			// Left Arm
			this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
			this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
			this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
			this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
			this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

			// Right Leg
			this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
			this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
			this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

			// Left Leg
			this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
			this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
			this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

			// populate body colors, one for each BodyIndex
			this.bodyColors = new List<Pen>();

			this.bodyColors.Add(new Pen(Brushes.Red, 6));
			this.bodyColors.Add(new Pen(Brushes.Orange, 6));
			this.bodyColors.Add(new Pen(Brushes.Green, 6));
			this.bodyColors.Add(new Pen(Brushes.Blue, 6));
			this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
			this.bodyColors.Add(new Pen(Brushes.Violet, 6));

			// set IsAvailableChanged event notifier
			this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

			// open the sensor
			this.kinectSensor.Open();

			// set the status text
			this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
															: Properties.Resources.NoSensorStatusText;

			// Create the drawing group we'll use for drawing
			this.drawingGroup = new DrawingGroup();

			// Create an image source that we can use in our image control
			this.imageSource = new DrawingImage(this.drawingGroup);

			// use the window object as the view model in this simple example
			this.DataContext = this;

			// initialize the components (controls) of the window
			this.InitializeComponent();
		}

		/// <summary>
		/// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Gets the bitmap to display
		/// </summary>
		public ImageSource ImageSource
		{
			get
			{
				return this.imageSource;
			}
		}

		/// <summary>
		/// Gets or sets the current status text to display
		/// </summary>
		public string StatusText
		{
			get
			{
				return this.statusText;
			}

			set
			{
				if (this.statusText != value)
				{
					this.statusText = value;

					// notify any bound elements that the text has changed
					if (this.PropertyChanged != null)
					{
						this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
					}
				}
			}
		}

		/// <summary>
		/// Execute start up tasks
		/// </summary>
		/// <param name="sender">object sending the event</param>
		/// <param name="e">event arguments</param>
		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (this.bodyFrameReader != null)
			{
				this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
			}
		}

		/// <summary>
		/// Execute shutdown tasks
		/// </summary>
		/// <param name="sender">object sending the event</param>
		/// <param name="e">event arguments</param>
		private void MainWindow_Closing(object sender, CancelEventArgs e)
		{
			if (this.bodyFrameReader != null)
			{
				// BodyFrameReader is IDisposable
				this.bodyFrameReader.Dispose();
				this.bodyFrameReader = null;
			}

			if (this.kinectSensor != null)
			{
				this.kinectSensor.Close();
				this.kinectSensor = null;
			}
		}

		/// <summary>
		/// Handles the body frame data arriving from the sensor
		/// </summary>
		/// <param name="sender">object sending the event</param>
		/// <param name="e">event arguments</param>
		private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
		{
			bool dataReceived = false;

			using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
			{
				if (bodyFrame != null)
				{
					if (this.bodies == null)
					{
						this.bodies = new Body[bodyFrame.BodyCount];
					}

					// The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
					// As long as those body objects are not disposed and not set to null in the array,
					// those body objects will be re-used.
					bodyFrame.GetAndRefreshBodyData(this.bodies);
					dataReceived = true;
				}
			}

			if (dataReceived)
			{
				using (DrawingContext dc = this.drawingGroup.Open())
				{
					// Draw a transparent background to set the render size
					dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));


					int penIndex = 0;
					foreach (Body body in this.bodies)
					{
						Pen drawPen = this.bodyColors[penIndex++];

						if (body.IsTracked)
						{

							this.DrawClippedEdges(body, dc);

							IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

							// convert the joint points to depth (display) space
							Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

							foreach (JointType jointType in joints.Keys)
							{
								// sometimes the depth(Z) of an inferred joint may show as negative
								// clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
								CameraSpacePoint position = joints[jointType].Position;
								if (position.Z < 0)
								{
									position.Z = InferredZPositionClamp;
								}

								DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
								jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
							}

							this.DrawBody(joints, jointPoints, dc, drawPen);

							//this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
							//this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);





							//    IReadOnlyDictionary<JointType, Joint> joints = body.Joints;
							if (wchStep == "start" || wchStep == "f0" || wchStep == "f1" || wchStep == "f2" || wchStep == "f3" || wchStep == "f4")
							{
								string posP1 = joints[JointType.Head].Position.X + "," + joints[JointType.Head].Position.Y + "," + joints[JointType.Head].Position.Z;
								string posP2 = joints[JointType.Neck].Position.X + "," + joints[JointType.Neck].Position.Y + "," + joints[JointType.Neck].Position.Z;
								string posP3 = joints[JointType.ShoulderLeft].Position.X + "," + joints[JointType.ShoulderLeft].Position.Y + "," + joints[JointType.ShoulderLeft].Position.Z;
								string posP4 = joints[JointType.ShoulderRight].Position.X + "," + joints[JointType.ShoulderRight].Position.Y + "," + joints[JointType.ShoulderRight].Position.Z;
								string posP5 = joints[JointType.ElbowLeft].Position.X + "," + joints[JointType.ElbowLeft].Position.Y + "," + joints[JointType.ElbowLeft].Position.Z;
								string posP6 = joints[JointType.ElbowRight].Position.X + "," + joints[JointType.ElbowRight].Position.Y + "," + joints[JointType.ElbowRight].Position.Z;
								string posP7 = joints[JointType.WristLeft].Position.X + "," + joints[JointType.WristLeft].Position.Y + "," + joints[JointType.WristLeft].Position.Z;
								string posP8 = joints[JointType.WristRight].Position.X + "," + joints[JointType.WristRight].Position.Y + "," + joints[JointType.WristRight].Position.Z;
								string posP9 = joints[JointType.SpineBase].Position.X + "," + joints[JointType.SpineBase].Position.Y + "," + joints[JointType.SpineBase].Position.Z;
								string posP10 = joints[JointType.HipLeft].Position.X + "," + joints[JointType.HipLeft].Position.Y + "," + joints[JointType.HipLeft].Position.Z;
								string posP11 = joints[JointType.HipRight].Position.X + "," + joints[JointType.HipRight].Position.Y + "," + joints[JointType.HipRight].Position.Z;
								string posP12 = joints[JointType.KneeLeft].Position.X + "," + joints[JointType.KneeLeft].Position.Y + "," + joints[JointType.KneeLeft].Position.Z;
								string posP13 = joints[JointType.KneeRight].Position.X + "," + joints[JointType.KneeRight].Position.Y + "," + joints[JointType.KneeRight].Position.Z;
								string posP14 = joints[JointType.FootLeft].Position.X + "," + joints[JointType.FootLeft].Position.Y + "," + joints[JointType.FootLeft].Position.Z;
								string posP15 = joints[JointType.FootRight].Position.X + "," + joints[JointType.FootRight].Position.Y + "," + joints[JointType.FootRight].Position.Z;
								string posFull = posP1 + "#" + posP2 + "#" + posP3 + "#" + posP4 + "#" + posP5 + "#" + posP6 + "#" + posP7 + "#" + posP8 + "#" + posP9 + "#" + posP10 + "#" + posP11 + "#" + posP12 + "#" + posP13 + "#" + posP14 + "#" + posP15;
								writeValues(posFull);
								stepName.Text = "Step: " + wchStep;
							}

							if (wchStep == "f4")
							{
								wchStep = "f4-end";
							}

							// Right Wrist at maximum position
							if (wchStep == "f3-end")
							{
								chkPoint = Math.Abs(Math.Round((decimal)joints[JointType.KneeLeft].Position.X, 4));

								//Console.WriteLine(chkPoint + " : " + currentLeftKnee);
								//Console.WriteLine(Math.Abs(chkPoint - currentLeftKnee));



								decimal newChkPoint = chkPoint + (decimal)0.0155;
								if (newChkPoint >= currentRightWrist)
								{
									currentRightWrist = chkPoint;
								}
								else
								{
									Console.WriteLine("Step: F4");
									wchStep = "f4";
								}
							}


							if (wchStep == "f3")
							{
								wchStep = "f3-end";
							}

							// Left foot hits ground
							if (wchStep == "f2-end")
							{
								chkPoint = Math.Abs(Math.Round((decimal)joints[JointType.FootLeft].Position.Y, 4));
								decimal diffPos = Math.Abs(chkPoint - startLeftFoot);
								//Console.WriteLine(diffPos);
								if (diffPos <= (decimal)1.75)
								{
									Console.WriteLine("Step: F3");
									wchStep = "f3";
								}
							}

							if (wchStep == "f2")
							{
								wchStep = "f2-end";
							}

							// Right foot hits ground
							if (wchStep == "f1-end")
							{
								chkPoint = Math.Abs(Math.Round((decimal)joints[JointType.FootRight].Position.Y, 4));
								decimal diffPos = Math.Abs(chkPoint - startRightFoot);
								Console.WriteLine(diffPos);
								if (diffPos <= (decimal)1.75)
								{
									Console.WriteLine("Step: F2");
									wchStep = "f2";
								}
							}

							if (wchStep == "f1")
							{
								wchStep = "f1-end";
							}

							// Right foot raised
							if (wchStep == "f0-end")
							{
								chkPoint = Math.Abs(Math.Round((decimal)joints[JointType.FootRight].Position.Y, 4));
								decimal diffPos = Math.Abs(chkPoint - startRightAnkle);

								//Console.WriteLine(chkPoint + " : " + startRightAnkle);
								//Console.WriteLine(Math.Abs(chkPoint - startRightAnkle));

								if (diffPos > (decimal)0.1)
								{
									Console.WriteLine("Step: F1");
									wchStep = "f1";
								}
							}


							if (wchStep == "f0")
							{
								wchStep = "f0-end";
							}

							// Left Knee at maximum position
							if (wchStep == "start-end")
							{
								chkPoint = Math.Abs(Math.Round((decimal)joints[JointType.KneeLeft].Position.X, 4));

								//Console.WriteLine(chkPoint + " : " + currentLeftKnee);
								//Console.WriteLine(Math.Abs(chkPoint - currentLeftKnee));



								decimal newChkPoint = chkPoint + (decimal)0.0155;
								if (newChkPoint >= currentLeftKnee)
								{
									currentLeftKnee = chkPoint;
								}
								else
								{
									Console.WriteLine("Step: F0");
									wchStep = "f0";
								}
							}

							if (wchStep == "start")
							{
								Console.WriteLine("Step: Start");
								wchStep = "start-end";
								startRightWrist = Math.Abs(Math.Round((decimal)joints[JointType.WristRight].Position.X, 4));
								startLeftKnee = Math.Abs(Math.Round((decimal)joints[JointType.KneeLeft].Position.X, 4));
								startLeftFoot = Math.Round((decimal)joints[JointType.FootLeft].Position.X, 4);
								startRightFoot = Math.Round((decimal)joints[JointType.FootRight].Position.Y, 4);
								startRightAnkle = Math.Abs(Math.Round((decimal)joints[JointType.AnkleRight].Position.Y, 4));
								currentLeftKnee = startLeftKnee;
								currentRightWrist = startRightWrist;
							}






						}
					}


					// prevent drawing outside of our render area
					this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
				}
			}
		}

		public void writeValues(string posFull)
		{
			using (StreamWriter sw = File.AppendText("TrackSkelton.txt"))
			{
				posFull = DateTime.Now + ":" + DateTime.Now.Millisecond + "#" + posFull;
				sw.WriteLine(posFull);
				sw.Close();
			}
		}
		// <summary>
		/// Draws a body
		/// </summary>
		/// <param name="joints">joints to draw</param>
		/// <param name="jointPoints">translated positions of joints to draw</param>
		/// <param name="drawingContext">drawing context to draw to</param>
		/// <param name="drawingPen">specifies color to draw a specific body</param>
		private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
		{
			// Draw the bones
			foreach (var bone in this.bones)
			{
				this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
			}

			// Draw the joints
			foreach (JointType jointType in joints.Keys)
			{
				Brush drawBrush = null;

				TrackingState trackingState = joints[jointType].TrackingState;

				if (trackingState == TrackingState.Tracked)
				{
					drawBrush = this.trackedJointBrush;
				}
				else if (trackingState == TrackingState.Inferred)
				{
					drawBrush = this.inferredJointBrush;
				}

				if (drawBrush != null)
				{
					drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
				}
			}
		}

		/// <summary>
		/// Draws one bone of a body (joint to joint)
		/// </summary>
		/// <param name="joints">joints to draw</param>
		/// <param name="jointPoints">translated positions of joints to draw</param>
		/// <param name="jointType0">first joint of bone to draw</param>
		/// <param name="jointType1">second joint of bone to draw</param>
		/// <param name="drawingContext">drawing context to draw to</param>
		/// /// <param name="drawingPen">specifies color to draw a specific bone</param>
		private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
		{
			Joint joint0 = joints[jointType0];
			Joint joint1 = joints[jointType1];

			// If we can't find either of these joints, exit
			if (joint0.TrackingState == TrackingState.NotTracked ||
				joint1.TrackingState == TrackingState.NotTracked)
			{
				return;
			}

			// We assume all drawn bones are inferred unless BOTH joints are tracked
			Pen drawPen = this.inferredBonePen;
			if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
			{
				drawPen = drawingPen;
			}

			drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
		}

		/// <summary>
		/// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
		/// </summary>
		/// <param name="handState">state of the hand</param>
		/// <param name="handPosition">position of the hand</param>
		/// <param name="drawingContext">drawing context to draw to</param>
		private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
		{
			switch (handState)
			{
				case HandState.Closed:
					drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
					break;

				case HandState.Open:
					drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
					break;

				case HandState.Lasso:
					drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
					break;
			}
		}

		/// <summary>
		/// Draws indicators to show which edges are clipping body data
		/// </summary>
		/// <param name="body">body to draw clipping information for</param>
		/// <param name="drawingContext">drawing context to draw to</param>
		private void DrawClippedEdges(Body body, DrawingContext drawingContext)
		{
			FrameEdges clippedEdges = body.ClippedEdges;

			if (clippedEdges.HasFlag(FrameEdges.Bottom))
			{
				drawingContext.DrawRectangle(
					Brushes.Red,
					null,
					new Rect(0, this.displayHeight - ClipBoundsThickness, this.displayWidth, ClipBoundsThickness));
			}

			if (clippedEdges.HasFlag(FrameEdges.Top))
			{
				drawingContext.DrawRectangle(
					Brushes.Red,
					null,
					new Rect(0, 0, this.displayWidth, ClipBoundsThickness));
			}

			if (clippedEdges.HasFlag(FrameEdges.Left))
			{
				drawingContext.DrawRectangle(
					Brushes.Red,
					null,
					new Rect(0, 0, ClipBoundsThickness, this.displayHeight));
			}

			if (clippedEdges.HasFlag(FrameEdges.Right))
			{
				drawingContext.DrawRectangle(
					Brushes.Red,
					null,
					new Rect(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight));
			}
		}

		/// <summary>
		/// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
		/// </summary>
		/// <param name="sender">object sending the event</param>
		/// <param name="e">event arguments</param>
		private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
		{
			// on failure, set the status text
			this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
															: Properties.Resources.SensorNotAvailableStatusText;
		}

		private void startTrack(object sender, RoutedEventArgs e)
		{
			wchStep = "start";


			startRightWrist = 0;
			startLeftKnee = 0;
			startRightFoot = 0;
			startRightAnkle = 0;
			startLeftFoot = 0;
			currentLeftKnee = 0;
			currentRightWrist = 0;
			chkPoint = 0;

			using (StreamWriter sw = new StreamWriter("TrackSkelton.txt"))
			{
				sw.Write("");
				sw.Close();
			}
		}

	}
}

