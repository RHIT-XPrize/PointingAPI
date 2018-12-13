using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;

namespace KinectPointingAPI.Image_Processing
{
    public class BlockDetector
    {
        private static Size STRUCT_ELEM_SIZE = new Size(10, 10);
        private static Point STRUCT_ELEM_ANCHOR = new Point(5, 5);
        private static int MIN_BLOCK_SIZE_PIXELS = 60;

        public Point[] DetectBlocks(Bitmap inputImg, int width, int height)
        {
            Image<Bgra, Byte> img = new Image<Bgra, Byte>(inputImg);

            // Threshold out bakground
            Image<Gray, Byte> grayImg = img.Convert<Gray, Byte>();
            Image<Gray, Byte> backgroundMask = new Image<Gray, Byte>(width, height);
            double threshold_value = CvInvoke.Threshold(grayImg, backgroundMask, 0, 255, ThresholdType.Otsu);

            Image<Gray, Byte> filledBackground = this.fillMask(backgroundMask);

            VectorOfVectorOfPoint allContours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(backgroundMask, allContours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

            // Remove all contours except table
            int tableContourIdx = this.findLargestContourIdx(allContours);
            Image<Gray, Byte> tableMask = new Image<Gray, Byte>(width, height);
            int fillInterior = -1;
            CvInvoke.DrawContours(tableMask, allContours, tableContourIdx, new MCvScalar(255), fillInterior);
            IInputArray structElem = CvInvoke.GetStructuringElement(ElementShape.Rectangle, STRUCT_ELEM_SIZE, STRUCT_ELEM_ANCHOR);
            CvInvoke.Erode(tableMask, tableMask, structElem, new Point(-1, -1), 1, BorderType.Constant, new MCvScalar(255));

            // Grab objects on table that are in foreground 
            Image<Gray, Byte> foregroundMask = new Image<Gray, Byte>(width, height);
            CvInvoke.BitwiseNot(backgroundMask, foregroundMask);

            Image<Gray, Byte> tableForegroundMask = new Image<Gray, Byte>(width, height);
            CvInvoke.BitwiseAnd(foregroundMask, tableMask, tableForegroundMask);
           
            // Find contours for blocks on table
            VectorOfVectorOfPoint possibleBlocks = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(tableForegroundMask, possibleBlocks, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);
            VectorOfVectorOfPoint filteredBlocks = this.filterSmallAreaContours(possibleBlocks);

            // Find block centers
            Point[] blockCenters = this.findContourCenters(filteredBlocks);
            return blockCenters;
        }

        public Image<Gray, Byte> fillMask(Image<Gray, Byte> input)
        {
            // Fill all parts of image that can be reached from edges
            Image<Gray, Byte> reachableBackground = input.Clone();
            for (int x = 0; x < input.Width; x++)
            {
                MCvScalar fillValue = new MCvScalar(255);
                Rectangle boundingBox = new Rectangle();
                MCvScalar minDiff = new MCvScalar(0);
                MCvScalar maxDiff = new MCvScalar(255);

                // Top pixel
                Point startFromTop = new Point(x, 0);
                if (reachableBackground.Data[0, x, 0] == 0)
                {
                    CvInvoke.FloodFill(reachableBackground, null, startFromTop, fillValue, out boundingBox, minDiff, maxDiff);
                }

                // Top pixel
                Point startFromBottom = new Point(x, input.Height - 1);
                if (reachableBackground.Data[input.Height - 1, x, 0] == 0)
                {
                    CvInvoke.FloodFill(reachableBackground, null, startFromBottom, fillValue, out boundingBox, minDiff, maxDiff);
                }
            }

            for (int y = 0; y < input.Height; y++)
            {
                MCvScalar fillValue = new MCvScalar(255);
                Rectangle boundingBox = new Rectangle();
                MCvScalar minDiff = new MCvScalar(0);
                MCvScalar maxDiff = new MCvScalar(255);

                // Top pixel
                Point startFromLeft = new Point(0, y);
                if (reachableBackground.Data[y, 0, 0] == 0)
                {
                    CvInvoke.FloodFill(reachableBackground, null, startFromLeft, fillValue, out boundingBox, minDiff, maxDiff);
                }

                // Top pixel
                Point startFromRight = new Point(input.Width - 1, y);
                if (reachableBackground.Data[y, input.Width - 1, 0] == 0)
                {
                    CvInvoke.FloodFill(reachableBackground, null, startFromRight, fillValue, out boundingBox, minDiff, maxDiff);
                }
            }

            // Grab unreachable holes in original image
            Image<Gray, Byte> holesToFill = reachableBackground.Clone();
            CvInvoke.BitwiseNot(reachableBackground, holesToFill);

            Image<Gray, Byte> filledImg = input.Clone();
            CvInvoke.BitwiseOr(input, holesToFill, filledImg);
            return filledImg;
        }

        public int findLargestContourIdx(VectorOfVectorOfPoint contours)
        {
            int largestContourIdx = 0;
            double largestContourArea = -1;
            for(int i = 0; i < contours.Size; i++)
            {
                VectorOfPoint currContour = contours[i];
                double contourArea = CvInvoke.ContourArea(currContour);
                if(contourArea > largestContourArea)
                {
                    largestContourIdx = i;
                    largestContourArea = contourArea;
                }
            }

            return largestContourIdx;
        }

        public VectorOfVectorOfPoint filterSmallAreaContours(VectorOfVectorOfPoint contours)
        {
            VectorOfVectorOfPoint filteredContours = new VectorOfVectorOfPoint();
            for(int i = 0; i < contours.Size; i++)
            {
                VectorOfPoint currContour = contours[i];
                double contourArea = CvInvoke.ContourArea(currContour);
                if (contourArea > MIN_BLOCK_SIZE_PIXELS)
                {
                    System.Diagnostics.Debug.Print(contours.Size + ", " + contourArea);
                    filteredContours.Push(currContour);
                }
            }
            return filteredContours;
        }

        public Point[] findContourCenters(VectorOfVectorOfPoint contours)
        {
            Point[] blockCenters = new Point[contours.Size];
            System.Diagnostics.Debug.Print("" + contours.Size);
            for (int i = 0; i < contours.Size; i++)
            {
                VectorOfPoint currContour = contours[i];
                MCvMoments currMoments = CvInvoke.Moments(currContour);

                int centerX = 0;
                int centerY = 0;
                if (currMoments.M00 != 0)
                {
                    centerX = Convert.ToInt32(currMoments.M10 / currMoments.M00);
                    centerY = Convert.ToInt32(currMoments.M01 / currMoments.M00);
                }

                blockCenters[i] = new Point(centerX, centerY);
            }

            return blockCenters;
        }

    }
}