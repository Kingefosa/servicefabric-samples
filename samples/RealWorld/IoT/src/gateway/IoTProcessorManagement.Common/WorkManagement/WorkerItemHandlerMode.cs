using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTProcessorManagement.Common
{
    public enum WorkItemHandlerMode
    {
        /// <summary>
        /// one Handler will be created for all Work Items 
        /// </summary>
        Singlton,
        /// <summary>
        /// one Handler will be created for every queue of work items
        /// </summary>
        PerQueue,
        /// <summary>
        /// one Handler will be created for every work item handled
        /// </summary>
        PerWorkItem
    }
}
