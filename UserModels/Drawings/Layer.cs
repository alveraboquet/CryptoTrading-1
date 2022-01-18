using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserModels
{
    public class Layer
    {
        public Layer()
        {  }

        public long Id { get; set; }
        public string Exchange { get; set; }
        public string Symbol { get; set; }
        public string Name { get; set; }
        public bool IsDefault { get; set; }

        #region Relations
        // User
        public int UserId { get; set; }
        public virtual User User { get; set; }

        // Drawing
        public virtual ICollection<Drawing> Drawings { get; set; }
        #endregion
    }
}
