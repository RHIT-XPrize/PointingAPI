using System;
using System.Collections.Generic;
using HRC_Datatypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace API_Testing
{
    [TestClass]
    public class TestBlockData
    {
        [TestMethod]
        public void TestConvertToDict_InitialValues()
        {
            BlockData testData = new BlockData(0, 0, 0, 0, 0, 0);

            Dictionary<string, double> convertedDict = testData.ConvertToDict();

            AssertDictDataValid(testData, convertedDict);
        }

        [TestMethod]
        public void TestConvertToDict_UpdateSingleValue()
        {
            BlockData testData = new BlockData(0, 0, 0, 0, 0, 0);
            testData.centerX = 25;

            Dictionary<string, double> convertedDict = testData.ConvertToDict();

            AssertDictDataValid(testData, convertedDict);
        }

        [TestMethod]
        public void TestConvertToDict_UpdateManyValues()
        {
            BlockData testData = new BlockData(0, 0, 0, 0, 0, 0);
            testData.cameraSpaceCenterX = 123;
            testData.cameraSpaceCenterY = 531;
            testData.cameraSpaceDepth = 3345;
            testData.rHueVal = 256;
            testData.bHueVal = 77;

            Dictionary<string, double> convertedDict = testData.ConvertToDict();

            AssertDictDataValid(testData, convertedDict);
        }

        private void AssertDictDataValid(BlockData expectedData, Dictionary<string, double> actualData)
        {
            Assert.AreEqual(expectedData.id, actualData["id"]);
            Assert.AreEqual(expectedData.centerX, actualData["center_X"]);
            Assert.AreEqual(expectedData.centerY, actualData["center_Y"]);
            Assert.AreEqual(expectedData.cameraSpaceCenterX, actualData["camera_space_center_X"]);
            Assert.AreEqual(expectedData.cameraSpaceCenterY, actualData["camera_space_center_Y"]);
            Assert.AreEqual(expectedData.cameraSpaceDepth, actualData["camera_space_depth"]);
            Assert.AreEqual(expectedData.rHueVal, actualData["r_hue"]);
            Assert.AreEqual(expectedData.gHueVal, actualData["g_hue"]);
            Assert.AreEqual(expectedData.bHueVal, actualData["b_hue"]);
        }
    }
}
