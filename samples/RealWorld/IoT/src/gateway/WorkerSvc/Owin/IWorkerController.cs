using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerSvc
{
    public interface IWorkerController
    {
         WorkerSvc WorkerService { get; set; }
    }
}
