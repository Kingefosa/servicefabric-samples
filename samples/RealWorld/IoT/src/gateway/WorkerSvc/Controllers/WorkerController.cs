using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace WorkerSvc
{
    class WorkerController : ApiController, IWorkerController
    {
        public WorkerSvc WorkerService { get; set; }

        [HttpPost]
        [Route("pause")]
        public async Task Pause()
        {
              
        }

        [HttpPost]
        [Route("stop")]
        public async Task Stop()
        {

        }

        [HttpPost]
        [Route("resume")]
        public async Task Resume()
        {

        }


        [HttpPost]
        [Route("drainstop")]
        public async Task DrainStop()
        {

        }

        [HttpGet]
        public async Task<string> getStatus()
        {
            return null;
        }
    }
}
