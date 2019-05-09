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
    public class StereoCalibrationController : ApiController
    {
        [HttpPost]
        [Route("api/StereoCalibrate")]
        public JsonResult<Dictionary<string, string>> Post()
        {
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

            int imgWidth = currColorFrame.FrameDescription.Width;
            int imgHeight = currColorFrame.FrameDescription.Height;
            byte[] frameData = this.ConvertColorFrameToBytes(currColorFrame);
            return this.GenerateResponse(frameData, imgWidth, imgHeight);
        }

        private byte[] ConvertColorFrameToBytes(ColorFrame colorFrame)
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
            byte[] imgBytes;
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create((BitmapSource)pxData));
                enc.Save(outStream);
                imgBytes = outStream.ToArray();
            }
            return imgBytes;
        }

        private byte[] ConvertBitmapToBytes(Bitmap bmp)
        {
            byte[] imgBytes;
            using (MemoryStream stream = new MemoryStream())
            {
                bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                imgBytes = stream.ToArray();
            }
            return imgBytes;
        }

        private JsonResult<Dictionary<string, string>> GenerateResponse(byte[] frameData, int imgWidth, int imgHeight)
        {
            Dictionary<string, string> responseData = new Dictionary<string, string>();

            responseData.Add("ImageWidth", Convert.ToString(imgWidth));
            responseData.Add("ImageHeight", Convert.ToString(imgHeight));
            string encodedFrameData = Convert.ToBase64String(frameData);
            responseData.Add("EncodedImage", encodedFrameData);

            return Json(responseData);
        }
    }
}
