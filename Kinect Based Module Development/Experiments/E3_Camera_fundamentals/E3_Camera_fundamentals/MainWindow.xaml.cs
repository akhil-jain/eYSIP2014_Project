/*
 This experiment is used for displaying a live stream of the kinect camera input onscreen in a 320 x 240 frame captured by the kinect.
 The kinect captures 30 frames per second.
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
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;


namespace WpfApplication2
{

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();      //Initialization process when main window is loaded, ie. when the program is run
        }

        KinectSensor newSensor, sensor;     //Variables of type KinectSensor
        KinectSensorChooser chooser = new KinectSensorChooser();        //Kinect chooser for determining the state of the kinect 
                                                                        //(disconnected, ready, not powered on etc)
                                                                        //This function is used for attaching a reference to the kinect sensor

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            chooser.KinectChanged += chooser_KinectChanged;
            chooserUI.KinectSensorChooser = chooser;        //Connect KinectSensorChooser with KinectSensorChooserUI
            chooser.Start();        //Initialize the Chooser to find a Kinect to use
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
            KinectSensor oldSensor = (KinectSensor)e.OldSensor;
            //Get reference to any old sensor
            StopKinect(oldSensor);
            //Stop the previous instances if any
            KinectSensor newSensor = (KinectSensor)e.NewSensor;
            //Get a reference to a new (latest) kinect sensor
            if (newSensor == null)
            {
                return;     //If no kinect sensor is connected simply return
            }
            newSensor.ColorStream.Enable();     //If a kinect sensor is attached then Enable the depth stream to recerive color image data of the frame of resolution 320 x 240
            newSensor.AllFramesReady += newSensor_AllFramesReady;       //Event handler for handling a newly generated frame

            try
            {
                newSensor.Start();      //Start the kinect sensor
            }
            catch (System.IO.IOException)
            {
                chooser.TryResolveConflict();       //Allow chooser to resolve conflict if any
            }
        }

        //This is the event handling code for the AllFramesReady event triggered when a new frame is available from the kinect
        void newSensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())        //initialize the color frame
            {
                if (colorFrame == null)
                {
                    return;     //if no frames are available then return because there is nothing to process
                }
                byte[] pixels = new byte[colorFrame.PixelDataLength];       //array of bytes for the pixels
                colorFrame.CopyPixelDataTo(pixels);

                int stride = colorFrame.Width * 4;      //the data is in the format - Blue,Green,Red,Blank hence stride is width x4
                image1.Source = BitmapSource.Create(colorFrame.Width, colorFrame.Height, 96, 96, PixelFormats.Bgr32, null, pixels, stride);     //passing color image frame to GUI for displaying live feed
                label1.Content = "Live Stream Video (320 x 240)";
            }
        }

        /*This function disables the colorstream and stops the audiosource and the sensor itself
        it is essential to call this function before terminating the program
        if not called it may lead to unhandled exceptions*/

        void StopKinect(KinectSensor sensor)
        {
            try
            {
                if (sensor != null)
                {
                    sensor.Stop();      //stop kinect sensor
                    sensor.AudioSource.Stop();      //stop audio source
                    sensor.ColorStream.Disable();       //disable the colorstream
                }
            }
            catch (System.NullReferenceException)
            {
                chooser.TryResolveConflict();
            }

        }

        /* When the main window is closed, thif event handler is called and the execution is terminated*/
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopKinect(chooser.Kinect);

        }
    }
}