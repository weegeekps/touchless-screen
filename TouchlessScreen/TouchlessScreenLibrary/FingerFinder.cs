using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TouchlessScreenLibrary
{
    public class FingerFinder
    {
        public static double [] FingerValues;
        static int static_count = 0;
        private delegate double distanceFunction(int x1, int y1, int x2, int y2);
        private static distanceFunction dist = (x1, y1, x2, y2) => Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y2) * (y2 - y1));
        private delegate double distanceFunction3D(int x1, int y1,int z1, int x2, int y2,int z2);
        private static distanceFunction3D dist3D = (x1, y1, z1, x2, y2, z2) => Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y2) * (y2 - y1) + (z2 - z1) * (z2 - z1));
        private static int[,] directions;

        static FingerFinder()
        {
            directions = Direction.getDirections();
            missingDuration = new int[5];
            fingerSamples = new Tuple<int, int>[N_SAMPLES, 5];
            for(int i=0; i<N_SAMPLES; i++)
            {
                for (int j = 0; j < 5; j++) fingerSamples[i, j] = new Tuple<int, int>(-1, -1);
            }
        }

        /// <summary>
        /// Uses k-Curvature algorithm to find the finger tip points
        /// </summary>
        /// <param name="contour"></param>
        /// <param name="k"></param>
        /// <param name="alpha"></param>
        /// <param name="x_center"></param>
        /// <param name="y_center"></param>
        /// <returns></returns>
        public static List<Tuple<int, int>> findFingers(List<Tuple<int, int>> contour, int k, double alpha, int x_center, int y_center)
        {
            contour = filterContour(contour);
            int len = contour.Count - k;
            int v1_x, v1_y, v2_x, v2_y;
            double angle, dist_i, dist_ip, dist_im;
            List<Tuple<int, int>> fingerPoints = new List<Tuple<int, int>>();
            List<double> angles = new List<double>(Math.Max(len,1));
            for (int i = k; i < len; ++i)
            {
                v1_x = contour[i].Item1 - contour[i - k].Item1;
                v1_y = contour[i].Item2 - contour[i - k].Item2;
                v2_x = contour[i].Item1 - contour[i + k].Item1;
                v2_y = contour[i].Item2 - contour[i + k].Item2;
                angle = Math.Acos(((double)(v1_x * v2_x + v1_y * v2_y)) / (Math.Sqrt(v1_x * v1_x + v1_y * v1_y) * Math.Sqrt(v2_x * v2_x + v2_y * v2_y)));
                angles.Add(angle);
                if (angle < alpha)
                {
                    dist_i = Math.Sqrt((x_center - contour[i].Item1) * (x_center - contour[i].Item1) + (y_center - contour[i].Item2) * (y_center - contour[i].Item2));
                    dist_ip = Math.Sqrt((x_center - contour[i + k].Item1) * (x_center - contour[i + k].Item1) + (y_center - contour[i + k].Item2) * (y_center - contour[i + k].Item2));
                    dist_im = Math.Sqrt((x_center - contour[i - k].Item1) * (x_center - contour[i - k].Item1) + (y_center - contour[i - k].Item2) * (y_center - contour[i - k].Item2));
                    if (dist_i > dist_ip && dist_i > dist_im )
                    {
                        fingerPoints.Add(new Tuple<int, int>(contour[i].Item1, contour[i].Item2));
                        //i += 3;
                    }
                }
            }
            FingerValues = angles.ToArray();
            if(static_count % 50 == 0 && FingerValues.Count() > 0 && static_count > 50)
            {
                string[] output = new string[len];
                for (int i = 0; i < k; i++) output[i] = contour[i].Item1 + "," + contour[i].Item2 + ",NAN";
                for (int i = k; i < len; ++i) output[i] = contour[i].Item1 + "," + contour[i].Item2 + ","+angles[i-k];
                System.IO.File.WriteAllLines("contour" + static_count + ".csv", output);
                len = 5;
            }
            static_count++;
            return fingerPoints;
        }

        public static Tuple<int, int> findPalmCenter(List<Tuple<int, int>> interiorPoints, List<Tuple<int, int>> contour)
        {
            int len = contour.Count;
            int center_x = contour.Select(i => i.Item1).Sum() / len;
            int center_y = contour.Select(i => i.Item2).Sum() / len;
            Tuple<int,int> point = new Tuple<int,int>(center_x,center_y);
            return point;
        }

        public static Point3d<int> findPalmCenter3D(List<Point3d<int>> contour, DepthImagePixel[] depthImagePixels)
        {
            int len = contour.Count;
            int center_x = contour.Select(i => i.X).Sum() / len;
            int center_y = contour.Select(i => i.Y).Sum() / len;
            int center_z = depthImagePixels[center_x * 640 + center_y].Depth;
            return new Point3d<int>(center_x, center_y, center_z);
        }

        public static List<Tuple<int,int>> findFingersByAngles(int[] [] lengths)
        {
            int[] fingerArr = lengths.OrderBy(i => i.Sum()).First();
            
            return null; 
        }

        private static List<Tuple<int,int>> filterContour(List<Tuple<int,int>> contour)
        {
            //List<Tuple<int, int>> filtered = contour.Where((p, i) => i % 4 == 0).ToList();
            int j = 0;
            List<Tuple<int, int>> filtered = contour.Where((p, i) => {
                bool retVal = i == 0 || dist(p.Item1, p.Item2, contour[j].Item1, contour[j].Item2) > 2.0;
                if (retVal) j = i;
                return retVal;
            }).ToList();
            return filtered;
        }

        private static List<Point3d<int>> filterContour3D(List<Point3d<int>> contour)
        {
            //List<Tuple<int, int>> filtered = contour.Where((p, i) => i % 4 == 0).ToList();
            int j = 0;
            List<Point3d<int>> filtered = contour.Where((p, i) =>
            {
                bool retVal = i == 0 || dist3D(p.X,p.Y,p.Z,contour[j].X,contour[j].Y,contour[j].Z) > 4.0;
                if (retVal) j = i;
                return retVal;
            }).ToList();
            return filtered;
        }

        private static int N_SAMPLES = 3;
        private static Tuple<int, int>[,] fingerSamples;
        private static  int[] missingDuration;
        /// <summary>
        /// Averages the finger positions N_Sample Times
        /// </summary>
        /// <param name="next"></param>
        /// <returns></returns>
        public static List<Tuple<int, int>> getNextFingerPositions(List<Tuple<int, int>> next)
        {
            
            List<Tuple<int, int>> values = new List<Tuple<int, int>>(5);
            int missing;

            next = next.Where((p, k) => k < 5).OrderBy(t => t.Item1).ToList();
            int nextLen = next.Count, i;

            for (i = 0; i < nextLen; i++)
            {
                int xAvg = next[i].Item1;
                int yAvg = next[i].Item2;
                missingDuration[i] = 0;
                missing = 0;
                for (int j = 0; j < N_SAMPLES; j++)
                {
                    if (fingerSamples[j, i].Item1 < 0 || fingerSamples[j, i].Item2 < 0) 
                    {
                        ++missing;
                        continue;
                    }
                    xAvg += fingerSamples[j,i].Item1;
                    yAvg += fingerSamples[j, i].Item2;
                }
                xAvg /= (N_SAMPLES+1-missing);
                yAvg /= (N_SAMPLES+1-missing);
                values.Add(new Tuple<int, int>(xAvg, yAvg));
            }
            for(; i<5; i++)
            {
                if (++missingDuration[i] < N_SAMPLES)
                {
                    int xAvg = 0;
                    int yAvg = 0;
                    missing = 0;
                    for (int j = 0; j < N_SAMPLES; j++)
                    {
                        if (fingerSamples[j, i].Item1 < 0 || fingerSamples[j, i].Item2 < 0)
                        {
                            missing++;
                            continue;
                        }
                        xAvg += fingerSamples[j, i].Item1;
                        yAvg += fingerSamples[j, i].Item2;
                    }
                    if (N_SAMPLES - missing > 0)
                    {
                        xAvg /= (N_SAMPLES - missing);
                        yAvg /= (N_SAMPLES - missing);
                        values.Add(new Tuple<int, int>(xAvg, yAvg));
                    }
                }
            }
            for (i = 0; i < N_SAMPLES-1; i++ )
            {
                for (int j = 0; j < 5; j++) fingerSamples[i, j] = fingerSamples[i + 1, j]; 
            }
            for (i = 0; i < nextLen; i++) fingerSamples[N_SAMPLES - 1, i] = next[i];
            for (; i < 5; i++) fingerSamples[N_SAMPLES - 1, i] = new Tuple<int, int>(-1, -1);
            return values;
        }
        
        public static List<Tuple<int,int>> findFingersByContour(List<Tuple<int,int>> contour, int center_x, int center_y)
        {
            List<Tuple<int, int>> fingers = new List<Tuple<int, int>>();
            contour = filterContour(contour);
            int len = contour.Count, x, y, max_x = center_x, max_y = center_y, min_x = center_x, min_y = center_y;
            string[] output = new string[len];
            for (int i = 0; i < len; ++i) output[i] = contour[i].Item1 + "," + contour[i].Item2;
            System.IO.File.WriteAllLines("contour" + static_count + ".csv", output);
            double maxDist = 0, minDist = double.MaxValue;
            double currDist;
            bool findMax = true;
            int max_i = 0;
            for (int i = 0; i < len; i++ )
            {
                x = contour[i].Item1;
                y = contour[i].Item2;
                currDist = dist(x, y, center_x, center_y);
                if(findMax)
                {
                    if (currDist > maxDist) 
                    {
                        maxDist = currDist;
                        max_x = x;
                        max_y = y;
                    }
                    else if(currDist < 0.8 * maxDist)
                    {
                        fingers.Add(new Tuple<int, int>(max_x, max_y));
                        findMax = false;
                        max_i = i;
                        maxDist = 0;
                    }
                }
                else
                {
                    if(currDist < minDist)
                    {
                        minDist = currDist;
                        min_x = x;
                        min_y = y;
                    }
                    else if(currDist > 1.2*minDist && i-max_i >= 6)
                    {
                        findMax = true;
                        minDist = double.MaxValue;
                    }
                }
            }
            if (fingers.Count > 5) fingers = fingers.Where((p, i) => i < 5).ToList();
            return fingers;
        }

        public static List<Point3d<int>> findFingersByContour3D(List<Point3d<int>> contour, int center_x, int center_y,int center_z)
        {
            List<Point3d<int>> fingers = new List<Point3d<int>>();
            contour = filterContour3D(contour);
            int len = contour.Count, x, y,z, max_x = center_x, max_y = center_y, min_x = center_x, min_y = center_y,max_z=center_z,min_z=center_z;
            string[] output = new string[len];
            for (int i = 0; i < len; ++i) output[i] = contour[i].X + "," + contour[i].Y+","+contour[i].Z;
            System.IO.File.WriteAllLines("contour3d" + static_count + ".csv", output);
            double maxDist = 0, minDist = double.MaxValue;
            double currDist;
            bool findMax = true;
            int max_i = 0;
            for (int i = 0; i < len; i++)
            {
                x = contour[i].X;
                y = contour[i].Y;
                z = contour[i].Z;
                currDist = dist3D(x, y,z, center_x, center_y,center_z);
                if (findMax)
                {
                    if (currDist > maxDist)
                    {
                        maxDist = currDist;
                        max_x = x;
                        max_y = y;
                        max_z = z;
                    }
                    else if (currDist < 0.8 * maxDist)
                    {
                        fingers.Add(new Point3d<int>(x,y,x));
                        findMax = false;
                        max_i = i;
                        maxDist = 0;
                    }
                }
                else
                {
                    if (currDist < minDist)
                    {
                        minDist = currDist;
                        min_x = x;
                        min_y = y;
                        min_z = z;
                    }
                    else if (currDist > 1.2 * minDist && i - max_i >= 3)
                    {
                        findMax = true;
                        minDist = double.MaxValue;
                    }
                }
            }
            if (fingers.Count > 5) fingers = fingers.Where((p, i) => i < 5).ToList();
            return fingers;
        }
        
        public static List<Tuple<int, int>> reduceFingerPoints(List<Tuple<int, int>> fingerPoints)
        {
            int len = fingerPoints.Count;
            int next_x, next_y, center_x = -1, center_y = -1,pointCount=0;
            double dist;
            List<Tuple<int, int>> reducedList = new List<Tuple<int, int>>();
            for (int i = 0; i < len; i++)
            {
                next_x = fingerPoints[i].Item1;
                next_y = fingerPoints[i].Item2;
                if(pointCount == 0)
                {
                    center_x = next_x;
                    center_y = next_y;
                    pointCount++;
                }
                else
                {
                    dist = Math.Sqrt((center_x - next_x) * (center_x - next_x) + (center_y - next_y) * (center_y - next_y));
                    if(Math.Abs(dist) <= 20)
                    {
                        //update center averaging in new point
                        center_x = (center_x * pointCount + next_x) / (pointCount + 1);
                        center_y = (center_y * pointCount + next_y) / (pointCount + 1);
                        pointCount++;
                    }
                    else
                    {
                        reducedList.Add(new Tuple<int, int>(center_x, center_y));
                        pointCount = 0; //reset and look for next finger
                    }
                }
            }
            return reducedList;
        }

    }
}
