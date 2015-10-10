using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTProcessorManagement.Common
{
    public class Click : ICloneable
    {
        public long When { get; internal set; } = DateTime.UtcNow.Ticks;
        public Click Next { get; internal set; }
        public virtual long Value { get; set; }
        internal Click(Click next)
        {
            Next = next;
        }

        public Click()
        {

        }

        public virtual object Clone()
        {
            return new Click() { When = this.When, Next = this.Next, Value = this.Value };
        }
    }
}
