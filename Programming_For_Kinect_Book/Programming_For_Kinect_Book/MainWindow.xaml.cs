using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Kinect.Toolbox;
using System.Runtime.InteropServices;
using System.Diagnostics;
using WindowsInput;


namespace Programming_For_Kinect_Book
{ 
    public partial class MainWindow : Window
    {
        ColorStreamManager colorManager = new ColorStreamManager();
        DepthStreamManager depthManager = new DepthStreamManager();
        SkeletonDisplayManager skeletonManager;
        GestureDetector gestureDetector = new SwipeGestureDetector();
        ContextTracker contextTracker = new ContextTracker();
        Skeleton[] skeletons;
        Skeleton primarySkeleton;

        List<VirtualKeyCode> downKeyStrokes;

        KinectSensor kinectSensor;

        public IntPtr MainWindowHandle { get; set; }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern long SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                //listen to any status change for Kinects
                KinectSensor.KinectSensors.StatusChanged += Kinects_StatusChanged;
                //loop through all the Kinects attached to this PC, and start the first that is connected without an error.

                foreach (KinectSensor kinect in KinectSensor.KinectSensors)
                {
                    if (kinect.Status == KinectStatus.Connected)
                    {
                        kinectSensor = kinect;
                        break;
                    }
                }
                if (KinectSensor.KinectSensors.Count == 0)
                    MessageBox.Show("No Kinect found");
                else
                    Initialize(); // Initialization of the current sensor
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            kinectDisplay.DataContext = colorManager;
            //kinectDisplay.DataContext = depthManager;

        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.kinectSensor != null)
            {
                this.kinectSensor.Stop();
                clearKeyStrokes();
            }
        }

        void Kinects_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Connected:
                    if (kinectSensor == null)
                    {
                        kinectSensor = e.Sensor;
                        Initialize();
                    }
                    break;
                case KinectStatus.Disconnected:
                    if (kinectSensor == e.Sensor)
                    {
                        Clean();
                        MessageBox.Show("Kinect was disconnected");
                    }
                    break;
                case KinectStatus.NotReady:
                    break;
                case KinectStatus.NotPowered:
                    if (kinectSensor == e.Sensor)
                    {
                        Clean();
                        MessageBox.Show("Kinect is no longer powered");
                    }
                    break;
                default:
                    MessageBox.Show("Unhandled Status: " + e.Status);
                    break;
            }
        }

        private void Initialize()
        {
            if (kinectSensor == null)
                return;

            kinectSensor.ColorStream.Enable();
            //kinectSensor.DepthStream.Enable();
            kinectSensor.SkeletonStream.Enable();
                        
            kinectSensor.ColorFrameReady += kinectSensor_ColorFrameReady;
            //kinectSensor.DepthFrameReady += kinectSensor_DepthFrameReady;

            skeletonManager = new SkeletonDisplayManager(kinectSensor, skeletonCanvas);
            kinectSensor.SkeletonFrameReady += kinectSensor_SkeletonFrameReady;

            downKeyStrokes = new List<VirtualKeyCode>();

            gestureDetector.DisplayCanvas = gestureCanvas;

            kinectSensor.Start();
        }

        private void Clean()
        {
            if (kinectSensor != null)
            {
                kinectSensor.Stop();
                kinectSensor = null;
            }
        }

        void kinectSensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (var frame = e.OpenColorImageFrame())
            {
                if (frame == null)
                    return;
                colorManager.Update(frame);
            }
        }

        void kinectSensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (var frame = e.OpenDepthImageFrame())
            {
                if (frame == null)
                    return;

                depthManager.Update(frame);
            }
        }

        void kinectSensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame == null)
                    return;
                frame.GetSkeletons(ref skeletons);
                if (skeletons.All(s => s.TrackingState == SkeletonTrackingState.NotTracked))
                    return;

                if (primarySkeleton == null)
                {
                    foreach (Skeleton skeleton in skeletons)
                    {   
                        if (skeleton.TrackingId != 0)
                        {
                            contextTracker.Add(skeleton, JointType.HipCenter);

                            if(skeletonIsReady(skeleton))
                            {
                                primarySkeleton = skeleton;
                                Console.WriteLine("primary skeleton identified with id: " + primarySkeleton.TrackingId);
                            }
                        }
                    }
                }

                else //if primarySkeleton is on screen
                {
                    //If the whole skeleton is in view, draw and detect gestures
                    if (!HasClippedEdges(primarySkeleton)) 
                    {
                        skeletonManager.Draw(primarySkeleton);
                        doGestureDetection();
                    }

                    else
                    {
                        //remove previous primary skeleton from array and sket primarySkeleton to null to find new primary
                        for (int i = 0; i < skeletons.Length; i++)
                            if (skeletons[i].TrackingId == primarySkeleton.TrackingId)
                                skeletons[i] = null;

                        primarySkeleton = null;
                        clearKeyStrokes();
                    }
                }                 
            }
        }

        //Returns true if skeleton is too close to edge and has a clipped skeleton
        private Boolean HasClippedEdges(Skeleton skeleton)
        {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom) || skeleton.ClippedEdges.HasFlag(FrameEdges.Top)
                    || skeleton.ClippedEdges.HasFlag(FrameEdges.Left) || skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
                return true;
            else
                return false;
        }

        private Boolean skeletonIsReady(Skeleton skeleton)
        {
            if (contextTracker.IsStable(skeleton.TrackingId) && contextTracker.IsShouldersTowardsSensor(skeleton))
                return true;
            else
                return false;

        }

        private void RenderClippedEdges(Skeleton skeleton)
        {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                //DrawClippedEdges(FrameEdges.Bottom); // Make the border red to show the user is reaching the border
                //Console.WriteLine("Too close to bottom!");
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                //DrawClippedEdges(FrameEdges.Top);
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                //DrawClippedEdges(FrameEdges.Left);
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                //DrawClippedEdges(FrameEdges.Right);
            }
        }

        public void doGestureDetection()
        {
            gestureDetector.Add(primarySkeleton.Joints[JointType.HandRight].Position, kinectSensor);

            if (primarySkeleton.Joints[JointType.HandRight].Position.Y > primarySkeleton.Joints[JointType.Head].Position.Y
                        && primarySkeleton.Joints[JointType.HandLeft].Position.Y > primarySkeleton.Joints[JointType.Head].Position.Y)
            {
                
            }

            else if (primarySkeleton.Joints[JointType.HipCenter].Position.Z - primarySkeleton.Joints[JointType.HandLeft].Position.Z > 0.3f
                        && primarySkeleton.Joints[JointType.HipCenter].Position.Z - primarySkeleton.Joints[JointType.HandRight].Position.Z > 0.3f)
            {
                
                PressKeyLeftArrow();
            }

            //Step forward with one foot
            else if (primarySkeleton.Joints[JointType.AnkleLeft].Position.Z - primarySkeleton.Joints[JointType.AnkleRight].Position.Z > 0.2f ||
                        primarySkeleton.Joints[JointType.AnkleRight].Position.Z - primarySkeleton.Joints[JointType.AnkleLeft].Position.Z > 0.2f)
            {
                

                PressKeyUpArrow();

            }

            //Turn shoulders left
            else if (primarySkeleton.Joints[JointType.ShoulderLeft].Position.Z - primarySkeleton.Joints[JointType.ShoulderRight].Position.Z > 0.1f)
            {
                PressKeyLeftArrow();
            }

            //Turn shoulders right
            else if (primarySkeleton.Joints[JointType.ShoulderRight].Position.Z - primarySkeleton.Joints[JointType.ShoulderLeft].Position.Z > 0.1f)
            {
                PressKeyRightArrow();
            }
               
            //Look up
            else if (primarySkeleton.Joints[JointType.ShoulderCenter].Position.Z - primarySkeleton.Joints[JointType.HipCenter].Position.Z > 0.05f)
                        //&& primarySkeleton.Joints[JointType.ShoulderCenter].Position.Z - primarySkeleton.Joints[JointType.FootRight].Position.Z > 0.05f)
            {
                PressKeyCtrlUpArrow();
            }
            else if (primarySkeleton.Joints[JointType.HipCenter].Position.Z - primarySkeleton.Joints[JointType.ShoulderCenter].Position.Z > 0.05f)
                        //&& primarySkeleton.Joints[JointType.ShoulderCenter].Position.Z - primarySkeleton.Joints[JointType.FootRight].Position.Z > 0.05f)
            {
                PressKeyCtrlDownArrow();
            }
                
            else
            {
                clearKeyStrokes();
            }

        }

        public void clearKeyStrokes() 
        {
            while(downKeyStrokes.Count > 0)
            {
                Console.WriteLine("Removed keystroke: " + downKeyStrokes[0]);
                InputSimulator.SimulateKeyUp(downKeyStrokes[0]);
                downKeyStrokes.Remove(downKeyStrokes[0]);
            }
        }

        public void PressKeyA()
        {
            InputSimulator.SimulateKeyPress(VirtualKeyCode.VK_A);
        }

        public void PressKeyUpArrow()
        {
            if(!InputSimulator.IsKeyDown(VirtualKeyCode.VK_W))
            {
                tb_Gestures.Text += (Environment.NewLine + "Step forward.");
                InputSimulator.SimulateKeyDown(VirtualKeyCode.VK_W);
                downKeyStrokes.Add(VirtualKeyCode.VK_W);
            }
        }

        public void PressKeyLeftArrow()
        {
            if(!InputSimulator.IsKeyDown(VirtualKeyCode.LEFT))
            {
                tb_Gestures.Text += (Environment.NewLine + "Turn left.");
                InputSimulator.SimulateKeyDown(VirtualKeyCode.LEFT);
                downKeyStrokes.Add(VirtualKeyCode.LEFT);
            }
        }

        public void PressKeyRightArrow()
        {
            if (!InputSimulator.IsKeyDown(VirtualKeyCode.RIGHT))
            {
                tb_Gestures.Text += (Environment.NewLine + "Turn right.");
                InputSimulator.SimulateKeyDown(VirtualKeyCode.RIGHT);
                downKeyStrokes.Add(VirtualKeyCode.RIGHT);
            }
        }

        public void PressKeyCtrlUpArrow()
        {
            if (!InputSimulator.IsKeyDown(VirtualKeyCode.CONTROL))
            {
                InputSimulator.SimulateKeyDown(VirtualKeyCode.CONTROL);
                downKeyStrokes.Add(VirtualKeyCode.CONTROL);
            }

            if (!InputSimulator.IsKeyDown(VirtualKeyCode.UP))
            {
                tb_Gestures.Text += (Environment.NewLine + "Look up.");
                InputSimulator.SimulateKeyDown(VirtualKeyCode.UP);
                downKeyStrokes.Add(VirtualKeyCode.UP);
            }
        }

        public void PressKeyCtrlDownArrow()
        {
            if (!InputSimulator.IsKeyDown(VirtualKeyCode.CONTROL))
            {
                InputSimulator.SimulateKeyDown(VirtualKeyCode.CONTROL);
                downKeyStrokes.Add(VirtualKeyCode.CONTROL);
            }

            if (!InputSimulator.IsKeyDown(VirtualKeyCode.DOWN))
            {
                tb_Gestures.Text += (Environment.NewLine + "Look down.");
                InputSimulator.SimulateKeyDown(VirtualKeyCode.DOWN);
                downKeyStrokes.Add(VirtualKeyCode.DOWN);
            }
        }

        private float jointDistance(Joint first, Joint second)
        {
            float dX = first.Position.X - second.Position.X;
            float dY = first.Position.Y - second.Position.Y;
            float dZ = first.Position.Z - second.Position.Z;

            return (float)Math.Sqrt((dX * dX) + (dY * dY) + (dZ * dZ));
        }
    }
}

//Code we may need later for reference

//var key = Key.A;                    // Key to send
//var target = Keyboard.FocusedElement;    // Target element
//var routedEvent = Keyboard.KeyDownEvent; // Event to send

//target.RaiseEvent(
//  new KeyEventArgs(
//    Keyboard.PrimaryDevice,
//    Keyboard.PrimaryDevice.ActiveSource,
//    0,
//    key) { RoutedEvent = routedEvent }
//);

//var eventArgs = new TextCompositionEventArgs(Keyboard.PrimaryDevice,
//                                            new TextComposition(InputManager.Current, Keyboard.FocusedElement, "A"));

//eventArgs.RoutedEvent = TextInputEvent;
//InputManager.Current.ProcessInput(eventArgs);
