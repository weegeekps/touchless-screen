using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TouchlessScreenLibrary
{
    public sealed class ScreenPlane
    {
        private ScreenPlane(int width, int height)
        {
            this.Width = width;
            this.Height = height;
        }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public Point3d<int> NormalVector { get; private set; }

        public static ScreenPlane Create(int width, int height)
        {
            ScreenPlane screen = new ScreenPlane(width, height);

            // Calculate Vector A

            // Calculate Vector B

            // Vector C
            Point3d<int> centerPoint = new Point3d<int>(0, 0, 0);

            throw new NotImplementedException();
        }
    }
}
