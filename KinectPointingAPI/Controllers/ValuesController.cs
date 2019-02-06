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

namespace KinectPointingAPI.Controllers
{
    [RoutePrefix("api/Pointing")]
    public class ValuesController : AnnotationController<Dictionary<string, List<Dictionary<string, double>>>>
    {
        private List<Dictionary<string, double>> blockConfidences;

        private static int CONNECT_TIMEOUT_MS = 20000;
        private static string ANNOTATION_TYPE_CLASS = "edu.rosehulman.aixprize.pipeline.types.Pointing";

        public ValuesController()
        {
            this.blockConfidences = new List<Dictionary<string, double>>();
        }

        public override void ProcessRequest(JToken allAnnotations)
        {
            KinectSensor kinectSensor = KinectSensor.GetDefault();

            kinectSensor.Open();
            int ms_slept = 0;
            while (!kinectSensor.IsAvailable)
            {
                Thread.Sleep(5);
                ms_slept += 5;
                if (ms_slept >= CONNECT_TIMEOUT_MS)
                {
                    System.Environment.Exit(-1);
                }
            }

            CoordinateMapper coordinateMapper = kinectSensor.CoordinateMapper;
            FrameDescription frameDescription = kinectSensor.DepthFrameSource.FrameDescription;
            BodyFrameReader bodyFrameReader = null;
            while (bodyFrameReader == null)
            {
                bodyFrameReader = kinectSensor.BodyFrameSource.OpenReader();
            }
            List<Tuple<JointType, JointType>> bones = new List<Tuple<JointType, JointType>>();

            // Right Arm
            bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            bool dataReceived = false;
            Body[] bodies = null;
            Body body = null;

            while (!dataReceived)
            {
                BodyFrame bodyFrame = null;
                while (bodyFrame == null)
                {
                    bodyFrame = bodyFrameReader.AcquireLatestFrame();
                }
                bodies = new Body[bodyFrame.BodyCount];
                bodyFrame.GetAndRefreshBodyData(bodies);
                if (bodyFrame.BodyCount > 0 && bodies[0].IsTracked)
                {
                    body = bodies[0];
                    dataReceived = true;
                }
            }

            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

            //// convert the joint points to depth (display) space
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
            System.Diagnostics.Debug.Write(detectedBlocks);
            List<BlockData> allBlocks = new List<BlockData>();
            foreach (JToken blockString in detectedBlocks)
            {
                int id = blockString["id"].ToObject<int>();
                int centerX = blockString["center_X"].ToObject<int>();
                int centerY = blockString["center_Y"].ToObject<int>();
                int depth = blockString["depth"].ToObject<int>();
                double rHue = blockString["r_hue"].ToObject<double>();
                double gHue = blockString["g_hue"].ToObject<double>();
                double bHue = blockString["b_hue"].ToObject<double>();

                BlockData block = new BlockData(id, centerX, centerY, rHue, gHue, bHue);
                block.depth = depth;
                allBlocks.Add(block);
            }

            return allBlocks;
        }

        private void ComputeConfidenceScores(Tuple<JointType, JointType> bone, Dictionary<JointType, CameraSpacePoint> jointPoints, List<BlockData> blocks)
        {
            Vector3D boneVector = new Vector3D(
               jointPoints[bone.Item2].X - jointPoints[bone.Item1].X,
               jointPoints[bone.Item2].Y - jointPoints[bone.Item1].Y,
               jointPoints[bone.Item2].Z - jointPoints[bone.Item1].Z
            );


            Dictionary<string, double> dict = new Dictionary<string, double>();
            foreach (BlockData block in blocks)
            {
                Vector3D distanceToBone = new Vector3D(
                        block.centerX - jointPoints[bone.Item1].X,
                        block.centerY - jointPoints[bone.Item1].Y,
                        block.depth - jointPoints[bone.Item1].Z
                );
                double confidence = 1 / (Vector3D.DotProduct(boneVector, distanceToBone));

                Dictionary<string, double> blockConfidence = new Dictionary<string, double>();
                blockConfidence.Add("id", block.id);
                blockConfidence.Add("confidence", confidence);

                this.blockConfidences.Add(blockConfidence);
            }
        }  
    }
}
