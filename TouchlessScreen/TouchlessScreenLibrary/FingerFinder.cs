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
        private static int[,] directions;

        static FingerFinder()
        {
            directions = Direction.getDirections();
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
                //System.IO.File.WriteAllLines("contour" + static_count + ".csv", output);
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

        
        
        public static List<Tuple<int,int>> findFingersByContour(List<Tuple<int,int>> contour, int center_x, int center_y)
        {
            List<Tuple<int, int>> fingers = new List<Tuple<int, int>>();
            contour = filterContour(contour);
            int len = contour.Count, x, y, max_x = center_x, max_y = center_y, min_x = center_x, min_y = center_y;
            string[] output = new string[len];
            for (int i = 0; i < len; ++i) output[i] = contour[i].Item1 + "," + contour[i].Item2;
            //System.IO.File.WriteAllLines("contour" + static_count + ".csv", output);
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
                        int nidx = (i - 30) % len;
                        if(nidx <0) nidx += len;
                        int v1_x = contour[i].Item1 - contour[nidx].Item1;
                        int v1_y = contour[i].Item2 - contour[nidx].Item2;
                        int v2_x = contour[i].Item1 - contour[(i + 30) % len].Item1;
                        int v2_y = contour[i].Item2 - contour[(i + 30) % len].Item2;
                        double angle = Math.Acos(((double)(v1_x * v2_x + v1_y * v2_y)) / (Math.Sqrt(v1_x * v1_x + v1_y * v1_y) * Math.Sqrt(v2_x * v2_x + v2_y * v2_y)));
                        /*if(angle < 40.0)*/ fingers.Add(new Tuple<int, int>(max_x, max_y));
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

        public static int[][] handAngleArray(int start_x, int start_y, bool[,] handPoints)
        {
            const int numDirs = 8;
            const int ninetyDegRot = numDirs/2;
            const int blank_threshold = 10;
            int blanks;
            bool[] done = new bool[numDirs];
            int[][] lengths = new int[numDirs][];
            int x,y, xDir,yDir, rotatedXDir,rotatedYDir, next_x_end,next_y_end,next_x_start,next_y_start,count, next_len, xv, yv,j;
            const int width = TouchlessScreen.IMG_WIDTH, height = TouchlessScreen.IMG_HEIGHT;
            for (int i = 0; i < numDirs /2; ++i)
            {
                //get next direction to find lines in
                xDir = directions[0,i];
                yDir = directions[1,i];
                //get the directions rotated by 90 degrees
                rotatedXDir = directions[0,(i+ninetyDegRot) ];
                rotatedYDir = directions[1,(i+ninetyDegRot) ];
                blanks = 0;
                next_x_end = next_x_start = start_x;
                next_y_end = next_y_start = start_y;
                next_len = 0;
                count = 0;
                for(x=start_x, y=start_y; blanks < blank_threshold && x >=0 && y>=0 && x<width&& y<height; x+=rotatedXDir,y+=rotatedYDir)
                {
                    count++;
                    if (!handPoints[x, y]) blanks++;
                }
                next_len = count;
                next_x_end += rotatedXDir * count;
                next_y_end += rotatedYDir * count;

                count = 0;
                blanks = 0;
                for (x = start_x, y = start_y; blanks < blank_threshold && x >= 0 && y >= 0 && x < width && y < height; x -= rotatedXDir, y -= rotatedYDir)
                {
                    count++;
                    if (!handPoints[x, y]) blanks++;
                }
                next_len += count;
                next_x_start -= rotatedXDir * count;
                next_y_start -= rotatedYDir * count;
                lengths[i] = new int[next_len];
                lengths[i+ninetyDegRot] = new int[next_len];
                for (x = next_x_start, y = next_y_start; x <= next_x_end && y <= next_y_end; x+=rotatedXDir,y+=rotatedYDir )
                {
                    blanks = 0;
                    for (j = 0, xv = x, yv = y; blanks < blank_threshold && xv >= 0 && yv >= 0 && xv < width && yv < height; xv += xDir, yv += yDir)
                    {
                        lengths[i][j]++;
                    }
                    for (j = 0, xv = x, yv = y; blanks < blank_threshold && xv >= 0 && yv >= 0 && xv < width && yv < height; xv -= xDir, yv -= yDir)
                    {
                        lengths[i+ninetyDegRot][j]++;
                    }
                }
            }
            return lengths;
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
