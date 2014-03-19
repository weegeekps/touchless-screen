using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TouchlessScreenLibrary
{
    public class TouchlessScreen
    {
        private static readonly Lazy<TouchlessScreen> lazy = new Lazy<TouchlessScreen>(() => new TouchlessScreen());

        private bool isInitalized;

        #region Private Ctors
        private TouchlessScreen()
        {
            this.isInitalized = false;
        }
        #endregion

        #region Static Properties
        public static TouchlessScreen Instance
        {
            get
            {
                return lazy.Value;
            }
        }
        #endregion

        #region Private Methods & Properties
        #endregion

        #region Public Methods & Properties

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        public KinectSensor Sensor { get; private set; }

        public void Initialize()
        {
            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.Sensor = potentialSensor;
                    break;
                }
            }

            if (this.Sensor != null)
            {
                // Turn on the depth stream to receive depth frames
                this.Sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

                // Add an event handler to be called whenever there is new depth frame data
                //this.Sensor.DepthFrameReady += this.SensorDepthFrameReady;

                this.Sensor.DepthStream.Range = DepthRange.Near;
                this.Sensor.SkeletonStream.EnableTrackingInNearRange = true; // enable returning skeletons while depth is in Near Range
                this.Sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated; // Use seated tracking
                this.Sensor.SkeletonStream.Enable();
            }

            this.isInitalized = true;
        }

        public bool TryStart()
        {
            if (!this.isInitalized)
            {
                return false;
            }

            try
            {
                this.Sensor.Start();

                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }
        #endregion
    }
}
