using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Numerics;
using Microsoft.Kinect;
using Newtonsoft.Json;
using System.Threading;
using System.Web.Http.Results;
using System.Windows.Media.Media3D;
using Newtonsoft.Json.Linq;
using HRC_Datatypes;
using KinectPointingAPI.Sensor;

namespace KinectPointingAPI.Controllers
{
    [RoutePrefix("api/Pointing")]
    public class ValuesController : AnnotationController<Dictionary<string, List<Dictionary<string, double>>>>
    {
        private KinectSensor kinectSensor;
        private List<Dictionary<string, double>> blockConfidences;
        private ColorFrame currColorFrame;


        private static int CONNECT_TIMEOUT_MS = 20000;
        private static int POINTING_TIMEOUT_MS = 60000;
        private static string ANNOTATION_TYPE_CLASS = "edu.rosehulman.aixprize.pipeline.types.Pointing";

        public ValuesController()
        {
            this.blockConfidences = new List<Dictionary<string, double>>();
        }

        public override void ProcessRequest(JToken allAnnotations)
        {
            KinectSensor kinectSensor = SensorHandler.GetSensor();

            int ms_slept = 0;
            while (!kinectSensor.IsAvailable)
            {
                Thread.Sleep(500);
                ms_slept += 500;
                System.Diagnostics.Debug.WriteLine("Waiting on sensor...");
                if (ms_slept >= CONNECT_TIMEOUT_MS)
                {
                    System.Environment.Exit(-1);
                }
            }

            CoordinateMapper coordinateMapper = kinectSensor.CoordinateMapper;
            FrameDescription frameDescription = kinectSensor.DepthFrameSource.FrameDescription;
            List<Tuple<JointType, JointType>> bones = new List<Tuple<JointType, JointType>>();

            // Right Arm
            List<Tuple<JointType, JointType>> bones = new List<Tuple<JointType, JointType>>();
            bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            bool dataReceived = false;
            Body[] bodies = null;
            Body body = null;
            ms_slept = 0;
            while (!dataReceived)
            {

                BodyFrame bodyFrame = null;
                System.Diagnostics.Debug.WriteLine("Waiting on body frame...");
                while (bodyFrame == null)
                {
                    bodyFrame = SensorHandler.GetBodyFrame();
                }
                bodies = new Body[bodyFrame.BodyCount];
                bodyFrame.GetAndRefreshBodyData(bodies);
                System.Diagnostics.Debug.WriteLine("Checking if body is detected in frame...");
                System.Diagnostics.Debug.WriteLine(bodyFrame.BodyCount + " bodies detected");
                int count = 0;
                if (bodyFrame.BodyCount > 0)
                {
                    foreach (Body b in bodies) {
                        if (b.IsTracked)
                        {
                            System.Diagnostics.Debug.WriteLine("Found body frame.");
                            body = b;
                            dataReceived = true;
                            count++;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine(count + " bodies tracked");
                Thread.Sleep(100);

                ms_slept += 100;
                if (ms_slept >= POINTING_TIMEOUT_MS)
                {
                    System.Environment.Exit(-1);
                }
                bodyFrame.Dispose();
            }
            bodyFrameReader.Dispose();

            //// convert the joint points to depth (display) space
            ///
            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;
            CoordinateMapper coordinateMapper = kinectSensor.CoordinateMapper;
            Dictionary<JointType, CameraSpacePoint> jointPoints = new Dictionary<JointType, CameraSpacePoint>();
            foreach (JointType jointType in joints.Keys)
            {
                // sometimes the depth(Z) of an inferred joint may show as negative
                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                CameraSpacePoint position = joints[jointType].Position;
                if (position.Z < 0)
                {
                    position.Z = 0.1f;
                }


                DepthSpacePoint depthSpacePoint = coordinateMapper.MapCameraPointToDepthSpace(position);
                jointPoints[jointType] = position;
            }
            Tuple<JointType, JointType> bone = bones.First();

            List<BlockData> blocks = this.GetBlocks(allAnnotations);
            this.ComputeConfidenceScores(bone, jointPoints, blocks);
        }

        public override JsonResult<Dictionary<string, List<Dictionary<string, double>>>> GenerateAnnotationResponse()
        {
            Dictionary<string, List<Dictionary<string, double>>> annotation = new Dictionary<string, List<Dictionary<string, double>>>();

            annotation.Add(ANNOTATION_TYPE_CLASS, this.blockConfidences);
            return Json(annotation);
        }

        private List<BlockData> GetBlocks(JToken allAnnotations)
        {
            JToken detectedBlocks = allAnnotations["DetectedBlock"];

            List<BlockData> allBlocks = new List<BlockData>();
            foreach (JToken blockString in detectedBlocks)
            {
                int id = blockString["id"].ToObject<int>();
                int centerX = blockString["center_X"].ToObject<int>();
                int centerY = blockString["center_Y"].ToObject<int>();
                double cameraSpaceCenterX = blockString["camera_space_center_X"].ToObject<double>();
                double cameraSpaceCenterY = blockString["camera_space_center_Y"].ToObject<double>();
                double cameraSpaceDepth = blockString["camera_space_depth"].ToObject<double>();
                double rHue = blockString["r_hue"].ToObject<double>();
                double gHue = blockString["g_hue"].ToObject<double>();
                double bHue = blockString["b_hue"].ToObject<double>();

                BlockData block = new BlockData(id, centerX, centerY, rHue, gHue, bHue);
                block.cameraSpaceCenterX = cameraSpaceCenterX;
                block.cameraSpaceCenterY = cameraSpaceCenterY;
                block.cameraSpaceDepth = cameraSpaceDepth;
                allBlocks.Add(block);
            }

            return allBlocks;
        }

        private void ComputeConfidenceScores(Tuple<JointType, JointType> bone, Dictionary<JointType, CameraSpacePoint> jointPoints, List<BlockData> blocks)
        {
            JointType handCenterFixture = bone.Item1; 
            JointType fingerEndFixture = bone.Item2;
            Vector3D pointingVector = new Vector3D(
               jointPoints[fingerEndFixture].X - jointPoints[handCenterFixture].X,
               jointPoints[fingerEndFixture].Y - jointPoints[handCenterFixture].Y,
               jointPoints[fingerEndFixture].Z - jointPoints[handCenterFixture].Z
            );
            System.Diagnostics.Debug.WriteLine("\n==================================");

            System.Diagnostics.Debug.WriteLine("Hand center point: X=" + jointPoints[handCenterFixture].X + ", Y=" + jointPoints[handCenterFixture].Y + ", Z=" + jointPoints[handCenterFixture].Z);
            System.Diagnostics.Debug.WriteLine("End of finger point: X=" + jointPoints[fingerEndFixture].X + ", Y=" + jointPoints[fingerEndFixture].Y + ", Z=" + jointPoints[fingerEndFixture].Z);
            System.Diagnostics.Debug.WriteLine("Current bone vector: " + pointingVector);

            Dictionary<string, double> dict = new Dictionary<string, double>();
            foreach (BlockData block in blocks)
            {
                Vector3D blockToHandCenter = new Vector3D(
                        block.cameraSpaceCenterX - jointPoints[handCenterFixture].X,
                        block.cameraSpaceCenterY - jointPoints[handCenterFixture].Y,
                        block.cameraSpaceDepth - jointPoints[handCenterFixture].Z
                );
                double confidence = Vector3D.DotProduct(pointingVector, blockToHandCenter) / (pointingVector.Length * blockToHandCenter.Length);
                System.Diagnostics.Debug.WriteLine("Vector to center of hand for block id=" + block.id + " and X=" + block.cameraSpaceCenterX + ", Y=" + block.cameraSpaceCenterY + ", Z=" + block.cameraSpaceDepth + ": " + blockToHandCenter);
                Dictionary<string, double> blockConfidence = new Dictionary<string, double>();
                blockConfidence.Add("id", block.id);
                blockConfidence.Add("confidence", confidence);

                this.blockConfidences.Add(blockConfidence);
            }
        }
    }
}
