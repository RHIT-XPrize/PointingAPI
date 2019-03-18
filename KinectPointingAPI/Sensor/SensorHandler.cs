using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
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
            if (HttpContext.Current.Session["Sensor"] == null)
            {
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
            }

            return (KinectSensor)HttpContext.Current.Session["Sensor"];
        }

        public static ColorFrame GetColorFrame()
        {
            KinectSensor currSensor = GetSensor();

            ColorFrame frame = null;
            using (ColorFrameReader frameReader = currSensor.ColorFrameSource.OpenReader())
            {
                frame = frameReader.AcquireLatestFrame();
            }
            return frame;
        }

        public static BodyFrame GetBodyFrame()
        {
            KinectSensor currSensor = GetSensor();

            BodyFrame frame = null;
            using (BodyFrameReader frameReader = currSensor.BodyFrameSource.OpenReader())
            {
                frame = frameReader.AcquireLatestFrame();
            }
            return frame;
        }

        public static DepthFrame GetDepthFrame()
        {
            KinectSensor currSensor = GetSensor();

            DepthFrame frame = null;
            using (DepthFrameReader frameReader = currSensor.DepthFrameSource.OpenReader())
            {
                frame = frameReader.AcquireLatestFrame();
            }
            return frame;
        }
    }
}