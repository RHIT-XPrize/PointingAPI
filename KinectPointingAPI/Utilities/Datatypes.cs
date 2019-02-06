using System.Collections.Generic;

namespace HRC_Datatypes
{
    public class BlockData
    {
        public int id;
        public int centerX;
        public int centerY;
        public int depth;
        public double rHueVal;
        public double gHueVal;
        public double bHueVal;

        public BlockData(int id, int centerX, int centerY, double rVal, double gVal, double bVal)
        {
            this.id = id;
            this.centerX = centerX;
            this.centerY = centerY;
            this.rHueVal = rVal;
            this.gHueVal = gVal;
            this.bHueVal = bVal;
        }

        public Dictionary<string, double> ConvertToDict()
        {
            Dictionary<string, double> serializedFormat = new Dictionary<string, double>();

            serializedFormat.Add("id", this.id);
            serializedFormat.Add("center_X", this.centerX);
            serializedFormat.Add("center_Y", this.centerY);
            serializedFormat.Add("depth", this.depth);
            serializedFormat.Add("r_hue", this.rHueVal);
            serializedFormat.Add("g_hue", this.gHueVal);
            serializedFormat.Add("b_hue", this.bHueVal);

            return serializedFormat;
        }
    }
}