using System;
using System.Collections.Generic;
using System.Web.Http;
using Microsoft.Kinect;
using System.Threading;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using Point = System.Drawing.Point;
using System.IO;
using System.Web.Http.Results;
using System.Threading.Tasks;

using KinectPointingAPI.Image_Processing;
using HRC_Datatypes;
using Newtonsoft.Json.Linq;

namespace KinectPointingAPI.Controllers
{
    [RoutePrefix("api/ObjectDetection")]
    public class ObjectDetectionController : AnnotationController<Dictionary<string, List<Dictionary<string, double>>>>
    {
        private KinectSensor kinectSensor;
        private CoordinateMapper coordinateMapper;
        private FrameDescription colorFrameDescription;
        private BlockDetector blockDetector;

        private List<BlockData> aggregatedData;
        private ColorFrame currColorFrame;
        private DepthFrame currDepthFrame;


        private static int TIMEOUT_MS = 30000;
        private static string ANNOTATION_TYPE_CLASS = "edu.rosehulman.aixprize.pipeline.types.DetectedBlock";

        public ObjectDetectionController()
        {
            this.kinectSensor = KinectSensor.GetDefault();
            this.coordinateMapper = kinectSensor.CoordinateMapper;
            this.colorFrameDescription = kinectSensor.ColorFrameSource.FrameDescription;
            this.blockDetector = new BlockDetector();
        }

        public override void ProcessRequest(JToken casJSON)
        {
            kinectSensor.Open();
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

            this.aggregatedData = this.ProcessBlocksFromFrames();
        }

        public override JsonResult<Dictionary<string, List<Dictionary<string, double>>>> GenerateAnnotationResponse()
        {
            List<Dictionary<string, double>> serializedBlocks = new List<Dictionary<string, double>>();

            foreach (BlockData block in this.aggregatedData)
            {
                serializedBlocks.Add(block.ConvertToDict());
            }

            Dictionary<String, List<Dictionary<string, double>>> annotation = new Dictionary<String, List<Dictionary<string, double>>>();
            annotation.Add(ANNOTATION_TYPE_CLASS, serializedBlocks);
            return Json(annotation);
        }

        private List<BlockData> ProcessBlocksFromFrames()
        {
            Bitmap colorData = this.ConvertCurrFrameToBitmap();
            List<BlockData> aggregatedData = this.blockDetector.DetectBlocks(colorData, this.colorFrameDescription.Width, this.colorFrameDescription.Height);
            aggregatedData = this.AugmentCentersWithDepth(aggregatedData);

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

        private List<BlockData> AugmentCentersWithDepth(List<BlockData> blocks)
        {
            int depthWidth = this.currDepthFrame.FrameDescription.Width;
            int depthHeight = this.currDepthFrame.FrameDescription.Height;
            ushort[] depths = new ushort[depthWidth * depthHeight];
            this.currDepthFrame.CopyFrameDataToArray(depths);

            int colorWidth = this.colorFrameDescription.Width;
            int colorHeight = this.colorFrameDescription.Height;
            DepthSpacePoint[] depthPoints = new DepthSpacePoint[colorWidth * colorHeight];
            kinectSensor.CoordinateMapper.MapColorFrameToDepthSpace(depths, depthPoints);

            foreach (BlockData block in blocks)
            {
                Point center = new Point(block.centerX, block.centerY);
                int viableIdx = -1;
                Boolean foundViableIndex = false;

                // Find a nearby point for which the depth is actually defined, as depth resolution is smaller than color resolution -> not all color points have a depth
                for(int i = -10; i < 10; i++)
                {
                    for(int j = -10; j < 10;  j++)
                    {
                        int colorIdx = (center.Y + j) * colorWidth + (center.X + i);

                        if (colorIdx < depthPoints.Length && depthPoints[colorIdx].X != Double.NegativeInfinity && depthPoints[colorIdx].Y != Double.NegativeInfinity)
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
                block.depth = currDepth;
            }

            return blocks;
        }

       
    }
}
