using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Numerics;
using Microsoft.Kinect;
using Newtonsoft.Json;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;

using KinectPointingAPI.Image_Processing;
using System.Windows.Media;
using System.Windows;
using Point = System.Drawing.Point;
using System.IO;

namespace KinectPointingAPI.Controllers
{
    public struct BlockData
    {
        public Point center;

        public BlockData(Point center)
        {
            this.center = center;
        }
    }

    public class ObjectDetectionController : ApiController
    {
        private KinectSensor kinectSensor;
        private CoordinateMapper coordinateMapper;
        private FrameDescription colorFrameDescription;

        private ColorFrame currColorFrame;
        private DepthFrame currDepthFrame;

        private BlockDetector blockDetector;

        private static int TIMEOUT_MS = 30000;

        public ObjectDetectionController()
        {
            this.kinectSensor = KinectSensor.GetDefault();
            this.coordinateMapper = kinectSensor.CoordinateMapper;
            this.colorFrameDescription = kinectSensor.ColorFrameSource.FrameDescription;
            this.blockDetector = new BlockDetector();
        }

        // GET api/ObjectDetection
        public string Get()
        {
            kinectSensor.Open();
            int time_slept = 0;
            while (!kinectSensor.IsAvailable)
            {
                Thread.Sleep(5);
                time_slept += 5;
                if(time_slept > TIMEOUT_MS)
                {
                    System.Environment.Exit(-1);
                }
            }

            ColorFrameReader colorFrameReader = kinectSensor.ColorFrameSource.OpenReader();

            bool dataReceived = false;
            while (!dataReceived)
            {
                this.currColorFrame = colorFrameReader.AcquireLatestFrame();
                if (this.currColorFrame != null)
                {
                    dataReceived = true;
                }
            }

            DepthFrameReader depthFrameReader = kinectSensor.DepthFrameSource.OpenReader();
            dataReceived = false;
            while (!dataReceived)
            {
                this.currDepthFrame = depthFrameReader.AcquireLatestFrame();
                if (this.currDepthFrame != null)
                {
                    dataReceived = true;
                }
            }

            List<BlockData> aggregatedData = this.ProcessBlocksFromFrames();
            return JsonConvert.SerializeObject(aggregatedData);
        }

        private List<BlockData> ProcessBlocksFromFrames()
        {
            Bitmap colorData = this.ConvertCurrFrameToBitmap();
            Point[] blockCenters = this.blockDetector.DetectBlocks(colorData, this.colorFrameDescription.Width, this.colorFrameDescription.Height);

            List<BlockData> aggregatedData = new List<BlockData>();
            for (int i = 0; i < blockCenters.Length; i++)
            {
                Point currCenter = blockCenters[i];
                BlockData currData = new BlockData(currCenter);
                aggregatedData.Add(currData);
            }

            return aggregatedData;
        }

        private Bitmap ConvertCurrFrameToBitmap()
        {
            int width = this.colorFrameDescription.Width;
            int height = this.colorFrameDescription.Height;

            WriteableBitmap pxData = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);
            pxData.Lock();
            this.currColorFrame.CopyConvertedFrameDataToIntPtr(
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
    }
}
