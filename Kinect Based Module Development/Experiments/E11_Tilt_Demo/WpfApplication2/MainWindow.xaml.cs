/*
 This experiment is used for changing the elevation angle of the kinect.

 */

using System;
using System.Timers;
using System.Collections.Generic;
using System.ComponentModel;
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
using Microsoft.Samples.Kinect.WpfViewers;

namespace WpfApplication2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //After Initialization subscribe to the loaded event of the form 
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            button1.IsEnabled = false;

            //Set angle to slider1 value
            if (chooser.Kinect != null && chooser.Kinect.IsRunning)
            {
                chooser.Kinect.ElevationAngle = (int)slider1.Value;
                //Set elevation angle of kinect
                label1.Content = chooser.Kinect.ElevationAngle;
             //display current elevation angle in GUI   
            }

            //Do not change Elevation Angle often, please see documentation on this and Kinect Explorer for a robust example
            System.Threading.Thread.Sleep(new TimeSpan(hours: 0, minutes: 0, seconds: 1));
            button1.IsEnabled = true;
        }


        KinectSensorChooser chooser = new KinectSensorChooser();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            chooser.KinectChanged += chooser_KinectChanged;
            chooserUI.KinectSensorChooser = chooser;
            chooser.Start();//Initialize the Chooser to find a Kinect to use
            
        }

        void chooser_KinectChanged(object sender, KinectChangedEventArgs e)
        {
            KinectSensor oldSensor = (KinectSensor)e.OldSensor;//Get reference to any old sensor
            StopKinect(oldSensor);//Stop the previous instances if any
            KinectSensor newSensor = (KinectSensor)e.NewSensor;//Get a reference to a new (latest) kinect sensor
            if(newSensor == null)
            {
                return;//If no kinect sensor is connected simply return
            }
            try
            {
                newSensor.Start();//Start the kinect sensor
            }
            catch (System.IO.IOException)
            {
                //Some other application is streaming from the same Kinect sensor
                chooser.TryResolveConflict();//Allow chooser to resolve conflict if any
            }
        }

        

        void StopKinect(KinectSensor sensor)
        {
            if (sensor != null)
            {
                if (sensor.IsRunning)
                {
                    sensor.Stop();//stop kinect sensor
                    if (sensor.AudioSource != null)
                    {
                        sensor.AudioSource.Stop();//stop audio source
                    }
                }
            }
        }

        // When the main window is closed the execution is terminated
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopKinect(chooser.Kinect);

        }

    }
}
