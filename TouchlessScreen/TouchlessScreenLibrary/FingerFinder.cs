using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TouchlessScreenLibrary
{
    public class FingerFinder
    {
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
            for (int i = k; i < len; ++i)
            {
                v1_x = contour[i].Item1 - contour[i - k].Item1;
                v1_y = contour[i].Item2 - contour[i - k].Item2;
                v2_x = contour[i].Item1 - contour[i + k].Item1;
                v2_y = contour[i].Item2 - contour[i + k].Item2;
                angle = Math.Acos(((double)(v1_x * v2_x + v1_y * v2_y)) / (Math.Sqrt(v1_x * v1_x + v1_y * v1_y) + Math.Sqrt(v2_x * v2_x + v2_y * v2_y)));
                if (angle < alpha)
                {
                    dist_i = Math.Sqrt((x_center - contour[i].Item1) * (x_center - contour[i].Item1) + (y_center - contour[i].Item2) * (y_center - contour[i].Item2));
                    dist_ip = Math.Sqrt((x_center - contour[i + k].Item1) * (x_center - contour[i + k].Item1) + (y_center - contour[i + k].Item2) * (y_center - contour[i + k].Item2));
                    dist_im = Math.Sqrt((x_center - contour[i - k].Item1) * (x_center - contour[i - k].Item1) + (y_center - contour[i - k].Item2) * (y_center - contour[i - k].Item2));
                    if (dist_i > dist_ip && dist_i > dist_im)
                    {
                        fingerPoints.Add(new Tuple<int, int>(contour[i].Item1, contour[i].Item2));
                    }
                }
            }
            return fingerPoints;
        }
    }
}
