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
        Skeleton[] skeletons;

        public IntPtr MainWindowHandle { get; set; }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern long SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        public MainWindow()
        {
            InitializeComponent();
        }

        KinectSensor kinectSensor;
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

                foreach (Skeleton skeleton in skeletons)
                {

                    if (skeleton.TrackingId != 0)
                    {

                        gestureDetector.Add(skeleton.Joints[JointType.HandRight].Position, kinectSensor);
                        //gestureDetector.Add(skeleton.Joints[JointType.HandLeft].Position, kinectSensor);

                        if (skeleton.Joints[JointType.HandRight].Position.Y > skeleton.Joints[JointType.Head].Position.Y && skeleton.Joints[JointType.HandLeft].Position.Y > skeleton.Joints[JointType.Head].Position.Y)
                        {
                            //Console.WriteLine("GESTURE DETECTED! Both hands above head");
                        }

                        else if (skeleton.Joints[JointType.HipCenter].Position.Z - skeleton.Joints[JointType.HandLeft].Position.Z > 0.3f
                                    && skeleton.Joints[JointType.HipCenter].Position.Z - skeleton.Joints[JointType.HandRight].Position.Z > 0.3f)
                        {
                            Console.WriteLine("Left and Right hand in front!!!");
                        }

                        else if (skeleton.Joints[JointType.AnkleLeft].Position.Z - skeleton.Joints[JointType.AnkleRight].Position.Z > 0.2f ||
                                    skeleton.Joints[JointType.AnkleRight].Position.Z - skeleton.Joints[JointType.AnkleLeft].Position.Z > 0.2f)
                        {
                            Console.WriteLine("Stepped forward!");

                            PressKeyA();

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

                            
                        }
                        else
                        {
                            //Console.WriteLine("Gesture not detected Right hand position: " + skeleton.TrackingId);
                        }
                    }
                }
                
                skeletonManager.Draw(skeletons);
            }
        }

        public void PressKeyA()
        {
            InputSimulator.SimulateKeyPress(VirtualKeyCode.VK_A);
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