using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    public class TakeoffAction
    {
        public string ActionName { get; set; }

        public string EntityType { get; set; }

        public int OrderNumber { get; set; }

        public Measurement Measurement { get; set; }

        public Quantity Quantity { get; set; }
    }
}
