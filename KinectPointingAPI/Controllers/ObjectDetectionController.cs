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
using System.Web.Http.Results;

namespace KinectPointingAPI.Controllers
{
    public struct BlockData
    {
        public Vector3 center;

        public BlockData(Vector3 center)
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

        // POST api/ObjectDetection
        [HttpPost]
        public JsonResult<Dictionary<string, BlockData>> Post()
        {
            this.ParsePostBody();
            System.Diagnostics.Debug.Print("POST body: " + value);
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

            Dictionary<string, BlockData> aggregatedData = this.ProcessBlocksFromFrames();
            return Json(aggregatedData);
        }

        private string ParsePostBody()
        {
            Request.Content.ReadAsStreamAsync();
            return "";
        }
        
        private Dictionary<string, BlockData> ProcessBlocksFromFrames()
        {
            Bitmap colorData = this.ConvertCurrFrameToBitmap();
            Point[] blockCenters = this.blockDetector.DetectBlocks(colorData, this.colorFrameDescription.Width, this.colorFrameDescription.Height);
            List<Vector3> augmentedCenters = this.AugmentCentersWithDepth(blockCenters);

            Dictionary<string, BlockData> aggregatedData = new Dictionary<string, BlockData>();
            for (int i = 0; i < augmentedCenters.Count; i++)
            {
                Vector3 currCenter = augmentedCenters[i];
                BlockData currData = new BlockData(currCenter);
                string currentBlock = "Block_" + i;
                aggregatedData.Add(currentBlock, currData);
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

        private List<Vector3> AugmentCentersWithDepth(Point[] centers)
        {
            int depthWidth = this.currDepthFrame.FrameDescription.Width;
            int depthHeight = this.currDepthFrame.FrameDescription.Height;
            ushort[] depths = new ushort[depthWidth * depthHeight];
            this.currDepthFrame.CopyFrameDataToArray(depths);

            int colorWidth = this.colorFrameDescription.Width;
            int colorHeight = this.colorFrameDescription.Height;
            DepthSpacePoint[] depthPoints = new DepthSpacePoint[colorWidth * colorHeight];
            kinectSensor.CoordinateMapper.MapColorFrameToDepthSpace(depths, depthPoints);

            List<Vector3> augmentedCenters = new List<Vector3>();
            foreach (Point center in centers)
            {

                int viableIdx = -1;
                Boolean foundViableIndex = false;

                // Find a nearby point for which the depth is actually defined, as depth resolution is smaller than color resolution -> not all color points have a depth
                for(int i = -10; i < 10; i++)
                {
                    for(int j = -10; j < 10;  j++)
                    {
                        int colorIdx = (center.Y + j) * colorWidth + (center.X + i);
                        
                        if(depthPoints[colorIdx].X != Double.NegativeInfinity && depthPoints[colorIdx].Y != Double.NegativeInfinity)
                        {
                            viableIdx = colorIdx;
                            foundViableIndex = true;
                            break;
                        }
                    }

                    if(foundViableIndex)
                    {
                        break;
                    }
                }

                int depthX = foundViableIndex ? Convert.ToInt32(depthPoints[viableIdx].X) : 0;
                int depthY = foundViableIndex ? Convert.ToInt32(depthPoints[viableIdx].Y) : 0;

                int depthIdx = depthY * depthWidth + depthX;
                int currDepth = Convert.ToInt32(depths[depthIdx]);
                augmentedCenters.Add(new Vector3(center.X, center.Y, currDepth));
            }

            return augmentedCenters;
        }
    }
}
