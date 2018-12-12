using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Numerics;
using Microsoft.Kinect;
using Newtonsoft.Json;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace KinectPointingAPI.Controllers
{
    public class ObjectDetectionController : ApiController
    {
        private KinectSensor kinectSensor;
        private CoordinateMapper coordinateMapper;
        private FrameDescription colorFrameDescription;


        public ObjectDetectionController()
        {
            this.kinectSensor = KinectSensor.GetDefault();
            this.coordinateMapper = kinectSensor.CoordinateMapper;
            this.colorFrameDescription = kinectSensor.ColorFrameSource.FrameDescription;
        }

        // GET api/values
        public string Get()
        {
            ColorFrameReader colorFrameReader = kinectSensor.ColorFrameSource.OpenReader();

            kinectSensor.Open();
            while (!kinectSensor.IsAvailable)
            {
                Thread.Sleep(5);
            }

            bool dataReceived = false;
            Bitmap colorData = null;
            while (!dataReceived)
            {
                ColorFrame colorFrame = colorFrameReader.AcquireLatestFrame();
                if (colorFrame != null)
                {
                    dataReceived = true;
                    colorData = this.GetColorData(colorFrame);
                }
            }

            return "";
        }

        private Bitmap GetColorData(ColorFrame colorFrame)
        {
            int width = this.colorFrameDescription.Width;
            int height = this.colorFrameDescription.Height;
            int pixelDataLength = Convert.ToInt32(this.colorFrameDescription.LengthInPixels);

            // Grab raw color data from frame
            byte[] rawColorData = new byte[pixelDataLength];
            colorFrame.CopyConvertedFrameDataToArray(rawColorData, ColorImageFormat.Rgba);
            
            // Convert raw image data into bitmap
            Bitmap bmap = new Bitmap(width, height, PixelFormat.Format32bppRgb);
            BitmapData bmapdata = bmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                bmap.PixelFormat);
            IntPtr ptr = bmapdata.Scan0;
            Marshal.Copy(rawColorData, 0, ptr, pixelDataLength);
            bmap.UnlockBits(bmapdata);
            return bmap;
        }
    }
}
