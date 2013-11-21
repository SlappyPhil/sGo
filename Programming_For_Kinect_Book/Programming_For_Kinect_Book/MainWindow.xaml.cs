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

//Make sure you have the speech SDK installed
//go to add reference, browse, navigate to program files, micrsoft SDKs
//speech, assemblies and select speech.dll
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using System.IO;

namespace Programming_For_Kinect_Book
{  
    public partial class MainWindow : Window
    {
        ColorStreamManager colorManager = new ColorStreamManager();
        DepthStreamManager depthManager = new DepthStreamManager();
        SkeletonDisplayManager skeletonManager;
        //GestureDetector gestureDetector = new SwipeGestureDetector();
        ContextTracker contextTracker = new ContextTracker();
        Skeleton[] skeletons;
        Skeleton primarySkeleton;

        private InteractionStream _interactionStream;

        private UserInfo[] _userInfos; //the information about the interactive users

        Boolean leftHandGripped = false;
        Boolean rightHandGripped = false;

        private float mStartGripRightX;
        private float mStartGripRightY;
        private float mStartGripLeftX;
        private float mStartGripLeftY;

        private Dictionary<int, InteractionHandEventType> _lastLeftHandEvents = new Dictionary<int, InteractionHandEventType>();
        private Dictionary<int, InteractionHandEventType> _lastRightHandEvents = new Dictionary<int, InteractionHandEventType>();

        //the speech recognition engine (SRE)
        private SpeechRecognitionEngine speechRecognizer;

        bool lastFrameUnstable = true;
        List<VirtualKeyCode> downKeyStrokes;

        KinectSensor kinectSensor;
        public String userName = "The user ";
        String test = "";
        MediaElement sound_Detected = new MediaElement();
        MediaElement sound_NotDetected = new MediaElement();

        public float stableRightFootPosition, stableLeftFootPosition;

        public IntPtr MainWindowHandle { get; set; }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern long SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        bool doingGesture = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Left;
            this.Top = desktopWorkingArea.Top;

            me_SkeletonReady.LoadedBehavior = MediaState.Manual;
            me_SkeletonReady.UnloadedBehavior = MediaState.Manual;
            me_SkeletonReady.Source = new Uri(@"C:\Users\Jake\Documents\GitHub\sGo\Programming_For_Kinect_Book\Programming_For_Kinect_Book\Ding.wav", UriKind.Absolute);
            //me_SkeletonReady.Source = new Uri(@"C:\Users\Daniel\Documents\GitHub\sGo\Programming_For_Kinect_Book\Programming_For_Kinect_Book\Ding.wav", UriKind.Absolute);
            //me_SkeletonReady.Source = new Uri(@"C:\Users\Matt\Documents\GitHub\sGo\Programming_For_Kinect_Book\Programming_For_Kinect_Book\Ding.wav", UriKind.Absolute);


            me_SkeletonOut.LoadedBehavior = MediaState.Manual;
            me_SkeletonOut.UnloadedBehavior = MediaState.Manual;
            me_SkeletonOut.Source = new Uri(@"C:\Users\Jake\Documents\GitHub\sGo\Programming_For_Kinect_Book\Programming_For_Kinect_Book\Fail.wav", UriKind.Absolute);
            //me_SkeletonOut.Source = new Uri(@"C:\Users\Daniel\Documents\GitHub\sGo\Programming_For_Kinect_Book\Programming_For_Kinect_Book\Fail.wav", UriKind.Absolute);
            //me_SkeletonOut.Source = new Uri(@"C:\Users\Matt\Documents\GitHub\sGo\Programming_For_Kinect_Book\Programming_For_Kinect_Book\Fail.wav", UriKind.Absolute);

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

            mStartGripLeftX = -1;
            mStartGripLeftY = -1;

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

            speechRecognizer = CreateSpeechRecognizer();

            kinectSensor.Start();

            VoiceRecognitionStart();
        }
        
        //here is the fun part: create the speech recognizer
        private SpeechRecognitionEngine CreateSpeechRecognizer()
        {
            //set recognizer info
            RecognizerInfo ri = GetKinectRecognizer();
            //create instance of SRE
            SpeechRecognitionEngine sre;
            sre = new SpeechRecognitionEngine(ri.Id);

            //Now we need to add the words we want our program to recognise
            var grammar = new Choices();
            grammar.Add("testing");
            grammar.Add("Exit Street View");
            grammar.Add("Go to Paris");
            grammar.Add("Go to Gainesville");
            grammar.Add("Reset");

            //set culture - language, country/region
            var gb = new GrammarBuilder { Culture = ri.Culture };
            gb.Append(grammar);

            //set up the grammar builder
            var g = new Grammar(gb);
            sre.LoadGrammar(g);

            //Set events for recognizing, hypothesising and rejecting speech
            sre.SpeechRecognized += SreSpeechRecognized;
            sre.SpeechHypothesized += SreSpeechHypothesized;
            sre.SpeechRecognitionRejected += SreSpeechRecognitionRejected;
            return sre;
        }

        //Start streaming audio for voice recognition
        private void VoiceRecognitionStart()
        {
            //set sensor audio source to variable
            var audioSource = kinectSensor.AudioSource;
            //Set the beam angle mode - the direction the audio beam is pointing
            //we want it to be set to adaptive
            audioSource.BeamAngleMode = BeamAngleMode.Adaptive;
            //start the audiosource 
            var kinectStream = audioSource.Start();
            //configure incoming audio stream
            speechRecognizer.SetInputToAudioStream(kinectStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
            //make sure the recognizer does not stop after completing     
            speechRecognizer.RecognizeAsync(RecognizeMode.Multiple);
            //reduce background and ambient noise for better accuracy
            kinectSensor.AudioSource.EchoCancellationMode = EchoCancellationMode.None;
            kinectSensor.AudioSource.AutomaticGainControlEnabled = false;
        }

        //Get the speech recognizer (SR)
        private static RecognizerInfo GetKinectRecognizer()
        {
            Func<RecognizerInfo, bool> matchingFunc = r =>
            {
                string value;
                r.AdditionalInfo.TryGetValue("Kinect", out value);
                return "True".Equals(value, StringComparison.InvariantCultureIgnoreCase) && "en-US".Equals(r.Culture.Name, StringComparison.InvariantCultureIgnoreCase);
            };
            return SpeechRecognitionEngine.InstalledRecognizers().Where(matchingFunc).FirstOrDefault();
        }

        //if speech is rejected
        private void RejectSpeech(RecognitionResult result)
        {
            tb_Gestures.Text += Environment.NewLine + "I didn't catch that...";
        }

        private void SreSpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            RejectSpeech(e.Result);
        }

        //hypothesized result
        private void SreSpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            tb_Gestures.Text += Environment.NewLine + "Hypothesized: " + e.Result.Text + " " + e.Result.Confidence;
        }

        //Speech is recognised
        private void SreSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            //Very important! - change this value to adjust accuracy - the higher the value
            //the more accurate it will have to be, lower it if it is not recognizing you
            if (e.Result.Confidence < .7)
            {
                RejectSpeech(e.Result);
            }
            //and finally, here we set what we want to happen when the SRE recognizes a word
            else
            {
                switch (e.Result.Text.ToUpperInvariant())
                {
                    case "TESTING":
                        tb_Gestures.Text += Environment.NewLine + "You said TESTING.";
                        break;
                    case "EXIT STREET VIEW":
                        tb_Gestures.Text += Environment.NewLine + "You said EXIT STREET VIEW.";
                        PressKeyEscape();
                        break;
                    case "GO TO PARIS":
                        tb_Gestures.Text += Environment.NewLine + "You said GO TO PARIS.";
                        GoToCityEvent("Paris");
                        break;
                    case "GO TO GAINESVILLE":
                        tb_Gestures.Text += Environment.NewLine + "You said GO TO GAINESVILLE.";
                        GoToCityEvent("Gainesville");
                        break;

                    case "RESET":
                        tb_Gestures.Text += Environment.NewLine + "You said RESET";
                        resetSkeleton();
                        break;

                    default:
                        break;
                }
            }
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

                        // This is set up to ONLY detect the grip of one hand. In other words,
                        // while one hand is gripped, we don't care if the other hand becomes gripped.
                        // This is temporary, as we will need to detect two grips for zoom.
                        if (hand.HandType == InteractionHandType.Left)
                            if (lastHandEvent == InteractionHandEventType.Grip)
                            {
                                // only set grip to true if it will be the first hand gripped
                                if (!(rightHandGripped || leftHandGripped))
                                    toggleLeftGrip(true);
                            }
                            else
                            {
                                toggleLeftGrip(false);
                            }

                        else if (hand.HandType == InteractionHandType.Right)
                            if (lastHandEvent == InteractionHandEventType.Grip)
                            {
                                // only set grip to true if it will be the first hand gripped
                                if (!(leftHandGripped || rightHandGripped))
                                    toggleRightGrip(true);
                            }
                            else
                            {
                                toggleRightGrip(false);
                            }

                        //dump.AppendLine();
                        //dump.AppendLine("    HandType: " + hand.HandType);
                        //dump.AppendLine("    HandEventType: " + hand.HandEventType);
                        //dump.AppendLine("    LastHandEventType: " + lastHandEvent);
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
                tb_Gestures.Text = "No user detected.";
        }

        private void toggleRightGrip(bool grip)
        {
            rightHandGripped = grip;

            if (rightHandGripped)
            {
                moveMouseToStreetView();

                // This sets the initial position of the hand when a grab is detected. This
                // may not be necessary... but it works
                mStartGripRightX = primarySkeleton.Joints[JointType.HandRight].Position.X;
                mStartGripRightY = primarySkeleton.Joints[JointType.HandRight].Position.Y;
            }
        }
        private void toggleLeftGrip(bool grip)
        {
            leftHandGripped = grip;

            if (leftHandGripped)
            {
                moveMouseToCenter();

                // This sets the initial position of the hand when a grab is detected. This
                // may not be necessary... but it works
                mStartGripLeftX = primarySkeleton.Joints[JointType.HandLeft].Position.X;
                mStartGripLeftY = primarySkeleton.Joints[JointType.HandLeft].Position.Y;
            }
        }

        private void moveMouseToCenter()
        {
            int centerX = (int)(System.Windows.SystemParameters.PrimaryScreenWidth * 0.65); //1536
            int centerY = (int)(System.Windows.SystemParameters.PrimaryScreenHeight * 0.5); //864

            Console.WriteLine("Width is: " + System.Windows.SystemParameters.PrimaryScreenWidth);
            Console.WriteLine("Height is: " + System.Windows.SystemParameters.PrimaryScreenHeight);

            MouseInterop.ControlMouseAbsolute(centerX, centerY);
        }

        private void moveMouseToStreetView()
        {
            int centerX = (int)(System.Windows.SystemParameters.PrimaryScreenWidth * 0.97); //1536
            int centerY = (int)(System.Windows.SystemParameters.PrimaryScreenHeight * 0.24); //864

            Console.WriteLine("Width is: " + System.Windows.SystemParameters.PrimaryScreenWidth);
            Console.WriteLine("Height is: " + System.Windows.SystemParameters.PrimaryScreenHeight);

            MouseInterop.ControlMouseAbsolute(centerX, centerY);
        }

        void kinectSensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {       
            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame == null)
                {
                    return;
                }

                frame.GetSkeletons(ref skeletons);

                if (skeletons.All(s => s.TrackingState == SkeletonTrackingState.NotTracked))
                {
                    skeletonManager.EraseCanvas();
                    return;
                }

                else if (primarySkeleton == null)
                {
                    Console.WriteLine("getting new skeleton");
                    primarySkeleton = getPrimarySkeleton(skeletons);
                }

                else
                {
                    if(primarySkeletonLost(skeletons, primarySkeleton))
                    {
                        //remove previous primary skeleton from array and sket primarySkeleton to null to find new primary
                        //tb_Gestures.Text += Environment.NewLine + "Primary skeleton lost";
                        
                        tb_Gestures.ScrollToEnd();
                        Console.WriteLine("primary skeleton removed with id: " + primarySkeleton.TrackingId);

                        primarySkeleton = null;

                        skeletonManager.EraseCanvas();

                        clearKeyStrokes();
                    }

                    else if (HasClippedEdges(primarySkeleton)) 
                    {
                        if (!lastFrameUnstable)
                        {
                            tb_Gestures.Text += Environment.NewLine + userName + "is lost!";
                            me_SkeletonOut.Play();
                        }
                        skeletonManager.DrawUnstable(primarySkeleton);
                        clearKeyStrokes();
                        lastFrameUnstable = true;
                        //tb_Gestures.Text += Environment.NewLine + "Primary skeleton partially out of view";
                        //tb_Gestures.ScrollToEnd();

                        //needsToBeStabalized = true;
                        //Console.WriteLine("Skeleton needs to be stabalized");
                    }

                    else
	                {
                        if (lastFrameUnstable)
                        {
                            tb_Gestures.Text += Environment.NewLine + userName + "is ready to explore!";
                            me_SkeletonReady.Play();
                        }
                        skeletonManager.DrawStable(primarySkeleton);
                        doGestureDetection();
                        lastFrameUnstable = false;

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

                            }

                            //tb_Debug.Text = dump.ToString();

                        }
                        catch (InvalidOperationException)
                        {
                            // SkeletonFrame functions may throw when the sensor gets
                            // into a bad state.  Ignore the frame in that case.
                        }

	                }
                }                 
            }
        }

        public void resetSkeleton()
        {
            primarySkeleton = null;
            clearKeyStrokes();
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

                    //tb_Gestures.Text += Environment.NewLine + "Skeleton found - id: " + skeleton.TrackingId;
                    //tb_Gestures.ScrollToEnd();

                    if (skeletonIsReady(skeleton))
                    {
                        skeletonToReturn = skeleton;
                        //tb_Gestures.Text += Environment.NewLine + "Primary skeleton ready - id: " + skeletonToReturn.TrackingId;
                        tb_Gestures.ScrollToEnd();
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
                    && contextTracker.IsNotClipped(skeleton) && !isDoingGesture())
                return true;
            else
                return false;

        }

        public void doGestureDetection()
        {
            //gestureDetector.Add(primarySkeleton.Joints[JointType.HandRight].Position, kinectSensor);

            //Step forward with one foot
            if (primarySkeleton.Joints[JointType.AnkleLeft].Position.Z - primarySkeleton.Joints[JointType.AnkleRight].Position.Z > 0.4f ||
                        primarySkeleton.Joints[JointType.AnkleRight].Position.Z - primarySkeleton.Joints[JointType.AnkleLeft].Position.Z > 0.4f)
            {
                test = "Fast walk";
                //clearSingleKey(VirtualKeyCode.VK_W); //THESE NEED TO BE HERE BUT ARENT WORKNG RIGHT
                clearKeyStrokes();
                PressKeyEqual();

            }
            else if ((primarySkeleton.Joints[JointType.AnkleLeft].Position.Z - primarySkeleton.Joints[JointType.AnkleRight].Position.Z > 0.2f && primarySkeleton.Joints[JointType.AnkleLeft].Position.Z - primarySkeleton.Joints[JointType.AnkleRight].Position.Z < 0.4f) ||
                    (primarySkeleton.Joints[JointType.AnkleRight].Position.Z - primarySkeleton.Joints[JointType.AnkleLeft].Position.Z > 0.2f && primarySkeleton.Joints[JointType.AnkleRight].Position.Z - primarySkeleton.Joints[JointType.AnkleLeft].Position.Z < 0.4f))
            {
                test = "Slow walk";
                //clearSingleKey(VirtualKeyCode.PRIOR); //THESE NEED TO BE HERE BUT ARENT WORKNG RIGHT
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
            else if (primarySkeleton.Joints[JointType.ShoulderCenter].Position.Z - primarySkeleton.Joints[JointType.HipCenter].Position.Z > 0.08f)
            {
                PressKeyCtrlUpArrow();
                
            }
            //Look down
            else if (primarySkeleton.Joints[JointType.HipCenter].Position.Z - primarySkeleton.Joints[JointType.ShoulderCenter].Position.Z > 0.08f)
            {
                PressKeyCtrlDownArrow();
            }

            // We check to see if one hand is gripped and not the other for panning.
            // For now, this is always the case.
            else if (leftHandGripped && !rightHandGripped)
            {
                float leftHandX = mStartGripLeftX;
                float leftHandY = mStartGripLeftY;

                mStartGripLeftX = primarySkeleton.Joints[JointType.HandLeft].Position.X;
                mStartGripLeftY = primarySkeleton.Joints[JointType.HandLeft].Position.Y;

                int dx = (int)((leftHandX - mStartGripLeftX) * 1000);
                int dy = (int)((leftHandY - mStartGripLeftY) * 1000);

                MouseInterop.ControlMouse(-dx, dy, true);
                clearKeyStrokes();
            }

            else if (rightHandGripped && !leftHandGripped)
            {
                float rightHandX = mStartGripRightX;
                float rightHandY = mStartGripRightY;

                mStartGripRightX = primarySkeleton.Joints[JointType.HandRight].Position.X;
                mStartGripRightY = primarySkeleton.Joints[JointType.HandRight].Position.Y;

                int dx = (int)((rightHandX - mStartGripRightX) * 1000);
                int dy = (int)((rightHandY - mStartGripRightY) * 1000);

                MouseInterop.ControlMouse(-dx, dy, true);
                clearKeyStrokes();
            }

            //else  if (!(rightHandGripped || leftHandGripped))
            //{
                //MouseInterop.ControlMouse(0, 0, false);
            //}

            else
            {
                clearKeyStrokes();
                MouseInterop.ControlMouse(0, 0, false);

            }

        }

        public bool isDoingGesture()
        {
            foreach (Skeleton skeleton in skeletons)
            {
                if (skeleton.TrackingId != 0)
                {
                    //Step forward with one foot
                    if (skeleton.Joints[JointType.AnkleLeft].Position.Z - skeleton.Joints[JointType.AnkleRight].Position.Z > 0.4f ||
                                skeleton.Joints[JointType.AnkleRight].Position.Z - skeleton.Joints[JointType.AnkleLeft].Position.Z > 0.4f)
                    {
                        return true;

                    }
                    else if ((skeleton.Joints[JointType.AnkleLeft].Position.Z - skeleton.Joints[JointType.AnkleRight].Position.Z > 0.2f && skeleton.Joints[JointType.AnkleLeft].Position.Z - skeleton.Joints[JointType.AnkleRight].Position.Z < 0.4f) ||
                            (skeleton.Joints[JointType.AnkleRight].Position.Z - skeleton.Joints[JointType.AnkleLeft].Position.Z > 0.2f && skeleton.Joints[JointType.AnkleRight].Position.Z - skeleton.Joints[JointType.AnkleLeft].Position.Z < 0.4f))
                    {
                        return true;
                    }

                    //Turn shoulders left
                    else if (skeleton.Joints[JointType.ShoulderLeft].Position.Z - skeleton.Joints[JointType.ShoulderRight].Position.Z > 0.1f)
                    {
                        return true;
                    }

                    //Turn shoulders right
                    else if (skeleton.Joints[JointType.ShoulderRight].Position.Z - skeleton.Joints[JointType.ShoulderLeft].Position.Z > 0.1f)
                    {
                        return true;
                    }

                    //Look up
                    else if (skeleton.Joints[JointType.ShoulderCenter].Position.Z - skeleton.Joints[JointType.HipCenter].Position.Z > 0.1f)
                    {
                        return true;
                    }
                    //Look down
                    else if (skeleton.Joints[JointType.HipCenter].Position.Z - skeleton.Joints[JointType.ShoulderCenter].Position.Z > 0.05f)
                    {
                        return true;
                    }

                    else
                    {
                        return false;
                    }
                }
            }

            return false;

        }

        public void clearKeyStrokes() 
        {
            while(downKeyStrokes.Count > 0)
            {
                InputSimulator.SimulateKeyUp(downKeyStrokes[0]);
                downKeyStrokes.Remove(downKeyStrokes[0]);
            }
            tb_Gestures.Text += Environment.NewLine + "Clear key strokes";

        }


        public void GoToCityEvent(String city)
        {
            tb_Gestures.Text += (Environment.NewLine + "Going to city: " + city);
            tb_Gestures.ScrollToEnd();
            InputSimulator.SimulateKeyPress(VirtualKeyCode.TAB);
            InputSimulator.SimulateTextEntry(city);
            InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.SHIFT);
            InputSimulator.SimulateKeyPress(VirtualKeyCode.TAB);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.SHIFT);
        }

        public void clearSingleKey(VirtualKeyCode key)
        {
            if(InputSimulator.IsKeyDown(key))
            {
                InputSimulator.SimulateKeyUp(key);
                downKeyStrokes.Remove(key);
                tb_Gestures.Text += Environment.NewLine + "Current: " + test + " Clearing: " + key.ToString();
            }
        }

        //Used for exiting street view
        public void PressKeyEscape()
        {
            tb_Gestures.Text += (Environment.NewLine + "Exiting street view.");
            tb_Gestures.ScrollToEnd();
            InputSimulator.SimulateKeyPress(VirtualKeyCode.ESCAPE);
        }

        //Used for walking forward faster
        public void PressKeyEqual()
        {
            if (!InputSimulator.IsKeyDown(VirtualKeyCode.PRIOR))
            {
                tb_Gestures.Text += (Environment.NewLine + "Step forward fast.");
                tb_Gestures.ScrollToEnd();
                InputSimulator.SimulateKeyUp(VirtualKeyCode.VK_W);
                InputSimulator.SimulateKeyDown(VirtualKeyCode.PRIOR);
                downKeyStrokes.Add(VirtualKeyCode.PRIOR);
            }
        }

        //Used for walking forward
        public void PressKeyUpArrow()
        {
            if(!InputSimulator.IsKeyDown(VirtualKeyCode.VK_W))
            {
                tb_Gestures.Text += (Environment.NewLine + "Step forward normal.");
                tb_Gestures.ScrollToEnd();
                InputSimulator.SimulateKeyUp(VirtualKeyCode.PRIOR);
                InputSimulator.SimulateKeyDown(VirtualKeyCode.VK_W);
                downKeyStrokes.Add(VirtualKeyCode.VK_W);
            }
        }

        //Used for walking backwards (not implemented)
        public void PressKeyDownArrow()
        {
            if (!InputSimulator.IsKeyDown(VirtualKeyCode.VK_S))
            {
                tb_Gestures.Text += (Environment.NewLine + "Step backward.");
                tb_Gestures.ScrollToEnd();
                InputSimulator.SimulateKeyDown(VirtualKeyCode.VK_S);
                downKeyStrokes.Add(VirtualKeyCode.VK_S);
            }
        }

        //Used for looking left
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