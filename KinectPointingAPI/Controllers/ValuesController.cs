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

namespace KinectPointingAPI.Controllers
{
    public class ValuesController : ApiController
    {

        private static int CONNECT_TIMEOUT_MS = 20000;
        // GET api/values
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // POST api/values/
        public JsonResult<Dictionary<string, double>> Post([FromBody]string value)
        {
            KinectSensor kinectSensor = KinectSensor.GetDefault();
            

            kinectSensor.Open();
            int ms_slept = 0;
            while(!kinectSensor.IsAvailable)
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
            while (!dataReceived)
            {
                BodyFrame bodyFrame = null;
                while (bodyFrame == null)
                {
                    bodyFrame = bodyFrameReader.AcquireLatestFrame();
                }
                bodies = new Body[bodyFrame.BodyCount];
                bodyFrame.GetAndRefreshBodyData(bodies);
                if (bodyFrame.BodyCount > 0)
                {
                    dataReceived = true;
                }
            }

            Body body = bodies[0];
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
            var bone = bones.First();
            Joint joint0 = joints[bone.Item1];
            Joint joint1 = joints[bone.Item2];
            Vector3 vect = new Vector3(
                jointPoints[bone.Item2].X - jointPoints[bone.Item1].X,
                jointPoints[bone.Item2].Y - jointPoints[bone.Item1].Y,
                jointPoints[bone.Item2].Z - jointPoints[bone.Item1].Z
                );
            Dictionary<string, double> dict = new Dictionary<string, double>();
            Dictionary<string, Vector3> tiles = new Dictionary<string, Vector3> { { "obj1", new Vector3(0, 1, 1) }, { "obj2", new Vector3(1, 0, 1) } }; //GET from Michael
            foreach (var tile in tiles)
            {
                Vector3 vect2 = new Vector3(
                        tile.Value.X - jointPoints[bone.Item1].X,
                        tile.Value.Y - jointPoints[bone.Item1].Y,
                        tile.Value.Z - jointPoints[bone.Item1].Z
                        );
                double val = 1 / (Vector3.Dot(vect, vect2));
                dict.Add(tile.Key, val);
            }

            return Json(dict);
        }
    }
}
