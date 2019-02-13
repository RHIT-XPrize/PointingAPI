using System.Collections.Generic;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using System.Web.Http.Results;
using System.Threading.Tasks;

using Microsoft.Kinect;
using System.Drawing;

using KinectPointingAPI.Image_Processing;
using HRC_Datatypes;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Windows;
using System.IO;
using System.Windows.Media;

namespace KinectPointingAPI.Controllers
{
    [RoutePrefix("api/Output")]
    public class OutputController : AnnotationController<Dictionary<string, List<Dictionary<string, string>>>>
    {
        private KinectSensor kinectSensor;
        private CoordinateMapper coordinateMapper;
        private FrameDescription colorFrameDescription;
        private ColorFrame currColorFrame;
        private BlockDisplay blockDisplay;
        private string feedback;

        private static int CONNECT_TIMEOUT_MS = 20000;
        private static string ANNOTATION_TYPE_CLASS = "edu.rosehulman.aixprize.pipeline.types.Feedback";

        public OutputController()
        {
            this.kinectSensor = KinectSensor.GetDefault();
            this.coordinateMapper = kinectSensor.CoordinateMapper;
            this.colorFrameDescription = kinectSensor.ColorFrameSource.FrameDescription;
      
            this.blockDisplay = new BlockDisplay();
        }

        public override void ProcessRequest(JToken allAnnotations)
        {
            this.kinectSensor = KinectSensor.GetDefault();

            int optimalBlockId = this.GetHighestConfidenceId(allAnnotations);
            if (optimalBlockId == -1)
            {
                this.feedback = "Failed to identify a block. Please try again.";
                return;
            }

            BlockData optimalBlock = this.GetBestBlock(allAnnotations, optimalBlockId);
            this.DisplayBestBlock(optimalBlock);
        }

        public override JsonResult<Dictionary<string, List<Dictionary<string, string>>>> GenerateAnnotationResponse()
        {
            Dictionary<string, string> singleFeedbackItem = new Dictionary<string, string>();
            singleFeedbackItem.Add("feedbackMsg", this.feedback);

            List<Dictionary<string, string>> allFeedback = new List<Dictionary<string, string>>();
            allFeedback.Add(singleFeedbackItem);

            Dictionary<string, List<Dictionary<string, string>>> annotation = new Dictionary<string, List<Dictionary<string, string>>>();
            annotation.Add(ANNOTATION_TYPE_CLASS, allFeedback);
            return Json(annotation);
        }

        private int GetHighestConfidenceId(JToken allAnnotations)
        {
            double bestBlockConfidence = -1;
            int bestBlockId = -1;

            JToken confidenceDetails = allAnnotations["Pointing"];
            foreach (JToken blockString in confidenceDetails)
            {
                int id = blockString["id"].ToObject<int>();
                double confidence  = blockString["confidence"].ToObject<double>();

                if(confidence > bestBlockConfidence)
                {
                    bestBlockConfidence = confidence;
                    bestBlockId = id;
                }
            }

            return bestBlockId;
        }

        private BlockData GetBestBlock(JToken allAnnotations, int bestId)
        {
            JToken detectedBlocks = allAnnotations["DetectedBlock"];
            BlockData bestBlock = null;
            foreach (JToken blockString in detectedBlocks)
            {
                int id = blockString["id"].ToObject<int>();
                int centerX = blockString["center_X"].ToObject<int>();
                int centerY = blockString["center_Y"].ToObject<int>();
                double rHue = blockString["r_hue"].ToObject<double>();
                double gHue = blockString["g_hue"].ToObject<double>();
                double bHue = blockString["b_hue"].ToObject<double>();

                if (id == bestId)
                {
                    bestBlock = new BlockData(id, centerX, centerY, rHue, gHue, bHue);
                    break;
                }
            }

            return bestBlock;
        }

        private void DisplayBestBlock(BlockData blockToDisplay)
        {
            kinectSensor.Open();
            int time_slept = 0;
            while (!kinectSensor.IsAvailable)
            {
                Thread.Sleep(5);
                time_slept += 5;
                if (time_slept > CONNECT_TIMEOUT_MS)
                {
                    System.Environment.Exit(-2);
                }
            }

            ColorFrameReader colorFrameReader = kinectSensor.ColorFrameSource.OpenReader();

            bool dataReceived = false;
            while (!dataReceived)
            {
                System.Diagnostics.Debug.WriteLine("About to acquire color frame for drawing!");
                this.currColorFrame = colorFrameReader.AcquireLatestFrame();
                if (this.currColorFrame != null)
                {
                    dataReceived = true;
                }
            }
            System.Diagnostics.Debug.WriteLine("Found color frame for drawing!");
            Bitmap currFrame = this.ConvertCurrFrameToBitmap();
            this.blockDisplay.DisplayBlockOnImage(currFrame, blockToDisplay);

            colorFrameReader.Dispose();
            this.feedback = "Found block!";
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
