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
            int len = contour.Count - k;
            int v1_x, v1_y, v2_x, v2_y;
            double angle, dist_i, dist_ip, dist_im;
            List<Tuple<int, int>> fingerPoints = new List<Tuple<int, int>>();
            List<double> angles = new List<double>();
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
                        i += 20;
                    }
                }
            }
            FingerValues = angles.ToArray();
            if(static_count % 50 == 0 && FingerValues.Count() > 0 && static_count > 50)
            {
                len = 5;
            }
            static_count++;
            return fingerPoints;
        }

        public static Tuple<int, int> findPalmCenter(List<Tuple<int, int>> interiorPoints, List<Tuple<int, int>> contour)
        {
            double maxDist = 0.0, currentMinDist, next;
            Tuple<int, int> point = interiorPoints.First(), innerPoint, interiorPoint, contourPoint;
            int interiorCount = interiorPoints.Count, contourCount = contour.Count;
            for (int i = 0; i < interiorCount; i+=7 )
            {
                interiorPoint = interiorPoints[i];
                currentMinDist = 10000;
                for(int j=0;j<contourCount; j+=2)
                {
                    contourPoint = contour[j];
                    next = Math.Abs(dist(contourPoint.Item1, contourPoint.Item2, interiorPoint.Item1, interiorPoint.Item2));
                    if (next < currentMinDist)
                    {
                        if (next < maxDist) break;
                        currentMinDist = next;
                        innerPoint = interiorPoint;
                    }
                }
                if (currentMinDist > maxDist)
                {
                    maxDist = currentMinDist;
                    point = interiorPoint;
                }
            }
            return point;
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
