using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TouchlessScreenLibrary;


namespace Microsoft.Samples.Kinect.DepthBasics
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Bitmap that will hold color information
        /// </summary>
        private WriteableBitmap colorBitmap;

        /// <summary>
        /// Intermediate storage for the depth data received from the camera
        /// </summary>
        private DepthImagePixel[] depthPixels;

        /// <summary>
        /// Intermediate storage for the depth data converted to color
        /// </summary>
        private byte[] colorPixels;

        /// <summary>
        /// Width of output drawing
        /// </summary>
        private const float RenderWidth = 640.0f;
        private const int IMG_WIDTH = (int)RenderWidth;
        /// <summary>
        /// Height of our output drawing
        /// </summary>
        private const float RenderHeight = 480.0f;
        private const int IMG_HEIGHT = (int)RenderHeight;
        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of body center ellipse
        /// </summary>
        private const double BodyCenterThickness = 10;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Brush used to draw skeleton center point
        /// </summary>
        private readonly Brush centerPointBrush = Brushes.Blue;

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        private TouchlessScreen touchlessScreen;

        /// <summary>
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            this.touchlessScreen = TouchlessScreen.Instance;
        }

        private bool[,] handPixels;

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            this.touchlessScreen.Initialize();

            handPixels = new bool[IMG_WIDTH, IMG_HEIGHT];
            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Display the drawing using our image control
            Image.Source = this.imageSource;

            // Allocate space to put the depth pixels we'll receive
            this.depthPixels = new DepthImagePixel[this.touchlessScreen.Sensor.DepthStream.FramePixelDataLength];

            // Allocate space to put the color pixels we'll create
            this.colorPixels = new byte[this.touchlessScreen.Sensor.DepthStream.FramePixelDataLength * sizeof(int)];

            // This is the bitmap we'll display on-screen
            this.colorBitmap = new WriteableBitmap(this.touchlessScreen.Sensor.DepthStream.FrameWidth, this.touchlessScreen.Sensor.DepthStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

            // Set the image we display to point to the bitmap where we'll put the image data
            this.Image.Source = this.colorBitmap;

            // Add an event handler to be called whenever there is new color frame data
            this.touchlessScreen.Sensor.AllFramesReady += this.SensorDepthFrameReady;

            if (!this.touchlessScreen.TryStart())
            {
                // TODO: Do some error handling here.
            }
 
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.touchlessScreen.Sensor)
            {
                this.touchlessScreen.Sensor.Stop();
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's AllFramesReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorDepthFrameReady(object sender, AllFramesReadyEventArgs e)
        {
            DepthImagePoint depthPoint = this.touchlessScreen.GetSkeletonDepthPoint(e, JointType.HandLeft);

            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                int minDepth;
                int maxDepth;
                int maxY, minY, maxX, minX;

                if (depthFrame != null)
                {
                    // Copy the pixel data from the image to a temporary array
                    depthFrame.CopyDepthImagePixelDataTo(this.depthPixels);
                    if (depthPoint.Depth == 0)
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
                        minDepth = Math.Max(depthFrame.MinDepth, depthPoint.Depth - 40);
                        maxDepth = Math.Min(depthFrame.MaxDepth, depthPoint.Depth + 40);
                        minX = Math.Max(0, depthPoint.X - 70);
                        maxX = Math.Min(depthFrame.Height, depthPoint.X + 70);
                        minY = Math.Max(0, depthPoint.Y - 70);
                        maxY = Math.Min(depthFrame.Height, depthPoint.Y + 90);
                    }
                    // Convert the depth to RGB
                    int colorPixelIndex = 0;

                    for (int i = 0; i < this.depthPixels.Length; ++i)
                    {
                        // Get the depth for this pixel
                        short depth = depthPixels[i].Depth;
                        int x = i % 640;
                        int y = i / 640;
                        // To convert to a byte, we're discarding the most-significant
                        // rather than least-significant bits.
                        // We're preserving detail, although the intensity will "wrap."
                        // Values outside the reliable depth range are mapped to 0 (black).

                        // Note: Using conditionals in this loop could degrade performance.
                        // Consider using a lookup table instead when writing production code.
                        // See the KinectDepthViewer class used by the KinectExplorer sample
                        // for a lookup table example.
                        byte intensity = (byte)(depth >= minDepth && depth <= maxDepth && y > minY && y < maxY && x > minX && x < maxX ? depth : 0);
                        handPixels[x, y] = intensity != 0;
                        // Write out blue byte
                        this.colorPixels[colorPixelIndex++] = intensity;

                        // Write out green byte
                        this.colorPixels[colorPixelIndex++] = intensity;

                        // Write out red byte                        
                        this.colorPixels[colorPixelIndex++] = intensity;

                        // We're outputting BGR, the last byte in the 32 bits is unused so skip it
                        // If we were outputting BGRA, we would write alpha here.
                        ++colorPixelIndex;
                    }
                    if (depthPoint.Depth != 0) findInteriorAndContour();
                    // Write the pixel data into our bitmap
                    this.colorBitmap.WritePixels(
                        new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                        this.colorPixels,
                        this.colorBitmap.PixelWidth * sizeof(int),
                        0);
                    /*                  this.colorBitmap.WritePixels(
                      new Int32Rect(minX, minY, maxX, maxY),
                      this.colorPixels,
                      (maxX-minX) * sizeof(int),
                      minX); */
                }
            }
        }

        /// <summary>
        /// Seperates the hand pixels into interior pixels and contour pixels
        /// </summary>
        public void findInteriorAndContour()
        {
            List<Tuple<int, int>> points = new List<Tuple<int, int>>();
            List<Tuple<int, int>> contour = new List<Tuple<int, int>>();
            List<Tuple<int, int>> interior = new List<Tuple<int, int>>();
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
                    contour.Add(point);
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
                    if (conections >= 7) interior.Add(point);
                    else contour.Add(point);
                }
            }

        }

        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void ButtonScreenshotClick(object sender, RoutedEventArgs e)
        {
            if (null == this.touchlessScreen.Sensor)
            {
                
                return;
            }

            // create a png bitmap encoder which knows how to save a .png file
            BitmapEncoder encoder = new PngBitmapEncoder();

            // create frame from the writable bitmap and add to encoder
            encoder.Frames.Add(BitmapFrame.Create(this.colorBitmap));

            string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

            string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            string path = Path.Combine(myPhotos, "KinectSnapshot-" + time + ".png");

            // write the new file to disk
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Create))
                {
                    encoder.Save(fs);
                }

                this.statusBarText.Text = string.Format(CultureInfo.InvariantCulture, "{0} {1}", "Screenshot Taken", path);
            }
            catch (IOException)
            {
                this.statusBarText.Text = string.Format(CultureInfo.InvariantCulture, "{0} {1}", "Screenshot Write Failed", path);
            }
        }

        /// <summary>
        /// Handles the checking or unchecking of the near mode combo box
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        //private void CheckBoxNearModeChanged(object sender, RoutedEventArgs e)
        //{
        //    if (this.sensor != null)
        //    {
        //        // will not function on non-Kinect for Windows devices
        //        try
        //        {
        //            if (this.checkBoxNearMode.IsChecked.GetValueOrDefault())
        //            {
        //                this.sensor.DepthStream.Range = DepthRange.Near;
        //            }
        //            else
        //            {
        //                this.sensor.DepthStream.Range = DepthRange.Default;
        //            }
        //        }
        //        catch (InvalidOperationException)
        //        {
        //        }
        //    }
        //}
    }
}