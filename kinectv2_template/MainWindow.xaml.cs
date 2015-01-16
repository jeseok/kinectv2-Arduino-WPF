using Microsoft.Kinect;
using Microsoft.Kinect.Face;
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
using System.IO.Ports;

namespace kinectv2_template
{
    public partial class MainWindow : Window
    {
        KinectSensor _kinect = null;

        // stuff for RGB color input from Kinect
        ColorFrameReader _colorReader = null;
      
        // Stuff for Skeleton Tracking
        int bodyCount;
        Body[] _bodies = null;
        BodyFrameReader _bodyReader = null;
        bool goTrack;
        bool goFaceTrack;
        // Stuff for Faces
        FaceFrameSource[] _faceSources = null;
        FaceFrameReader[] _faceReaders = null;
        FaceFrameResult[] _faceResults = null;

        // Serial for Arduino testing
        SerialPort myPort = null;
        bool[] mouthReading;
        
        bool[] mouthState;
        bool[] lastMouthState;
        long[] lastDebounceTime;

        public MainWindow()
        {
            InitializeComponent(); 
            InitKinect();
        }

        #region INITs

        private void InitKinect()
        {
            _kinect = KinectSensor.GetDefault();

            if (_kinect != null)
            {
                _kinect.Open();

                InitCamera();
                InitBody();
               
            }



            // close Kinect when closing app
            Closing += OnClosing;
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_kinect != null) _kinect.Close();

        }
        private void InitCamera()
        {
            if (_kinect == null) return;

            // get frameDescription for the color output
            FrameDescription desc = _kinect.ColorFrameSource.FrameDescription;

            // get the frameReader
            _colorReader = _kinect.ColorFrameSource.OpenReader();
            // Hook-up event
            _colorReader.FrameArrived += OnColorFrameArrived;
        }


        private void InitBody()
        {
            if (_kinect == null) return;
            bodyCount = _kinect.BodyFrameSource.BodyCount;
            // alloc body array
            _bodies = new Body[bodyCount];

            // Open Body reader
            _bodyReader = _kinect.BodyFrameSource.OpenReader();

            // hook-up event
            _bodyReader.FrameArrived += OnBodyFrameArrived;

            InitFaceTracking(bodyCount);
            initSerial(bodyCount);
            //goTrack = true;
        }

        private void StopBody()
        {
            if (_kinect == null) return;
            // stop drawing bodies
            goTrack = false;

            //clear canvas
            SkeletonCanvas.Children.Clear();

        }

        private void InitFaceTracking(int bcount)
        {
            FaceFrameFeatures faceFrameFeatures =
                 FaceFrameFeatures.BoundingBoxInColorSpace
                 | FaceFrameFeatures.PointsInColorSpace
                 | FaceFrameFeatures.RotationOrientation
                 | FaceFrameFeatures.FaceEngagement
                 | FaceFrameFeatures.Glasses
                 | FaceFrameFeatures.Happy
                 | FaceFrameFeatures.LeftEyeClosed
                 | FaceFrameFeatures.RightEyeClosed
                 | FaceFrameFeatures.LookingAway
                 | FaceFrameFeatures.MouthMoved
                 | FaceFrameFeatures.MouthOpen;
            // 2) Initialize the face source with the desired features

            _faceSources = new FaceFrameSource[bcount];
            _faceReaders = new FaceFrameReader[bcount];

            for (int i = 0; i < bcount; i++)
            {
                _faceSources[i] = new FaceFrameSource(_kinect, 0, faceFrameFeatures);
                _faceReaders[i] = _faceSources[i].OpenReader();
                _faceReaders[i].FrameArrived += FaceReader_FrameArrived;
            }

            _faceResults = new FaceFrameResult[bcount];
        }

        private void initSerial(int bcount) {

             myPort = new SerialPort("COM3", 9600);
             try{
                 
                 myPort.Open();
             }
             catch (System.IO.IOException ex)
             {
                 //MessageBox.Show("Error: " + ex.ToString(), "ERROR");
             }


             mouthReading = new bool[bcount];

             mouthState = new bool[bcount];
             lastMouthState = new bool[bcount];

             lastDebounceTime = new long[bcount];

 
        }

        #endregion INITs

        #region KinectEvents

        private void OnColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // get the reference to the color frame
            ColorFrameReference colorRef = e.FrameReference;

            if (colorRef == null) return;

            ColorFrame frame = colorRef.AcquireFrame();

            if (frame == null) return;

            using (frame)
            {
                // drawing RGB input using Extensions
                CameraImage.Source = frame.ToBitmap();
            }
        }

        private void OnBodyFrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {

            // get the body reference
            BodyFrameReference bodyRef = e.FrameReference;

            if (bodyRef == null) return;

            // get body Frame
            BodyFrame frame = bodyRef.AcquireFrame();

            using (frame)
            {

                if (frame == null) return;
                // get body data
                frame.GetAndRefreshBodyData(_bodies);

                // Clear skeleton canvas
                SkeletonCanvas.Children.Clear();

                // loop all bodies
                for (int i = 0; i < _bodies.Length; i++)
                {
                    // only process tracked bodies
                    if (_bodies[i].IsTracked)
                    {
                        if (goTrack) SkeletonCanvas.DrawSkeleton(_bodies[i], _kinect.CoordinateMapper, 2);

                    }

                     if (_faceSources[i].IsTrackingIdValid)
                     {
                         // check if we have valid face frame results
                         if (_faceResults[i] != null)
                         {
                             // draw face frame results
                             if (goFaceTrack)
                             {
                                 SkeletonCanvas.DrawFace(i, _faceResults[i], 2);
                                 faceToArduino(i, _faceResults[i]);
                             }

                         }
                     }
                     else
                     {
                         // check if the corresponding body is tracked 
                         if (_bodies[i].IsTracked)
                         {
                             // update the face frame source to track this body
                             _faceSources[i].TrackingId = _bodies[i].TrackingId;
                         }
                     }
                     
                }
            }
        }

        // Update FaceFrame Result
        void FaceReader_FrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {

            using (FaceFrame faceFrame = e.FrameReference.AcquireFrame())
            {
                if (faceFrame != null)
                {
                    // get the index of the face source from the face source array
                    int index = GetFaceSourceIndex(faceFrame.FaceFrameSource);
                    
                    // check if this face frame has valid face frame results
                    //if (ValidateFaceBoxAndPoints(faceFrame.FaceFrameResult))
                    if(faceFrame.FaceFrameResult != null)
                    {
                        // store this face frame result to draw later
                        _faceResults[index] = faceFrame.FaceFrameResult;
                        //printOut("face tracked with " + index);
                    }
                    else
                    {
                        // indicates that the latest face frame result from this reader is invalid
                        _faceResults[index] = null;
                    }
                }
            }

        }

        private int GetFaceSourceIndex(FaceFrameSource faceFrameSource)
        {
            int index = -1;

            for (int i = 0; i < _faceSources.Length; i++)
            {
                if (_faceSources[i] == faceFrameSource)
                {
                    index = i;
                    break;
                }
            }

            return index;
        }

        // not using this now
        private bool ValidateFaceBoxAndPoints(FaceFrameResult faceResult)
        {
            bool isFaceValid = faceResult != null;

            if (isFaceValid)
            {
                var faceBox = faceResult.FaceBoundingBoxInColorSpace;
                if (faceBox != null)
                {
                    // check if we have a valid rectangle within the bounds of the screen space
                    isFaceValid = (faceBox.Right - faceBox.Left) > 0 &&
                                  (faceBox.Bottom - faceBox.Top) > 0 &&
                                  faceBox.Right <= SkeletonCanvas.Width &&
                                  faceBox.Bottom <= SkeletonCanvas.Height;

                    if (isFaceValid)
                    {
                        var facePoints = faceResult.FacePointsInColorSpace;
                        if (facePoints != null)
                        {
                            foreach (PointF pointF in facePoints.Values)
                            {
                                // check if we have a valid face point within the bounds of the screen space
                                bool isFacePointValid = pointF.X > 0.0f &&
                                                        pointF.Y > 0.0f &&
                                                        pointF.X < SkeletonCanvas.Width &&
                                                        pointF.Y < SkeletonCanvas.Height;

                                if (!isFacePointValid)
                                {
                                    isFaceValid = false;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return isFaceValid;
        }

        #endregion KinectEvents

        #region Drawing 
        // all drawing funciton moved to extension.cs but keep here just in case later. 
        private void DrawBody(Body body)
        {
            // draw points
            foreach (JointType type in body.Joints.Keys)
            {
                switch (type)
                {
                    case JointType.Head:
                    case JointType.FootLeft:
                    case JointType.FootRight:
                        DrawJoint(body.Joints[type], 20, Brushes.Yellow, 2, Brushes.White);
                        break;
                    case JointType.ShoulderLeft:
                    case JointType.ShoulderRight:
                    case JointType.HipLeft:
                    case JointType.HipRight:
                        DrawJoint(body.Joints[type], 20, Brushes.YellowGreen, 2, Brushes.White);
                        break;
                    case JointType.ElbowLeft:
                    case JointType.ElbowRight:
                    case JointType.KneeLeft:
                    case JointType.KneeRight:
                        DrawJoint(body.Joints[type], 15, Brushes.LawnGreen, 2, Brushes.White);
                        break;
                    case JointType.HandLeft:
                        DrawHandJoint(body.Joints[type], body.HandLeftState, 20, 2, Brushes.White);
                        break;
                    case JointType.HandRight:
                        DrawHandJoint(body.Joints[type], body.HandRightState, 20, 2, Brushes.White);
                        break;
                    default:
                        DrawJoint(body.Joints[type], 15, Brushes.RoyalBlue, 2, Brushes.White);
                        break;

                }
            }
        }

        private void DrawJoint(Joint joint, double radius, SolidColorBrush fill, double borderWidth, SolidColorBrush border)
        {
            if (joint.TrackingState != TrackingState.Tracked) return;

            // Map the CameraPoint to Colorspace for matching
            ColorSpacePoint colorPoint = _kinect.CoordinateMapper.MapCameraPointToColorSpace(joint.Position);

            // create the ui element on the params
            Ellipse el = new Ellipse();
            el.Fill = fill;
            el.Stroke = border;
            el.StrokeThickness = borderWidth;
            el.Width = el.Height = radius;

            //add element to SkeletonCanvas
            SkeletonCanvas.Children.Add(el);

            // avoid exception on bad tracking
            if (float.IsInfinity(colorPoint.X) || float.IsInfinity(colorPoint.Y)) return;

            // allign ellipse on canvas (divide by 2 since image is only 50% of original image size from Kinect)
            Canvas.SetLeft(el, colorPoint.X / 2);
            Canvas.SetTop(el, colorPoint.Y / 2);
        }


        private void DrawHandJoint(Joint joint, HandState handState, double radius, double borderWidth, SolidColorBrush border)
        {
            switch (handState)
            {
                case HandState.Lasso:
                    DrawJoint(joint, radius, Brushes.Cyan, borderWidth, border);
                    break;
                case HandState.Open:
                    DrawJoint(joint, radius, Brushes.Green, borderWidth, border);
                    break;
                case HandState.Closed:
                    DrawJoint(joint, radius, Brushes.Red, borderWidth, border);
                    break;
                default:
                    break;

            }
        }

        

        #endregion Drawing

        #region UI
        // bodyTrack toggle
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Handle(sender as CheckBox);
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Handle(sender as CheckBox);
        }

        //faceBox_Checked

        private void faceBox_Checked(object sender, RoutedEventArgs e)
        {
            goFaceTrack = true;
            
        }

        private void faceBox_Unchecked(object sender, RoutedEventArgs e)
        {
           
            SkeletonCanvas.Children.Clear();
            goFaceTrack = false;
           
        }

        void Handle(CheckBox checkBox)
        {
            // Use IsChecked.
            bool flag = checkBox.IsChecked.Value;

            // Assign Window Title.
            String test = "IsChecked = " + flag.ToString();
            System.Console.WriteLine(test);
            if (flag) goTrack = true;
            else StopBody();
        }
        #endregion UI
        public void printOut(string input) {
            System.Console.WriteLine(input);
        }

        private void faceToArduino(int i, FaceFrameResult faceResult)
        {

            // debouncing mouth state

            var mouthOpen = faceResult.FaceProperties[FaceProperty.MouthOpen];

            if (mouthOpen == DetectionResult.Yes)
            {
                mouthReading[i] = true;
            }
            else
            {
                mouthReading[i] = false;
            }

            if (mouthReading[i] != lastMouthState[i])
            {
                // reset the debouncing timer
                lastDebounceTime[i] = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            }

            if ((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - lastDebounceTime[i]) > 50)
            {
                // whatever the reading is at, it's been there for longer
                // than the debounce delay, so take it as the actual current state:

                // if the mouth state has changed:
                if (mouthReading[i] != mouthState[i])
                {
                    mouthState[i] = mouthReading[i];
                    printOut("mouth state is " + mouthState[i]);
                    ledControl(mouthState[i]);
                    
                }
            }

            lastMouthState[i] = mouthReading[i];
          
            /*
            if (mouthOpen == DetectionResult.Yes)
            {
                mouthReading[i] = true;
            }
            else
            {
                mouthReading[i] = false;
            }



            if (mouthReading[i] != lastMouthState[i])
            {
                ledControl(mouthReading[i]);


                long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

                printOut("mouthState " + mouthReading[i] + " // " + milliseconds);
            }
            lastMouthState[i] = mouthReading[i]; 
            */



        }

        private void ledControl(bool b) {
            String outMessage;
            if (!b) outMessage = "hi:1,off\n";
            else outMessage = "hi:1,on\n";
            if (myPort.IsOpen) myPort.Write(outMessage);
        }

    }

    
}
