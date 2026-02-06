using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    public class TakeoffState
    {
        public List<Quantity> Quantities { get; set; }

        public List<Measurement> Measurements { get; set; }
    }
}
