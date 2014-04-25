using System.Windows.Forms;
using Microsoft.Kinect;
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
using VMultiDllWrapper;

namespace TouchlessScreenLibrary
{

    public class TouchlessScreen : IDisposable
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

        /*private static readonly Point3d<int> DEPTH_UPPER_LEFT = new Point3d<int>(90, -90, 0);
        private static readonly Point3d<int> DEPTH_CENTER = new Point3d<int>(30, -130, 0);
        private static readonly Point3d<int> DEPTH_LOWER_RIGHT = new Point3d<int>(-30, -150, 0);*/
        private static readonly Point3d<int> DEPTH_UPPER_LEFT = new Point3d<int>(120, -100, 0); 
        private static readonly Point3d<int> DEPTH_CENTER = new Point3d<int>(36, -140, 0); 
        private static readonly Point3d<int> DEPTH_LOWER_RIGHT = new Point3d<int>(-40, -160, 0); 

        private bool[,] handPixels;
        private bool[,] fingerPixels;
        private bool[,] contourPixels;
        private byte[] intensityValues;
        public DepthImagePoint handPoint;
        public DepthImagePoint headPoint;
        public DepthImagePoint shoulderPoint;
        //public DepthImagePoint elbowPoint;
        //public DepthImagePoint wristPoint;
        public int threshold;
        private static readonly Lazy<TouchlessScreen> lazy = new Lazy<TouchlessScreen>(() => new TouchlessScreen());

        private bool isInitalized;
        private Skeleton[] skeletons;
        private VMulti vMulti;
        //private bool pressOnce = false;
        //private int iterationCounter = 0;

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
            int cursorX;
            int cursorY;
            double translatedX;
            double translatedY;

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

            Point2d<double> userPointer = new Point2d<double>
            {
                X = x,
                Y = y,
            };

            System.Diagnostics.Debug.WriteLine("WARNING: Pointing at - " + userPointer.ToString());

            // Translate into something usable
            //   NOTE: This ends up being 16px blocks with Adam's calibration settings. Not a great resolution, but decent.
            translatedX = ((userPointer.X - DEPTH_UPPER_LEFT.X) / (DEPTH_LOWER_RIGHT.X - DEPTH_UPPER_LEFT.X)) * System.Windows.SystemParameters.PrimaryScreenWidth; // Screen Width is likely 1920
            translatedY = ((userPointer.Y - DEPTH_UPPER_LEFT.Y) / (DEPTH_LOWER_RIGHT.Y - DEPTH_UPPER_LEFT.Y)) * System.Windows.SystemParameters.PrimaryScreenHeight; // Screen Height is likely 1080

            //cursorX = (int)Math.Ceiling(System.Windows.SystemParameters.PrimaryScreenWidth - translatedX);
            //cursorY = (int)Math.Ceiling(System.Windows.SystemParameters.PrimaryScreenHeight - translatedY);
            //cursorY = Math.Abs(cursorY);

            cursorX = (int)Math.Ceiling(translatedX);
            cursorY = (int)Math.Ceiling(translatedY);

            if (cursorX < 0 || cursorY < 0 || cursorX > System.Windows.SystemParameters.PrimaryScreenWidth || cursorY > System.Windows.SystemParameters.PrimaryScreenHeight)
            {
                System.Diagnostics.Debug.WriteLine("WARNING: Cursor position outside of bounds. X: {0} Y: {1}", cursorX, cursorY);
            }

            return new Point2d<int>
            {
                X = cursorX,
                Y = cursorY,
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

        private void UpdateMultiTouch(Point2d<int> position, bool press)
        {
            List<Point2d<int>> positions = new List<Point2d<int>>(1);

            positions.Add(position);

            this.UpdateMultiTouch(positions, press);
        }

        private void UpdateMultiTouch(List<Point2d<int>> positions, bool press)
        {
            List<MultitouchPointerInfo> pointers = new List<MultitouchPointerInfo>(positions.Count);

            for (int i = 0; i < positions.Count; ++i)
            {
                pointers.Add(new MultitouchPointerInfo());
                pointers[i].X = positions[i].X/System.Windows.SystemParameters.PrimaryScreenWidth;
                pointers[i].Y = positions[i].Y/System.Windows.SystemParameters.PrimaryScreenHeight;

                pointers[i].Down = press;
            }

            MultitouchReport report = new MultitouchReport(pointers);
            vMulti.updateMultitouch(report);
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
                // VMulti Init
                this.vMulti = new VMulti();

                if (!this.vMulti.connect())
                {
                    throw new Exception("Failed to connect to VMulti.");
                }

                // Kinect Init
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
                this.shoulderPoint = GetSkeletonDepthPoint(e, JointType.ShoulderLeft);
                //this.elbowPoint = GetSkeletonDepthPoint(e, JointType.ElbowLeft);
                //this.wristPoint = GetSkeletonDepthPoint(e, JointType.WristLeft);

                //get arm length to determine threshold
                //double lenPartOne = Math.Sqrt(Math.Pow(shoulderPoint.X - elbowPoint.X,2) + Math.Pow(shoulderPoint.Y - elbowPoint.Y,2) + Math.Pow(shoulderPoint.Depth - elbowPoint.Depth,2));
                //double lenPartTwo = Math.Sqrt(Math.Pow(elbowPoint.X - wristPoint.X, 2) + Math.Pow(elbowPoint.Y - wristPoint.Y, 2) + Math.Pow(elbowPoint.Depth - wristPoint.Depth, 2));
                //double lenPartThree = Math.Sqrt(Math.Pow(wristPoint.X - handPoint.X, 2) + Math.Pow(wristPoint.Y - handPoint.Y, 2) + Math.Pow(wristPoint.Depth - handPoint.Depth, 2));
                //double armLength = lenPartOne + lenPartTwo + lenPartThree;

                //set click zone threshold as 25mm less than arm length
                //may need to move to own method
                //threshold = Convert.ToInt32(armLength) - 25;
                threshold = 500;

                // *** POINTER CODE, SHOULD BE MOVED TO OWN METHOD WHEN DONE ***
                Point3d<int> ptHandPoint = this.ConvertDepthImagePointToPoint3d(this.handPoint);
                Point3d<int> ptHeadPoint = this.ConvertDepthImagePointToPoint3d(this.headPoint);

                Point3d<int> normalVector = this.CalculateNormalVector(DEPTH_UPPER_LEFT, DEPTH_CENTER, DEPTH_LOWER_RIGHT);
                //Point2d<int> screenPos = this.MapRealspacePointToScreen(ptHeadPoint, ptHandPoint, normalVector);

                //this.iterationCounter++;
                //if (this.iterationCounter % 100 == 0)
                //{
                //    this.pressOnce = false;
                //}

                //if (this.pressOnce == false)
                //{
                //    /*this.UpdateMultiTouch(new Point2d<int>(30, 1060), false);
                //    this.UpdateMultiTouch(new Point2d<int>(30, 1060), true);
                //    this.UpdateMultiTouch(new Point2d<int>(30, 1060), false);*/
                //    this.UpdateMultiTouch(screenPos, true);
                //    this.pressOnce = true;
                //}

                System.Diagnostics.Debug.WriteLine("Hand Pos: " + ptHandPoint.ToString());
                //System.Diagnostics.Debug.WriteLine("Pointer Pos: " + screenPos.ToString());
                // *** END POINTER CODE

                //enter click zone threshold
                

                //Point3d<int> pointerRay = this.CalculateVector(this.handPoint, this.headPoint);
                //System.Diagnostics.Debug.WriteLine(this.ConvertDepthImagePointToPoint3d(this.handPoint).ToString());
                //System.Diagnostics.Debug.WriteLine(pointerRay);
                //System.Diagnostics.Debug.WriteLine(this.ConvertDepthImagePointToPoint3d(this.handPoint).ToString() + " && " + this.ConvertDepthImagePointToPoint3d(this.headPoint).ToString());

                

                using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
                {
                    int minDepth;
                    int maxDepth;
                    int maxY, minY, maxX, minX, centerX = 0, centerY = 0;
                    //int threshold;

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
                            int x, y;
                            fingerPixels = new bool[IMG_WIDTH, IMG_HEIGHT];
                            findInteriorAndContour(interior);
                            List<Tuple<int, int>> filtered_contour = (new ContourCreator(contourPixels)).findContour();
                            //we could probably play around with these parameters alot
                            Tuple<int, int> center = FingerFinder.findPalmCenter(interior, filtered_contour);
                            /*FingerFinder.reduceFingerPoints(FingerFinder.findFingers(filtered_contour, 10, 0.75, center.Item1, center.Item2)).ForEach(i =>
                            {
                                fingerPixels[i.Item1, i.Item2] = true;
                            });*/
                            List<Point2d<int>> fingerPoints = new List<Point2d<int>>(5); 
                            FingerFinder.findFingersByContour(filtered_contour,center.Item1,center.Item2).ForEach(i =>
                            {
                                fingerPixels[i.Item1, i.Item2] = true;
                                x = i.Item1;
                                y = i.Item2;
                                Point3d<int> ptFingerPoint = new Point3d<int>(x, y, handPoint.Depth);
                                Point2d<int> fingerPos = this.MapRealspacePointToScreen(ptHeadPoint, ptFingerPoint, normalVector);
                                fingerPoints.Add(fingerPos);
                            });
                            if (shoulderPoint.Depth - handPoint.Depth > threshold)
                            {
                                System.Diagnostics.Debug.WriteLine("ALERT: Arm is in threshold!");
                                //if single tracked finger; single click
                                //if two tracked fingers; right click
                                //if single tracked finger twice; double click
                                //if two tracked fingers twice; enter scroll lock
                                this.UpdateMultiTouch(fingerPoints, true);
                            }
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
            if (intensityValues == null) return;
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
                    colorPixels[colorPixelIndex++] = 255;
                    colorPixels[colorPixelIndex++] = 0;
                }
                /*else if (contourPixels[x, y])
                {
                    colorPixels[colorPixelIndex++] = 255;
                    colorPixels[colorPixelIndex++] = 0;
                    colorPixels[colorPixelIndex++] = 0;
                }*/
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

        public void Dispose()
        {
            if (this.isInitalized)
            {
                this.vMulti.disconnect();
                this.Sensor.Dispose();
            }
        }
    }
}
