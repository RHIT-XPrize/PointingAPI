using System;
using System.Collections.Generic;
using HRC_Datatypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using System.Drawing;
using KinectPointingAPI.Image_Processing;

namespace API_Testing
{
    [TestClass]
    public class TestImageProcessing
    {

        BlockDetector detector;

        [TestInitialize]
        public void SetUp()
        {
            detector = new BlockDetector();
        }

        [TestMethod]
        public void TestFillMask_EmptyImage()
        {
            Image<Gray, Byte> inputImg = new Image<Gray, byte>(3, 3);

            Image<Gray, Byte> expectedImg = inputImg.Clone();
            Image<Gray, Byte> actualImg = detector.FillMask(inputImg);

            AssertImagesEqual(expectedImg, actualImg);
        }

        [TestMethod]
        public void TestFillMask_NoHoles()
        {
            Image<Gray, Byte> inputImg = new Image<Gray, byte>(3, 3);
            // Create L pattern in image
            inputImg[0, 0] = new Gray(255);
            inputImg[1, 0] = new Gray(255);
            inputImg[2, 0] = new Gray(255);
            inputImg[2, 1] = new Gray(255);
            inputImg[2, 2] = new Gray(255);

            Image<Gray, Byte> expectedImg = inputImg.Clone();
            Image<Gray, Byte> actualImg = detector.FillMask(inputImg);

            AssertImagesEqual(expectedImg, actualImg);
        }

        [TestMethod]
        public void TestFillMask_OneHole()
        {
            Image<Gray, Byte> inputImg = new Image<Gray, byte>(3, 3);
            // Create hollow square pattern in image
            inputImg[0, 0] = new Gray(255);
            inputImg[0, 1] = new Gray(255);
            inputImg[0, 2] = new Gray(255);
            inputImg[1, 0] = new Gray(255);
            inputImg[1, 2] = new Gray(255);
            inputImg[2, 0] = new Gray(255);
            inputImg[2, 1] = new Gray(255);
            inputImg[2, 2] = new Gray(255);

            Image<Gray, Byte> expectedImg = inputImg.Clone();
            expectedImg[1, 1] = new Gray(255);
            Image<Gray, Byte> actualImg = detector.FillMask(inputImg);

            AssertImagesEqual(expectedImg, actualImg);
        }

        [TestMethod]
        public void TestFindColorAtCenter_NoCenters()
        {
            Point[] blockCenters = new Point[1];
            Image<Bgra, Byte> inputImg = new Image<Bgra, byte>(3, 3);
            inputImg[1, 1] = new Bgra(255, 255, 255, 0);

            List<BlockData> actualColors = detector.FindColorAtCenters(blockCenters, inputImg);

            AssertColorsMatch(blockCenters, inputImg, actualColors);
        }

        [TestMethod]
        public void TestFindColorAtCenter_SingleCenterDefaultColor()
        {
            Point[] blockCenters = new Point[1] {
                new Point(1, 1)
            };
            Image<Bgra, Byte> inputImg = new Image<Bgra, byte>(3, 3);

            List<BlockData> actualColors = detector.FindColorAtCenters(blockCenters, inputImg);

            AssertColorsMatch(blockCenters, inputImg, actualColors);
        }

        [TestMethod]
        public void TestFindColorAtCenter_SingleCenterRedColor()
        {
            Point[] blockCenters = new Point[1] {
                new Point(1, 1)
            };
            Image<Bgra, Byte> inputImg = new Image<Bgra, byte>(3, 3);
            inputImg[1, 1] = new Bgra(10, 10, 200, 0);

            List<BlockData> actualColors = detector.FindColorAtCenters(blockCenters, inputImg);

            AssertColorsMatch(blockCenters, inputImg, actualColors);
        }

        [TestMethod]
        public void TestFindColorAtCenter_SingleCenterMaxColor()
        {
            Point[] blockCenters = new Point[1] {
                new Point(1, 1)
            };
            Image<Bgra, Byte> inputImg = new Image<Bgra, byte>(3, 3);
            inputImg[1, 1] = new Bgra(255, 255, 255, 0);
            
            List<BlockData> actualColors = detector.FindColorAtCenters(blockCenters, inputImg);

            AssertColorsMatch(blockCenters, inputImg, actualColors);
        }

        [TestMethod]
        public void TestFindColorAtCenter_MultipleCenters()
        {
            Point[] blockCenters = new Point[2] {
                new Point(1, 1),
                new Point(2, 0)
            };
            Image<Bgra, Byte> inputImg = new Image<Bgra, byte>(3, 3);
            inputImg[0, 2] = new Bgra(255, 255, 255, 0);
            inputImg[1, 1] = new Bgra(105, 5, 58, 0);

            List<BlockData> actualColors = detector.FindColorAtCenters(blockCenters, inputImg);

            AssertColorsMatch(blockCenters, inputImg, actualColors);
        }

        private void AssertImagesEqual(Image<Gray, Byte> expected, Image<Gray, Byte> actual)
        {
            Image<Gray, Byte> imageDiff = expected.AbsDiff(actual);

            Gray expectedPixelVal = new Gray(0);
            for (int row = 0; row < imageDiff.Rows; row++)
            {
                for (int col = 0; col < imageDiff.Cols; col++)
                {
                    Assert.AreEqual(expectedPixelVal, imageDiff[row, col], String.Format("Mismatch found at entry with row = {0}, col = {1}.", row, col));
                }
            }
        }

        private void AssertColorsMatch(Point[] blockCenters, Image<Bgra, Byte> inputImg, List<BlockData> actualColors)
        {
            Assert.AreEqual(blockCenters.Length, actualColors.Count, "Number of items returned does not equal the number of centers provided.");
            for(int i = 0; i < blockCenters.Length; i++)
            {
                Point currPoint = blockCenters[i];
                int row = currPoint.Y;
                int col = currPoint.X;
                Bgra expectedColor = inputImg[row, col];

                BlockData currBlock = actualColors[i];
                Bgra actualColor = new Bgra(currBlock.bHueVal, currBlock.gHueVal, currBlock.rHueVal, 0);

                Assert.AreEqual(expectedColor, actualColor, "Mismatch found at point {0}, which corresponds to row = {1}, col = {2}.", i + 1, row, col);
            }
        }
    }
}
