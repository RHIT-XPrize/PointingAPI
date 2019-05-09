 using Microsoft.Kinect;
using System.Threading;
using System.Web;

namespace KinectPointingAPI.Sensor
{
    public static class SensorHandler
    {
        private static int CONNECT_TIMEOUT_MS = 20000;

        public static KinectSensor GetSensor()
        {
            KinectSensor sensor = null;
            sensor = KinectSensor.GetDefault();
            sensor.Open();

            int ms_slept = 0;
            while (!sensor.IsAvailable)
            {
                Thread.Sleep(5);
                ms_slept += 5;
                if (ms_slept >= CONNECT_TIMEOUT_MS)
                {
                    System.Environment.Exit(-1);
                }
            }

            HttpContext.Current.Session["Sensor"] = sensor;
            HttpContext.Current.Session["ColorFrameReader"] = sensor.ColorFrameSource.OpenReader();
            HttpContext.Current.Session["BodyFrameReader"] = sensor.BodyFrameSource.OpenReader();
            HttpContext.Current.Session["DepthFrameReader"] = sensor.DepthFrameSource.OpenReader();
            HttpContext.Current.Session["CoordinateMapper"] = sensor.CoordinateMapper;

            return (KinectSensor)HttpContext.Current.Session["Sensor"];
        }

        public static ColorFrame GetColorFrame()
        {
            GetSensor();
            return ((ColorFrameReader)HttpContext.Current.Session["ColorFrameReader"]).AcquireLatestFrame();
        }

        public static BodyFrame GetBodyFrame()
        {
            GetSensor();
            return ((BodyFrameReader)HttpContext.Current.Session["BodyFrameReader"]).AcquireLatestFrame();
        }

        public static DepthFrame GetDepthFrame()
        {
            GetSensor();
            return ((DepthFrameReader)HttpContext.Current.Session["DepthFrameReader"]).AcquireLatestFrame();
        }
    }
}
