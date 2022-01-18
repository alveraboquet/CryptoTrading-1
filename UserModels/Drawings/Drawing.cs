using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace UserModels
{
    public class Drawing
    {
        public long Id { get; set; }
        public int Type { get; set; }
        public string Data { get; set; }

        #region Relations
        // Layer
        public long LayerId { get; set; }
        public Layer Layer { get; set; }
        #endregion
    }
    public class DrawingRes
    {
        public long Id { get; set; }
        public int Type { get; set; }
        public string Data { get; set; }
    }
}
