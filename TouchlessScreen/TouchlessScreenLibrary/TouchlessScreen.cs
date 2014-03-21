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
        private Skeleton[] skeletons;

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

        public DepthImagePoint GetSkeletonDepthPoint(AllFramesReadyEventArgs eventArgs, JointType joint)
        {
            Skeleton skeleton = null;
            SkeletonPoint point;

            this.skeletons = new Skeleton[0];

            if (eventArgs == EventArgs.Empty)
            {
                throw new InvalidOperationException("eventArgs can not be empty");
            }

            using (SkeletonFrame skeletonFrame = eventArgs.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }

                int len = skeletons.Length;

                for (int i = 0; i < len; ++i)
                {
                    skeleton = skeletons[i];

                    if (skeleton.TrackingState == SkeletonTrackingState.Tracked || skeleton.TrackingState == SkeletonTrackingState.PositionOnly)
                    {
                        break;
                    }
                }

                if (skeleton != null)
                {
                    Joint wrist = skeleton.Joints[JointType.HandLeft];
                    point = wrist.Position;
                    return this.Sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(point, DepthImageFormat.Resolution640x480Fps30);
                }
            }

            return new DepthImagePoint();
        }

        public void Initialize()
        {
            if (!this.isInitalized)
            {
                this.skeletons = new Skeleton[0];

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
