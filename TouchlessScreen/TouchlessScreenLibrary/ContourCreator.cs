using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TouchlessScreenLibrary
{
    public class ContourCreator
    {
        private bool[,] contourPixels;
        private int width, height, firstX, firstY, nextX, nextY, currX, currY, dirX, dirY;
        private static int[,] rotations;

        static ContourCreator()
        {
            rotations = Direction.getDirections();
        }

        /// <summary>
        /// Creates a ContourCreator backed by the array of pixels which form the rough contour
        /// </summary>
        /// <param name="contourPixels"></param>
        public ContourCreator(bool[,] contourPixels)
        {
            this.contourPixels = contourPixels;
            width = contourPixels.GetLength(0);
            height = contourPixels.GetLength(1);
        }
        private void findCornerPoint()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (contourPixels[x, y])
                    {
                        firstX = x;
                        firstY = y;
                        return;
                    }
                }
            }
        }

        private bool findNext()
        {
            int dx = (int)(dirX - currX);
            int dy = (int)(dirY - currY);
            const int numDirs = 8;
            int startDir = Direction.GetDirectionIndex(dx, dy);
            int dirIndex;
            for (int j = startDir; j < startDir + numDirs; j++)
            {
                dirIndex = j % numDirs;
                int x = currX + rotations[0, dirIndex];
                int y = currY + rotations[1, dirIndex];
                if (x < 0 || y < 0 || x >= width || y >= height) return false;
                if (contourPixels[x, y])
                {
                    nextX = x;
                    nextY = y;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Finds the contour from the backed list of points
        /// </summary>
        /// <returns></returns>
        public List<Tuple<int, int>> findContour()
        {
            List<Tuple<int, int>> contour;
            contour = new List<Tuple<int, int>>();
            findCornerPoint();
            currX = dirX = firstX;
            currY = firstY;
            dirY = firstY - 1;
            contour.Add(new Tuple<int, int>(currX, currY));
            bool found;

            do
            {
                found = findNext();
                if (found)
                {
                    dirX = currX;
                    dirY = currY;
                    contour.Add(new Tuple<int, int>(nextX, nextY));
                    currX = nextX;
                    currY = nextY;
                }
            } while (found && (nextX != firstX || nextY != firstY));

            return contour;
        }

    }  


}
