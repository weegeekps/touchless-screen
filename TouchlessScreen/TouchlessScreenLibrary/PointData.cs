using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TouchlessScreenLibrary
{
    public sealed class PointData
    {
        #region Private Members
        private DepthImagePoint mDepthImagePoint;
        private bool mDepthImagePointDefined;
        private KinectSensor mSensor;
        #endregion

        #region Internal Ctors
        public PointData(SkeletonPoint skeletonPoint, ref KinectSensor sensor)
        {
            this.mDepthImagePointDefined = false;
            this.SkeletonPoint = skeletonPoint;
            this.mSensor = sensor;
        }

        public PointData(SkeletonPoint skeletonPoint, DepthImagePoint depthImagePoint)
        {
            this.SkeletonPoint = skeletonPoint;
            this.mDepthImagePoint = depthImagePoint;
            this.mDepthImagePointDefined = true;
        }
        #endregion

        #region Private Methods
        private DepthImagePoint GetDepthPointMapping()
        {
            if (this.mSensor != null)
            {
                this.mDepthImagePoint = this.mSensor.CoordinateMapper.MapSkeletonPointToDepthPoint(this.SkeletonPoint, DepthImageFormat.Resolution640x480Fps30);
                this.mDepthImagePointDefined = true;
            }

            return this.mDepthImagePoint;
        }
        #endregion

        #region Public Methods & Properties
        public SkeletonPoint SkeletonPoint { get; private set; }
        public DepthImagePoint DepthImagePoint
        {
            get
            {
                if (!this.mDepthImagePointDefined)
                {
                    return this.GetDepthPointMapping();
                }

                return this.mDepthImagePoint;
            }
        }
        #endregion
    }
}
