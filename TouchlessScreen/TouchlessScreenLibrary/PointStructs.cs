namespace TouchlessScreenLibrary
{
    public struct Point2d<T>
    {
        public T X;
        public T Y;

        public Point2d(T x, T y)
        {
            this.X = x;
            this.Y = y;
        }

        public Point2d(Point3d<T> point3d)
        {
            this.X = point3d.X;
            this.Y = point3d.Y;
        }
    }

    public struct Point3d<T>
    {
        public T X;
        public T Y;
        public T Z;

        public Point3d(T x, T y, T z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public Point3d(Point2d<T> point2d, T z)
        {
            this.X = point2d.X;
            this.Y = point2d.Y;
            this.Z = z;
        }
    }
}
