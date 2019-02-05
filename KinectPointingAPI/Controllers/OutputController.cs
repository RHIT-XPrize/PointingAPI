using System.Collections.Generic;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using System.Web.Http.Results;
using System.Threading.Tasks;

using HRC_Datatypes;

namespace KinectPointingAPI.Controllers
{
    public class OutputController : ApiController
    {

        [HttpPost]
        public JsonResult<Dictionary<string, double>> Post()
        {
            string casJSON = this.ParsePostBody();

            return Json(new Dictionary<string, double>());
        }

        private string ParsePostBody()
        {
            Task<string> task = this.GetPostBody();

            string casJSON = "";
            try
            {
                task.Wait();
                casJSON = task.Result;
            }
            catch
            {
                System.Environment.Exit(-1);
            }

            JObject payloadContent = JObject.Parse(casJSON);
            JToken allAnnotations = payloadContent["_views"]["_InitialView"];
            JToken detectedBlocks = allAnnotations["DetectedBlock"];
            foreach (JToken blockString in detectedBlocks)
            {
                int id = blockString["id"].ToObject<int>();
                int centerX = blockString["center_X"].ToObject<int>();
                int centerY = blockString["center_Y"].ToObject<int>();
                double rHue = blockString["r_hue"].ToObject<double>();
                double gHue = blockString["g_hue"].ToObject<double>();
                double bHue = blockString["b_hue"].ToObject<double>();

                BlockData block = new BlockData(id, centerX, centerY, rHue, gHue, bHue);
            }
            return casJSON;
        }

        private async Task<string> GetPostBody()
        {
            return await Request.Content.ReadAsStringAsync();
        }
    }
}
