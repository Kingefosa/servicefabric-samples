using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTProcessorManagement.Common
{
    public enum WorkerManagerClickType
    {
        Added,
        Processed
    }
    class WorkManagerClick : Click
    {
        public WorkerManagerClickType ClickType { get; set; }

        public override object Clone()
        {
            var baseClone =  (Click) base.Clone();

            return new WorkManagerClick()
                                        {
                                            ClickType = this.ClickType,
                                            Value = baseClone.Value,
                                            When = baseClone.When  
                                        };
 
        }
    }
}
