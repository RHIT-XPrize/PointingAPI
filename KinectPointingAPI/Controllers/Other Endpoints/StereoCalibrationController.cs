using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Results;
using System.Web.SessionState;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HRC_Datatypes;
using KinectPointingAPI.Sensor;
using Microsoft.Kinect;
using Newtonsoft.Json.Linq;

namespace KinectPointingAPI.Controllers
{
    public class StereoCalibrationControllor : ApiController
    {
        private static int TIMEOUT_MS = 30000;

        [HttpPost]
        [Route("api/StereoCalibrate")]
        public JsonResult<Dictionary<string, double>> Post()
        {
            KinectSensor kinectSensor = SensorHandler.GetSensor();

            int time_slept = 0;
            while (!kinectSensor.IsAvailable)
            {
                Thread.Sleep(5);
                time_slept += 5;
                if (time_slept > TIMEOUT_MS)
                {
                    System.Environment.Exit(-2);
                }
            }

            bool dataReceived = false;
            ColorFrame currColorFrame = null;
            while (!dataReceived)
            {
                currColorFrame = SensorHandler.GetColorFrame();
                if (currColorFrame != null)
                {
                    dataReceived = true;
                }
            }

            Bitmap frameData = this.ConvertCurrFrameToBitmap(currColorFrame);
            return this.ProcessCurrentFrame(frameData);
        }

        private Bitmap ConvertCurrFrameToBitmap(ColorFrame colorFrame)
        {
            int width = colorFrame.FrameDescription.Width;
            int height = colorFrame.FrameDescription.Height;

            WriteableBitmap pxData = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);
            pxData.Lock();
            colorFrame.CopyConvertedFrameDataToIntPtr(
                pxData.BackBuffer,
                (uint)(width * height * 4),
                ColorImageFormat.Bgra);

            pxData.AddDirtyRect(new Int32Rect(0, 0, width, height));
            pxData.Unlock();
            Bitmap bmp;
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create((BitmapSource)pxData));
                enc.Save(outStream);
                bmp = new System.Drawing.Bitmap(outStream);
            }
            return bmp;
        }

        private JsonResult<Dictionary<string, double>> ProcessCurrentFrame(Bitmap frameData)
        {
            return Json(new Dictionary<string, double>());
        }
    }
}
