/*
 * This experiment is used for hand cursor movement 
 * by tracking the hand joint.
 * Just hover on the buttons in the GUI to make the firebird robot move.
 * The buttons make the firebird V robot move left, right, up, down.
 * the commands transmitted serially are as follows :
 * 2 - backward
 * 4 - left
 * 5 - stop
 * 6 - right
 * 8 - forward
 */

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
using Microsoft.Kinect.Toolkit;
using Microsoft.Kinect;
using Microsoft.Samples.Kinect.WpfViewers;


namespace WpfApplication4
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        KinectSensorChooser chooser = new KinectSensorChooser();
        KinectSensor newSensor;
        private WriteableBitmap _ColorImageBitmap;
        private Int32Rect _ColorImageBitmapRect;
        private int _ColorImageStride;
        private Skeleton[] FrameSkeletons;
        System.IO.Ports.SerialPort myPort = new System.IO.Ports.SerialPort("COM3");// Serial port to transfer data serially
        // This may be different everytime the user plugs in the zigbee module hence appropriate changes are required
        // Verify the COM port on XCTU software and use the same here

        List<Button> buttons;
        static Button selected;

        float handX;
        float handY;

        public MainWindow()
        {

            InitializeComponent();
            InitializeButtons();
            //After Initialization subscribe to the loaded event of the form
            ResetHandPosition(kinectButton);//Reset hand position
            chooser.KinectChanged += chooser_KinectChanged;//Event handler for when kinect sensor has changed
            chooserUI.KinectSensorChooser = chooser;//Connect KinectSensorChooser with KinectSensorChooserUI
            chooser.Start();//Initialize the Chooser to find a Kinect to use
            InitializeKinectSensor(newSensor);
            if (myPort.IsOpen == false) //if not open, open the port
                myPort.Open();
            kinectButton.Click += new RoutedEventHandler(kinectButton_Click);
            this.WindowState = System.Windows.WindowState.Maximized;
            this.WindowStyle = System.Windows.WindowStyle.None;
            InitializeKinectSensor(chooser.Kinect);
        }

        public static int LoadingStatus = 0;

        //Function to reset hand position, called at start of program
        public static void ResetHandPosition(Coding4Fun.Kinect.Wpf.Controls.HoverButton btnHoverButton)
        {
            btnHoverButton.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;

            btnHoverButton.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;

        }


        void kinectButton_Click(object sender, RoutedEventArgs e)
        {
            selected.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, selected));
        }
        //initialize buttons to be checked 
        private void InitializeButtons()
        {
            buttons = new List<Button> { UP, RIGHT, DOWN, LEFT, STOP };//initialize List of buttons in GUI
        }



        /* This function is used to stop any previous instances of a running kinect sensor
         * and starts a new reference under the name newSensor.
         * if a kinect sensor is connected to the PC then its depth stream is enabled
         * to receive the depth data and the sensor is started. Exceptions may occur
         * during the process of starting the sensor which is taken care of in the try-
         * catch block.
         */ 
        void chooser_KinectChanged(object sender, KinectChangedEventArgs e)
        {
            KinectSensor oldSensor = (KinectSensor)e.OldSensor;//Get reference to any old sensor
            StopKinect(oldSensor);//Stop the previous instances if any
            KinectSensor newSensor = (KinectSensor)e.NewSensor;//Get a reference to a new (latest) kinect sensor
            if (newSensor == null)
            {
                return;//If no kinect sensor is connected simply return
            }
            try
            {
                newSensor.Start();//Start the kinect sensor
            }
            catch (System.IO.IOException)
            {
                chooser.TryResolveConflict();//Allow chooser to resolve conflict if any
            }
            
        }

        private void InitializeKinectSensor(KinectSensor kinectSensor)
        {
            if (kinectSensor != null)
            {
                ColorImageStream colorStream = kinectSensor.ColorStream;//initialize colorstream
                colorStream.Enable();//enable colorstream
                this._ColorImageBitmap = new WriteableBitmap(colorStream.FrameWidth, colorStream.FrameHeight,
                    96, 96, PixelFormats.Bgr32, null);//create new writeablebitmap to display captured image frame(Not used here, can be added through gui)
                this._ColorImageBitmapRect = new Int32Rect(0, 0, colorStream.FrameWidth, colorStream.FrameHeight);//Add a border to the frame
                this._ColorImageStride = colorStream.FrameWidth * colorStream.FrameBytesPerPixel;//Set stride

                kinectSensor.SkeletonStream.Enable(new TransformSmoothParameters()//Apply various smoothing parameters to smooth the cursor movement
                {
                    Correction = 0.5f,
                    JitterRadius = 0.05f,
                    MaxDeviationRadius = 0.04f,
                    Smoothing = 0.5f
                });

                kinectSensor.SkeletonFrameReady += Kinect_SkeletonFrameReady;//Event handler for when a new skeletonframe is ready
                kinectSensor.ColorFrameReady += Kinect_ColorFrameReady;//Event handler for when a new color frame is ready

                if (!kinectSensor.IsRunning)
                {
                    kinectSensor.Start();//Start the kinect sensor if it is not running
                }

                this.FrameSkeletons = new Skeleton[kinectSensor.SkeletonStream.FrameSkeletonArrayLength];//Store the detected skeleton

            }
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopKinect(chooser.Kinect);
            //stop the Sensor
        }

        void StopKinect(KinectSensor sensor)
        {
            if (sensor != null)
            {
                if (sensor.IsRunning)
                {
                    sensor.Stop();//stop the Sensor
                    if (sensor.AudioSource != null)
                    {
                        sensor.AudioSource.Stop();//Stop audio input from kinect
                    }
                }
            }
        }

        //This is the event handling code for the colorFrameReady event triggered when a new colorFrame is available from the kinect
        private void Kinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame frame = e.OpenColorImageFrame())//initialize the color frame
            {
                if (frame != null)
                {
                    byte[] pixelData = new byte[frame.PixelDataLength];//array of bytes for the pixels
                    frame.CopyPixelDataTo(pixelData);//the data is in the format - Blue,Green,Red,Blank hence stride is width x4
                    this._ColorImageBitmap.WritePixels(this._ColorImageBitmapRect, pixelData,
                        this._ColorImageStride, 0);
                    //passing color image frame to GUI for displaying live feed
                }
            }
        }

        private void Kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame != null)
                {
                    frame.CopySkeletonDataTo(this.FrameSkeletons);
                    Skeleton skeleton = GetPrimarySkeleton(this.FrameSkeletons);

                    if (skeleton == null)
                    {
                        kinectButton.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        Joint primaryHand = GetPrimaryHand(skeleton);
                        TrackHand(primaryHand);

                    }
                }
            }
        }

        //track and display hand
        private void TrackHand(Joint hand)
        {
            if (hand.TrackingState == JointTrackingState.NotTracked)
            {
                kinectButton.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                kinectButton.Visibility = System.Windows.Visibility.Visible;

                DepthImagePoint point = this.chooser.Kinect.CoordinateMapper.MapSkeletonPointToDepthPoint(hand.Position, DepthImageFormat.Resolution640x480Fps30);
                
                handX = (int)((point.X * LayoutRoot.ActualWidth / this.chooser.Kinect.DepthStream.FrameWidth) -
                    (kinectButton.ActualWidth / 2.0));//Transform X coordinate of hand in image frame to some X coordinate of GUI
                handY = (int)((point.Y * LayoutRoot.ActualHeight / this.chooser.Kinect.DepthStream.FrameHeight) -
                    (kinectButton.ActualHeight / 2.0));//Transform Y coordinate of hand in image frame to some X coordinate of GUI
                Canvas.SetLeft(kinectButton, handX);
                Canvas.SetTop(kinectButton, handY);
                label1.Content = handX.ToString() + " " + handY.ToString();

                if (isHandOver(kinectButton, buttons)) kinectButton.Hovering();//called if hand is hovering over a button
                else kinectButton.Release();//else release the button
                if (hand.JointType == JointType.HandRight)
                {
                    kinectButton.ImageSource = "/WpfApplication4;component/Images/myhand.png";//hand image
                    kinectButton.ActiveImageSource = "/WpfApplication4;component/Images/myhand.png";//hand image
                }
                else
                {
                    kinectButton.ImageSource = "/WpfApplication4;component/Images/myhand.png";//hand image
                    kinectButton.ActiveImageSource = "/WpfApplication4;component/Images/myhand.png";//hand image
                }
            }
        }

        //detect if hand is overlapping over any button
        private bool isHandOver(FrameworkElement hand, List<Button> buttonslist)
        {
            var handTopLeft = new Point(Canvas.GetLeft(hand), Canvas.GetTop(hand));
            var handX = handTopLeft.X + hand.ActualWidth / 2;
            var handY = handTopLeft.Y + hand.ActualHeight / 2;

            foreach (Button target in buttonslist)//Check if hand is over any buttons in the buttons list
            {

                if (target != null)
                {
                    Point targetTopLeft = new Point(Canvas.GetLeft(target), Canvas.GetTop(target));
                    if (handX > targetTopLeft.X &&
                        handX < targetTopLeft.X + target.Width &&
                        handY > targetTopLeft.Y &&
                        handY < targetTopLeft.Y + target.Height)
                    {
                        selected = target;
                        return true;
                    }
                }
            }
            return false;
        }

        //get the hand closest to the Kinect sensor
        private static Joint GetPrimaryHand(Skeleton skeleton)
        {
            Joint primaryHand = new Joint();
            if (skeleton != null)
            {
                primaryHand = skeleton.Joints[JointType.HandLeft];
                Joint rightHand = skeleton.Joints[JointType.HandRight];
                if (rightHand.TrackingState != JointTrackingState.NotTracked)
                {
                    if (primaryHand.TrackingState == JointTrackingState.NotTracked)
                    {
                        primaryHand = rightHand;
                    }
                    else
                    {
                        if (primaryHand.Position.Z > rightHand.Position.Z)
                        {
                            primaryHand = rightHand;
                        }
                    }
                }
            }
            return primaryHand;
        }

        //get the skeleton closest to the Kinect sensor
        private static Skeleton GetPrimarySkeleton(Skeleton[] skeletons)
        {
            Skeleton skeleton = null;
            if (skeletons != null)
            {
                for (int i = 0; i < skeletons.Length; i++)
                {
                    if (skeletons[i].TrackingState == SkeletonTrackingState.Tracked)
                    {
                        if (skeleton == null)
                        {
                            skeleton = skeletons[i];
                        }
                        else
                        {
                            if (skeleton.Position.Z > skeletons[i].Position.Z)
                            {
                                skeleton = skeletons[i];
                            }
                        }
                    }
                }
            }
            return skeleton;
        }

        //Event handler when hand is over UP button
        private void UP_Click(object sender, RoutedEventArgs e)
        {
            myPort.WriteLine("58");
        }

        //Event handler when hand is over RIGHT button
        private void RIGHT_Click(object sender, RoutedEventArgs e)
        {
            myPort.WriteLine("56");
        }

        //Event handler when hand is over DOWN button
        private void DOWN_Click(object sender, RoutedEventArgs e)
        {
            myPort.WriteLine("52");
        }

        //Event handler when hand is over LEFT button
        private void LEFT_Click(object sender, RoutedEventArgs e)
        {
            myPort.WriteLine("54");
        }

        //Event handler when hand is over STOP button
        private void STOP_Click(object sender, RoutedEventArgs e)
        {
            myPort.WriteLine("5");
        }


    }
}
