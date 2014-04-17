﻿using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TouchlessScreenLibrary
{

    public class TouchlessScreen
    {
        /// <summary>
        /// Width of output drawing
        /// </summary>
        public const float RenderWidth = 640.0f;
        public const int IMG_WIDTH = (int)RenderWidth;
        /// <summary>
        /// Height of our output drawing
        /// </summary>
        public const float RenderHeight = 480.0f;
        public const int IMG_HEIGHT = (int)RenderHeight;

        private static readonly Point3d<int> DEPTH_UPPER_LEFT = new Point3d<int>(180, 220, 0);
        private static readonly Point3d<int> DEPTH_CENTER = new Point3d<int>(325, 250, 0);
        private static readonly Point3d<int> DEPTH_LOWER_RIGHT = new Point3d<int>(430, 320, 0);

        private bool[,] handPixels;
        private bool[,] fingerPixels;
        private bool[,] contourPixels;
        private byte[] intensityValues;
        public DepthImagePoint handPoint;
        public DepthImagePoint headPoint;
        private static readonly Lazy<TouchlessScreen> lazy = new Lazy<TouchlessScreen>(() => new TouchlessScreen());

        private bool isInitalized;
        private Skeleton[] skeletons;

        /// <summary>
        /// Intermediate storage for the depth data received from the camera
        /// </summary>
        private DepthImagePixel[] depthPixels;

        #region Private Ctors
        private TouchlessScreen()
        {
            this.isInitalized = false;
            this.intensityValues = new byte[0];
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
        private Point3d<int> CalculateVector(Point3d<int> a, Point3d<int> b)
        {
            int x = b.X - a.X;
            int y = b.Y - a.Y;
            int z = b.Z - a.Z;

            return new Point3d<int>
            {
                X = x,
                Y = y,
                Z = z,
            };
        }

        private Point3d<int> CalculateVector(DepthImagePoint a, DepthImagePoint b)
        {
            return this.CalculateVector(this.ConvertDepthImagePointToPoint3d(a), this.ConvertDepthImagePointToPoint3d(b));
        }

        private Point3d<int> CalculateNormalVector(Point3d<int> a, Point3d<int> b, Point3d<int> c)
        {
            int normalX;
            int normalY;
            int normalZ;

            Point3d<int> ab = this.CalculateVector(b, a);  // A - B
            Point3d<int> bc = this.CalculateVector(c, b);  // B - C

            normalX = (ab.Y * bc.Z) - (bc.Y * ab.Z);
            normalY = (ab.Z * bc.X) - (bc.Z * ab.X);
            normalZ = (ab.X * bc.Y) - (bc.X * ab.Y);

            return new Point3d<int>
            {
                X = normalX,
                Y = normalY,
                Z = normalZ,
            };
        }

        private Point2d<int> MapRealspacePointToScreen(Point3d<int> origin, Point3d<int> direction, Point3d<int> normalVector)
        {
            int x;
            int y;
            int t;

            if (origin.Equals(new Point3d<int>(0, 0, 0)) || direction.Equals(new Point3d<int>(0, 0, 0)))
            {
                return new Point2d<int>(0, 0);
            }

            if (normalVector.X != 0 && normalVector.Y != 0)
            {
                // For the screen plane, the normal vector *should* have X and Y values which are 0.
                //   If it doesn't, then the algorithm below won't work.
                throw new InvalidDataException("Normal Vector X or Y is non-zero.");
            }

            t = -(origin.Z / direction.Z);

            x = origin.X + (direction.X * t);
            y = origin.Y + (direction.Y * t);

            // THIS ISN'T DONE YET.
            // TODO: ACTUALLY GET MapRealspacePointToScreen working correctly!
            return new Point2d<int>
            {
                X = x,
                Y = y,
            };
        }

        private Point3d<int> ConvertDepthImagePointToPoint3d(DepthImagePoint depthPoint)
        {
            return new Point3d<int>
            {
                X = depthPoint.X,
                Y = depthPoint.Y,
                Z = depthPoint.Depth,
            };
        }
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
                    Joint wrist = skeleton.Joints[joint];
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
                handPixels = new bool[IMG_WIDTH, IMG_HEIGHT];
                fingerPixels = new bool[IMG_WIDTH, IMG_HEIGHT];
                contourPixels = new bool[IMG_WIDTH, IMG_HEIGHT];
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
                    // Allocate space to put the depth pixels we'll receive
                    this.depthPixels = new DepthImagePixel[this.Sensor.DepthStream.FramePixelDataLength];
                }
            }

            this.isInitalized = true;
        }

        public void HandleSensorEvent(object sender, AllFramesReadyEventArgs e)
        {
            try
            {
                this.handPoint = GetSkeletonDepthPoint(e, JointType.HandLeft);
                this.headPoint = GetSkeletonDepthPoint(e, JointType.Head);


                //Point3d<int> pointerRay = this.CalculateVector(this.handPoint, this.headPoint);
                //System.Diagnostics.Debug.WriteLine(this.ConvertDepthImagePointToPoint3d(this.handPoint).ToString());
                //System.Diagnostics.Debug.WriteLine(pointerRay);
                //System.Diagnostics.Debug.WriteLine(this.ConvertDepthImagePointToPoint3d(this.handPoint).ToString() + " && " + this.ConvertDepthImagePointToPoint3d(this.headPoint).ToString());

                // *** POINTER CODE, SHOULD BE MOVED TO OWN METHOD WHEN DONE ***
                Point3d<int> ptHandPoint = this.ConvertDepthImagePointToPoint3d(this.handPoint);
                Point3d<int> ptHeadPoint = this.ConvertDepthImagePointToPoint3d(this.headPoint);

                Point3d<int> normalVector = this.CalculateNormalVector(DEPTH_UPPER_LEFT, DEPTH_CENTER, DEPTH_LOWER_RIGHT);
                Point2d<int> screenPos = this.MapRealspacePointToScreen(ptHeadPoint, ptHandPoint, normalVector);

                System.Diagnostics.Debug.WriteLine(screenPos.ToString());
                // *** END POINTER CODE

                using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
                {
                    int minDepth;
                    int maxDepth;
                    int maxY, minY, maxX, minX, centerX = 0, centerY = 0;
                    int threshold;

                    if (depthFrame != null)
                    {
                        // Copy the pixel data from the image to a temporary array
                        depthFrame.CopyDepthImagePixelDataTo(this.depthPixels);
                        if (handPoint.Depth == 0)
                        {
                            minDepth = depthFrame.MinDepth;
                            maxDepth = depthFrame.MaxDepth;
                            minX = 0;
                            maxX = depthFrame.Width;
                            minY = 0;
                            maxY = depthFrame.Height;
                        }
                        else
                        {
                            // Get the min and max reliable depth for the current frame
                            minDepth = Math.Max(depthFrame.MinDepth, handPoint.Depth - 50);
                            maxDepth = Math.Min(depthFrame.MaxDepth, handPoint.Depth + 50);
                            centerX = handPoint.X;
                            centerY = handPoint.Y;
                            minX = Math.Max(0, centerX - 90);
                            maxX = Math.Min(depthFrame.Height, centerX + 90);
                            minY = Math.Max(0, centerY - 110);
                            maxY = Math.Min(depthFrame.Height, centerY + 90);
                            
                        }

                        threshold = headPoint.Depth - handPoint.Depth;
                        if (threshold > 500)


                        intensityValues = new byte[this.depthPixels.Length];
                        for (int i = 0; i < this.depthPixels.Length; ++i)
                        {
                            // Get the depth for this pixel
                            short depth = depthPixels[i].Depth;
                            int x = i % IMG_WIDTH;
                            int y = i / IMG_WIDTH;

                            byte intensity = (byte)(depth >= minDepth && depth <= maxDepth && y > minY && y < maxY && x > minX && x < maxX ? depth : 0);
                            intensityValues[i] = intensity;
                            handPixels[x, y] = intensity != 0;

                            // To convert to a byte, we're discarding the most-significant
                            // rather than least-significant bits.
                            // We're preserving detail, although the intensity will "wrap."
                            // Values outside the reliable depth range are mapped to 0 (black).

                            // Note: Using conditionals in this loop could degrade performance.
                            // Consider using a lookup table instead when writing production code.
                            // See the KinectDepthViewer class used by the KinectExplorer sample
                            // for a lookup table example.
                        }
                        if (handPoint.Depth != 0)
                        {
                            //List<Tuple<int, int, int>> convexHull = ConvexHullCreator.CreateHull(points);
                            List<Tuple<int, int>> contour = new List<Tuple<int, int>>();
                            List<Tuple<int, int>> interior = new List<Tuple<int, int>>();
                            contourPixels = new bool[IMG_WIDTH, IMG_HEIGHT];
                            fingerPixels = new bool[IMG_WIDTH, IMG_HEIGHT];
                            findInteriorAndContour(interior);
                            List<Tuple<int, int>> filtered_contour = (new ContourCreator(contourPixels)).findContour();
                            //we could probably play around with these parameters alot
                            Tuple<int, int> center = FingerFinder.findPalmCenter(interior, contour);
                            List<Tuple<int, int>> fingerPoints = FingerFinder.reduceFingerPoints(FingerFinder.findFingers(filtered_contour, 30, 1.7, center.Item1, center.Item2));
                            fingerPoints.ForEach(i =>
                            {
                                fingerPixels[i.Item1, i.Item2] = true;
                            });
                        }
                        
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        public void DrawBitmap(WriteableBitmap colorBitmap, byte[] colorPixels)
        {
            // Convert the depth to RGB
            int colorPixelIndex = 0;
            for (int i = 0; i < this.depthPixels.Length; ++i)
            {
                short depth = depthPixels[i].Depth;
                int x = i % 640;
                int y = i / 640;
                if (handPoint.Depth == 0)
                {
                    if (this.intensityValues.Length < 1)
                    {
                        System.Diagnostics.Debug.WriteLine("WARN: Intensity Values length was less than 0.");
                        return;
                    }

                    byte intensity = intensityValues[i];
                    // Write out blue byte
                    colorPixels[colorPixelIndex++] = intensity;

                    // Write out green byte
                    colorPixels[colorPixelIndex++] = intensity;

                    // Write out red byte                        
                    colorPixels[colorPixelIndex++] = intensity;
                }
                else if (fingerPixels[x, y])
                {
                    colorPixels[colorPixelIndex++] = 0;
                    colorPixels[colorPixelIndex++] = 0;
                    colorPixels[colorPixelIndex++] = 255;
                }
                else if (contourPixels[x, y])
                {
                    colorPixels[colorPixelIndex++] = 255;
                    colorPixels[colorPixelIndex++] = 0;
                    colorPixels[colorPixelIndex++] = 0;
                }
                else
                {
                    colorPixels[colorPixelIndex++] = 0;
                    colorPixels[colorPixelIndex++] = 0;
                    colorPixels[colorPixelIndex++] = 0;
                }
                // We're outputting BGR, the last byte in the 32 bits is unused so skip it
                // If we were outputting BGRA, we would write alpha here.
                ++colorPixelIndex;
            }
            // Write the pixel data into our bitmap
            colorBitmap.WritePixels(
                new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight),
                colorPixels,
                colorBitmap.PixelWidth * sizeof(int),
                0);
            /*                  this.colorBitmap.WritePixels(
              new Int32Rect(minX, minY, maxX, maxY),
              this.colorPixels,
              (maxX-minX) * sizeof(int),
              minX); */
        }

        /// <summary>
        /// Seperates the hand pixels into interior pixels and contour pixels
        /// </summary>
        private void findInteriorAndContour(List<Tuple<int, int>> interior)
        {
            List<Tuple<int, int>> points = new List<Tuple<int, int>>();

            int x, y;
            for (int i = 0; i < IMG_WIDTH; ++i)
            {
                for (int j = 0; j < IMG_HEIGHT; ++j)
                {
                    if (handPixels[i, j]) points.Add(new Tuple<int, int>(i, j));
                }
            }
            int conections;
            foreach (Tuple<int, int> point in points)
            {
                conections = 0;
                x = point.Item1;
                y = point.Item2;
                if (x == 0 || y == 0 || x == IMG_WIDTH - 1 || y == IMG_HEIGHT - 1) //add end of range pixels to contour
                {

                }
                else
                {
                    if (handPixels[x + 1, y]) ++conections;
                    if (handPixels[x, y + 1]) ++conections;
                    if (handPixels[x + 1, y]) ++conections;
                    if (handPixels[x + 1, y + 1]) ++conections;
                    if (handPixels[x - 1, y]) ++conections;
                    if (handPixels[x, y - 1]) ++conections;
                    if (handPixels[x - 1, y]) ++conections;
                    if (handPixels[x - 1, y - 1]) ++conections;
                    if (handPixels[x + 1, y - 1]) ++conections;
                    if (handPixels[x - 1, y + 1]) ++conections;
                    if (conections == 8)
                    {
                        interior.Add(new Tuple<int, int>(x, y));
                    }
                    else
                    {

                        contourPixels[x, y] = true;
                    }
                }
            }

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
