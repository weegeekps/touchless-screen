using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TouchlessScreenLibrary
{
    class Direction
    {
        private Direction()
        {

        }

        public static int[,] getDirections()
        {
            int[,] directions = new int[2, 8];
            directions[0, 0] = 1;
            directions[1, 0] = 0;

            directions[0, 1] = 1;
            directions[1, 1] = 1;

            directions[0, 2] = 0;
            directions[1, 2] = 1;

            directions[0, 3] = -1;
            directions[1, 3] = 1;

            directions[0, 4] = -1;
            directions[1, 4] = 0;

            directions[0, 5] = -1;
            directions[1, 5] = -1;

            directions[0, 6] = 0;
            directions[1, 6] = -1;

            directions[0, 7] = 1;
            directions[1, 7] = -1;
            return directions;
        }

        /// <summary>
        /// Copied from Candesent NUI
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <returns></returns>
        public static int GetDirectionIndex(int dx, int dy)
        {
            if (dy == -1)
            {
                if (dx == -1)
                {
                    return 6;
                }
                if (dx == 0)
                {
                    return 7;
                }
                if (dx == 1)
                {
                    return 0;
                }
            }
            if (dy == 0)
            {
                if (dx == -1)
                {
                    return 5;
                }
                if (dx == 1)
                {
                    return 1;
                }
            }
            if (dy == 1)
            {
                if (dx == -1)
                {
                    return 4;
                }
                if (dx == 0)
                {
                    return 3;
                }
                if (dx == 1)
                {
                    return 2;
                }
            }
            return 0;
        }
    }
    
}
