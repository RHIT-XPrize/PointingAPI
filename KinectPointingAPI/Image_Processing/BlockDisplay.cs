using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.Structure;

using HRC_Datatypes;

namespace KinectPointingAPI.Image_Processing
{
    public class BlockDisplay
    {
        public void DisplayBlockOnImage(Bitmap inputImg, BlockData blockToDisplay)
        {
            Image<Bgra, Byte> img = new Image<Bgra, Byte>(inputImg);

            Point blockCenter = new Point(blockToDisplay.centerX, blockToDisplay.centerY);
            int radius = 10;
            int filledCircle = -1;

            CvInvoke.Circle(img, blockCenter, radius, new MCvScalar(0, 0, 0, 0), filledCircle);
        }

        private void DisplayImage(Image<Bgr, Byte> img)
        {
            ImageViewer.Show(img, "Detected Block");
        }
    }
}