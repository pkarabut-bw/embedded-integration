using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    public class TakeoffActionsList
    {
        public Guid ProjectId { get; set; }

        public List<TakeoffAction> Actions { get; set; }
    }
}
