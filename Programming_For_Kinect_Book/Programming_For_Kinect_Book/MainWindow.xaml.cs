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

using Microsoft.Kinect.Toolkit.Interaction;

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
        //bool needsToBeStabalized = false; 

        private InteractionStream _interactionStream;

        private UserInfo[] _userInfos; //the information about the interactive users

        Boolean leftHandGripped = false;
        Boolean rightHandGripped = false;

        private Dictionary<int, InteractionHandEventType> _lastLeftHandEvents = new Dictionary<int, InteractionHandEventType>();
        private Dictionary<int, InteractionHandEventType> _lastRightHandEvents = new Dictionary<int, InteractionHandEventType>();

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
            kinectSensor.ColorFrameReady += kinectSensor_ColorFrameReady;

            kinectSensor.DepthStream.Enable();
            kinectSensor.DepthFrameReady += kinectSensor_DepthFrameReady;

            _userInfos = new UserInfo[InteractionFrame.UserInfoArrayLength];

            kinectSensor.SkeletonStream.Enable();
            skeletonManager = new SkeletonDisplayManager(kinectSensor, skeletonCanvas);
            kinectSensor.SkeletonFrameReady += kinectSensor_SkeletonFrameReady;

            _interactionStream = new InteractionStream(kinectSensor, new DummyInteractionClient());
            _interactionStream.InteractionFrameReady += InteractionStreamOnInteractionFrameReady;

            downKeyStrokes = new List<VirtualKeyCode>();

            //gestureDetector.DisplayCanvas = gestureCanvas;

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

        void kinectSensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs depthImageFrameReadyEventArgs)
        {
            using (DepthImageFrame depthFrame = depthImageFrameReadyEventArgs.OpenDepthImageFrame())
            {
                if (depthFrame == null)
                    return;

                try
                {
                    _interactionStream.ProcessDepth(depthFrame.GetRawPixelData(), depthFrame.Timestamp);
                }
                catch (InvalidOperationException)
                {
                    // DepthFrame functions may throw when the sensor gets
                    // into a bad state.  Ignore the frame in that case.
                }
            }
        }

        private void InteractionStreamOnInteractionFrameReady(object sender, InteractionFrameReadyEventArgs args)
        {
            using (var iaf = args.OpenInteractionFrame()) //dispose as soon as possible
            {
                if (iaf == null)
                    return;

                iaf.CopyInteractionDataTo(_userInfos);
            }

            StringBuilder dump = new StringBuilder();

            var hasUser = false;
            foreach (var userInfo in _userInfos)
            {
                var userID = userInfo.SkeletonTrackingId;
                if (userID == 0)
                    continue;

                hasUser = true;
                dump.AppendLine("User ID = " + userID);
                dump.AppendLine("  Hands: ");
                var hands = userInfo.HandPointers;
                if (hands.Count == 0)
                    dump.AppendLine("    No hands");
                else
                {
                    foreach (var hand in hands)
                    {
                        var lastHandEvents = hand.HandType == InteractionHandType.Left
                                                 ? _lastLeftHandEvents
                                                 : _lastRightHandEvents;

                        if (hand.HandEventType != InteractionHandEventType.None)
                            lastHandEvents[userID] = hand.HandEventType;

                        var lastHandEvent = lastHandEvents.ContainsKey(userID)
                                                ? lastHandEvents[userID]
                                                : InteractionHandEventType.None;

                        if (hand.HandType == InteractionHandType.Left)
                            if (lastHandEvent == InteractionHandEventType.Grip)
                                leftHandGripped = true;
                            else
                                leftHandGripped = false;

                        else if (hand.HandType == InteractionHandType.Right)
                            if (lastHandEvent == InteractionHandEventType.Grip)
                                rightHandGripped = true;
                            else
                                rightHandGripped = false;

                        //dump.AppendLine();
                        //dump.AppendLine("    HandType: " + hand.HandType);
                        //dump.AppendLine("    HandEventType: " + hand.HandEventType);
                        dump.AppendLine("    LastHandEventType: " + lastHandEvent);
                        //dump.AppendLine("    IsActive: " + hand.IsActive);
                        //dump.AppendLine("    IsPrimaryForUser: " + hand.IsPrimaryForUser);
                        //dump.AppendLine("    IsInteractive: " + hand.IsInteractive);
                        //dump.AppendLine("    PressExtent: " + hand.PressExtent.ToString("N3"));
                        //dump.AppendLine("    IsPressed: " + hand.IsPressed);
                        //dump.AppendLine("    IsTracked: " + hand.IsTracked);
                        //dump.AppendLine("    X: " + hand.X.ToString("N3"));
                        //dump.AppendLine("    Y: " + hand.Y.ToString("N3"));
                        //dump.AppendLine("    RawX: " + hand.RawX.ToString("N3"));
                        //dump.AppendLine("    RawY: " + hand.RawY.ToString("N3"));
                        //dump.AppendLine("    RawZ: " + hand.RawZ.ToString("N3"));
                    }
                }

                //tb.Text = dump.ToString();
            }

            if (!hasUser)
                tb_Debug.Text = "No user detected.";
        }

        void kinectSensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {       
            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame == null)
                    return;

                frame.GetSkeletons(ref skeletons);
                if (skeletons.All(s => s.TrackingState == SkeletonTrackingState.NotTracked))
                {
                    skeletonManager.EraseCanvas();
                    return;
                }

                if (primarySkeleton == null)
                {
                    primarySkeleton = getPrimarySkeleton(skeletons);
                }

                else
                {
                    if(primarySkeletonLost(skeletons, primarySkeleton))
                    {
                        //remove previous primary skeleton from array and sket primarySkeleton to null to find new primary
                        tb_Debug.Text += Environment.NewLine + "Primary skeleton lost";
                        tb_Debug.ScrollToEnd();
                        Console.WriteLine("primary skeleton removed with id: " + primarySkeleton.TrackingId);

                        primarySkeleton = null;

                        skeletonManager.EraseCanvas();

                        clearKeyStrokes();
                    }

                    else if (HasClippedEdges(primarySkeleton)) 
                    {
                        skeletonManager.DrawUnstable(primarySkeleton);
                        clearKeyStrokes();
                        //tb_Debug.Text += Environment.NewLine + "Primary skeleton partially out of view";
                        //tb_Debug.ScrollToEnd();

                        //needsToBeStabalized = true;
                        //Console.WriteLine("Skeleton needs to be stabalized");
                    }

                    else
	                {
                        skeletonManager.DrawStable(primarySkeleton);
                        doGestureDetection();

                        try
                        {
                            frame.CopySkeletonDataTo(skeletons);
                            var accelerometerReading = kinectSensor.AccelerometerGetCurrentReading();
                            _interactionStream.ProcessSkeleton(skeletons, accelerometerReading, frame.Timestamp);

                            StringBuilder dump = new StringBuilder();

                            if (primarySkeleton.TrackingId != 0)
                            {
                                dump.AppendLine("User ID = " + skeletons[0].TrackingId);
                                dump.AppendLine("    LeftHandEventType: " + leftHandGripped);
                                dump.AppendLine("    rightHandEventType: " + rightHandGripped);
                                Console.WriteLine("    rightHandEventType: " + rightHandGripped);
                                Console.WriteLine("    LeftHandEventType: " + leftHandGripped);

                            }

                            tb_Debug.Text = dump.ToString();

                        }
                        catch (InvalidOperationException)
                        {
                            // SkeletonFrame functions may throw when the sensor gets
                            // into a bad state.  Ignore the frame in that case.
                        }

                        /*if (needsToBeStabalized)
                        {
                            if (skeletonIsReady(primarySkeleton))
                            {
                                Console.WriteLine("Skeleton ready after red");
                                skeletonManager.DrawStable(primarySkeleton);
                                doGestureDetection();
                                needsToBeStabalized = false;
                            }
                            else
                            {
                                skeletonManager.DrawUnstable(primarySkeleton);
                            }
                        }
                        else
                        {
                            skeletonManager.DrawStable(primarySkeleton);
                        }
                        */

	                }
                }                 
            }
        }

        public Boolean primarySkeletonLost(Skeleton[] skeletons, Skeleton skeleton)
        {
            if(skeleton.TrackingId != 0)
                foreach (Skeleton thisSkeleton in skeletons)
                {
                    if (thisSkeleton.TrackingId == skeleton.TrackingId)
                        return false;
                }
            return true;
        }
        public Skeleton getPrimarySkeleton(Skeleton[] skeletons)
        {
            Skeleton skeletonToReturn = null;

            foreach (Skeleton skeleton in skeletons)
            {
                if (skeleton.TrackingId != 0)
                {
                    //contextTracker.Add(skeleton, JointType.HipCenter);
                    contextTracker.Add(skeleton.Position.ToVector3(), skeleton.TrackingId);

                    //tb_Debug.Text += Environment.NewLine + "Skeleton found - id: " + skeleton.TrackingId;
                    //tb_Debug.ScrollToEnd();

                    if (skeletonIsReady(skeleton))
                    {
                        skeletonToReturn = skeleton;
                        tb_Debug.Text += Environment.NewLine + "Primary skeleton ready - id: " + skeletonToReturn.TrackingId;
                        tb_Debug.ScrollToEnd();
                        Console.WriteLine("primary skeleton identified with id: " + skeletonToReturn.TrackingId);
                    }
                    else
                    {
                        skeletonManager.DrawUnstable(skeleton);
                    }
                }
            }

            return skeletonToReturn;
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
            if (contextTracker.IsStableRelativeToAverageSpeed(skeleton.TrackingId) && contextTracker.IsShouldersTowardsSensor(skeleton)
                    && contextTracker.IsNotClipped(skeleton))
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
            //gestureDetector.Add(primarySkeleton.Joints[JointType.HandRight].Position, kinectSensor);

            //if (primarySkeleton.Joints[JointType.HandRight].Position.Y > primarySkeleton.Joints[JointType.Head].Position.Y
            //            && primarySkeleton.Joints[JointType.HandLeft].Position.Y > primarySkeleton.Joints[JointType.Head].Position.Y)
            //{
                
            //}

            //if (primarySkeleton.Joints[JointType.HipCenter].Position.Z - primarySkeleton.Joints[JointType.HandLeft].Position.Z > 0.3f
            //            && primarySkeleton.Joints[JointType.HipCenter].Position.Z - primarySkeleton.Joints[JointType.HandRight].Position.Z > 0.3f)
            //{
                
            //    PressKeyLeftArrow();
            //}

            //Step forward with one foot
            if (primarySkeleton.Joints[JointType.AnkleLeft].Position.Z - primarySkeleton.Joints[JointType.AnkleRight].Position.Z > 0.2f ||
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
            else if (primarySkeleton.Joints[JointType.ShoulderCenter].Position.Z - primarySkeleton.Joints[JointType.HipCenter].Position.Z > 0.1f)
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
            //if (downKeyStrokes.Count > 0)
            //{
            //    tb_Debug.Text += Environment.NewLine + "Key strokes cleared";
            //    tb_Debug.ScrollToEnd();
            //}
            while(downKeyStrokes.Count > 0)
            {
                //Console.WriteLine("Removed keystroke: " + downKeyStrokes[0]);
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
                tb_Gestures.ScrollToEnd();
                InputSimulator.SimulateKeyDown(VirtualKeyCode.VK_W);
                downKeyStrokes.Add(VirtualKeyCode.VK_W);
            }
        }

        public void PressKeyLeftArrow()
        {
            if(!InputSimulator.IsKeyDown(VirtualKeyCode.LEFT))
            {
                tb_Gestures.Text += (Environment.NewLine + "Turn left.");
                tb_Gestures.ScrollToEnd();
                InputSimulator.SimulateKeyDown(VirtualKeyCode.LEFT);
                downKeyStrokes.Add(VirtualKeyCode.LEFT);
            }
        }

        public void PressKeyRightArrow()
        {
            if (!InputSimulator.IsKeyDown(VirtualKeyCode.RIGHT))
            {
                tb_Gestures.Text += (Environment.NewLine + "Turn right.");
                tb_Gestures.ScrollToEnd();
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
                tb_Gestures.ScrollToEnd();
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
                tb_Gestures.ScrollToEnd();
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
