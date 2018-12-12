using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;

namespace KinectPointingAPI.Image_Processing
{
    public class BlockDetector
    {
        public void DetectBlocks(Bitmap inputImg)
        {
            Image<Rgba, Byte> img = new Image<Rgba, Byte>(inputImg);
            

            // Threshold out bakground
            Image<Gray, Byte> grayImg = img.Convert<Gray, Byte>();
            double threshold_value = CvInvoke.Threshold(grayImg, grayImg, 0, 255, ThresholdType.Otsu);

            
        }
    }
}