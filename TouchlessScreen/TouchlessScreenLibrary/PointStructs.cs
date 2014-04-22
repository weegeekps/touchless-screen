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

        public override string ToString()
        {
            return string.Format("X:{0} Y:{1}", this.X, this.Y);
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

        public bool Equals(Point3d<T> obj)
        {
            if (this.X.Equals(obj.X) && this.Y.Equals(obj.Y) && this.Z.Equals(obj.Z))
            {
                return true;
            }

            return false;
        }

        public override string ToString()
        {
            return string.Format("X:{0} Y:{1} Z:{2}", this.X, this.Y, this.Z);
        }
    }
}
