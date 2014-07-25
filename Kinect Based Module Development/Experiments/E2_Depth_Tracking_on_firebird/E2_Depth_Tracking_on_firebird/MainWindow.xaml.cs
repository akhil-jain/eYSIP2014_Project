/*
 This experiment is used for tracking the depth of each pixel in a 320 x 240 frame captured by the kinect.
 The kinect captures 30 frames per second.
 The distance of the center pixel is displayed on the main window.
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

        int counttowards = 0, countaway = 0, newvalue = 1500, farcount = 0, nearcount = 0;
        static int oldvalue = 1500;

        KinectSensor newSensor, sensor;     //Variables of type KinectSensor
        KinectSensorChooser chooser = new KinectSensorChooser();        //Kinect chooser for determining the state of the kinect (disconnected, ready, not powered on etc)
                                                                        //This function is used for attaching a reference to the kinect sensor
        int countnear, countfar = 0, msgflag = 1;
        MessageBox msgBox;

        // Serial port to transfer data serially
        // This may be different everytime the user plugs in the zigbee module hence appropriate changes are required
        // Verify the COM port on XCTU software and use the same here
        System.IO.Ports.SerialPort myport = new System.IO.Ports.SerialPort("COM2");

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            chooser.KinectChanged += chooser_KinectChanged;
            chooserUI.KinectSensorChooser = chooser;        //link KinectSensorChooser with KinectSensorChooserUI
            chooser.Start();        //Initialize the Chooser to find a Kinect to use
            if (myport.IsOpen == false)
            {
                myport.Open();

            }
        }


        /* This function is used to stop any previous instances of a running kinect sensor
         * and starts a new referance under the name NewSensor.
         * if a kinect sensor is connected to the PC then its depth stream is enabled
         * to receive the depth data and the sensor is started. Exceptions may occur
         * during the process of starting the sensor which is taken care of in the try-
         * catch block.
         */ 
        void chooser_KinectChanged(object sender, KinectChangedEventArgs e)
        {
            KinectSensor oldSensor = (KinectSensor)e.OldSensor;     //Get reference to any old sensor
            StopKinect(oldSensor);
            //Stop the previous instances if any
            KinectSensor newSensor = (KinectSensor)e.NewSensor;     //Get a reference to a new (latest) kinect sensor
            if (newSensor == null)
            {
                return;     //If no kinect sensor is connected simply return
            }
            newSensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);      //If a kinect sensor is attached then enable the depth stream to recerive depth data of the frame of resolution 320 x 240
            newSensor.SkeletonStream.Enable();
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
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                //start the depth frame
                if (depthFrame == null)
                {
                    return;
                }       //if no frames are available then return because there is nothing to process
                byte[] pixels = GenerateColoredBytes(depthFrame);       //array of bytes for the pixels
                int stride = depthFrame.Width * 4;      //the data is in the format - Blue,Green,Red,Blank 
                image1.Source = BitmapSource.Create(depthFrame.Width, depthFrame.Height, 96, 96, PixelFormats.Bgr32, null, pixels, stride);     //video frame
            }
        }

        //This function reveives the depth frame as an input and based on the depth, 
        //it assigns Blue color for pixels less than 900mm away (minimum distance from kinect must be 800mm)
        //Red color for pixels in the range 900mm to 2000mm
        //Green color for pixels at a distance greater than 2000mm in the frame
        //it also displays the center pixel distance from the kinect

        private byte[] GenerateColoredBytes(DepthImageFrame depthFrame)
        {
            short[] rawDepthData = new short[depthFrame.PixelDataLength];       //array of short for the raw data storage
            depthFrame.CopyPixelDataTo(rawDepthData);       //copy the pixels data to the rawDepthData array
            Byte[] pixels = new byte[depthFrame.Height * depthFrame.Width * 4];     //the data is in the format - Blue,Green,Red,Blank hence length of the array is width x height x 4
            const int BlueIndex = 0;
            const int GreenIndex = 1;
            const int RedIndex = 2;     //these constants are only for simple understanding of color assignments to pixels
            int raw = rawDepthData.Length;      //set raw as the no. of pixels

            newvalue = rawDepthData[(raw / 2) - (depthFrame.Width) / 2] >> DepthImageFrame.PlayerIndexBitmaskWidth;     //distance of center pixel from the kinect sensor

           if (newvalue < oldvalue)
                    counttowards++;     //if center pixel is coming towards the kinect, increase counttowards
                else
               countaway++;        //if center pixel is going outward from the kinect, increase countaway

            if (countaway > counttowards)       
            {
                farcount++;     //increment farcount
                if (farcount > 10)      //if moving away for 10 frames consecutively
                {
                    countaway = 0;      //reset countaway
                    counttowards = 0;   //reset counttowards
                    label2.Content = "Direction of firebird - Away from Kinect";    //display in GUI
                    farcount = 0;   //reset farcount
                    try
                    {
                        if (System.IO.Ports.SerialPort.GetPortNames().Contains("COM2"))
                        {
                            if (myport.IsOpen)
                            {
                                myport.WriteLine("52");
                            }
                            else
                            {
                                label1.Content = "";
                                myport.Open();
                            }
                        }
                        else
                        {
                            label1.Content = "COM port does not exist";
                            label2.Content = "";
                        }
                    }
                    catch (System.InvalidOperationException)
                    {
                        MessageBox.Show("Unable to write to COM port");
                    }
                }
            }

            else
            {
                nearcount++;        //increment nearcount
                if (nearcount > 10)     //if moving towards kinect for 10 frames
                {
                    countaway = 0;      //reset countaway
                    counttowards = 0;   //reset counttowards
                    label2.Content = "Direction of firebird - Towards Kinect";      //display in GUI
                    nearcount = 0;      //reset nearcount
                    try
                    {
                        if (System.IO.Ports.SerialPort.GetPortNames().Contains("COM2"))
                        {
                            if (myport.IsOpen)
                            {
                                myport.WriteLine("58");
                            }
                            else
                            {
                                label1.Content = "";
                                myport.Open();
                            }
                        }
                        else
                        {
                            label1.Content = "COM port does not exist";
                            label2.Content = "";
                        }
                    }
                    catch (System.InvalidOperationException)
                    {
                        MessageBox.Show("Unable to write to COM port");
                    }
                }
            }
            oldvalue = newvalue;        //set oldvalue as newvalue
            label1.Content = "Distance of center pixel from kinect = " + newvalue + " mm";          //display in GUI the disance of center pixel

            for (int depthIndex = 0, colorIndex = 0; depthIndex < rawDepthData.Length && colorIndex < pixels.Length; depthIndex++, colorIndex += 4)
            {
                int player = rawDepthData[depthIndex] & DepthImageFrame.PlayerIndexBitmask;     //based on the depth the player index is extracted (there may be multiple players in the frame)
                int depth = rawDepthData[depthIndex] >> DepthImageFrame.PlayerIndexBitmaskWidth;//get depth of the player
                if (depth <= 900)
                {
                    pixels[colorIndex + BlueIndex] = 255;       //Blue color
                    pixels[colorIndex + RedIndex] = 0;
                    pixels[colorIndex + GreenIndex] = 0;
                }
                else if (depth > 900 && depth <= 2000)
                {
                    pixels[colorIndex + BlueIndex] = 0;
                    pixels[colorIndex + RedIndex] = 255;        //Green color
                    pixels[colorIndex + GreenIndex] = 0;
                }
                else if (depth > 2000)
                {
                    pixels[colorIndex + BlueIndex] = 0;
                    pixels[colorIndex + RedIndex] = 0;
                    pixels[colorIndex + GreenIndex] = 255;      //Red color
                }
            }
            return pixels;      //return pixels array
        }

        //this function disables the colorstream and stops the audiosource and the sensor itself
        //it is essential to call this function before terminating the program
        //if not called it may lead to unhandled exceptions
        void StopKinect(KinectSensor sensor)
        {
            try
            {
                if (sensor != null)
                {
                    sensor.Stop();
                    //stop kinect sensor
                    sensor.AudioSource.Stop();
                    //stop audio source
                    sensor.ColorStream.Disable();
                    //disable the colorstream
                }
            }
            catch (System.NullReferenceException)
            {
                chooser.TryResolveConflict();
            }
        }

        // When the main window is closed the execution is terminated
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopKinect(chooser.Kinect);
        }
    }
}