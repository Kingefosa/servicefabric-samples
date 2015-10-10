using IoTProcessorManagement.Clients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace EventHubProcessor
{
   public class EventHubProcessorController : ApiController, IEventHubProcessorController
    {
        public EventHubProcessorService ProcessorService { get; set; }

        [HttpPost]
        [Route("eventhubprocessor/pause")]
        public async Task Pause()
        {
            ProcessorService.TraceWriter.TraceMessage("Recevied Pause Command");
            await ProcessorService.Pause();
            ProcessorService.TraceWriter.TraceMessage("Completed Pause Command");
        }

        [HttpPost]
        [Route("eventhubprocessor/stop")]
        public async Task Stop()
        {
            ProcessorService.TraceWriter.TraceMessage("Recevied Stop Command");
            await ProcessorService.Stop();
            ProcessorService.TraceWriter.TraceMessage("Completed Stop Command");
        }

        [HttpPost]
        [Route("eventhubprocessor/resume")]
        public async Task Resume()
        {
            ProcessorService.TraceWriter.TraceMessage("Recevied Resume Command");
            await ProcessorService.Resume();
            ProcessorService.TraceWriter.TraceMessage("Completed Resume Command");
        }


        [HttpPost]
        [Route("eventhubprocessor/drainstop")]
        public async Task DrainStop()
        {
            ProcessorService.TraceWriter.TraceMessage("Recevied Drain/Stop Command");
            await ProcessorService.DrainAndStop();
            ProcessorService.TraceWriter.TraceMessage("Completed Drain/Stop Command");
        }

        [HttpGet]
        public async Task<ProcessorRuntimeStatus> getStatus()
        {

            ProcessorService.TraceWriter.TraceMessage("Recevied GetStatus Command");
            //scater gather status for each reading. 

            var status = new ProcessorRuntimeStatus();

            var TotalPostedLastMinuteTask = ProcessorService.GetTotalPostedLastMinute();
            var TotalProcessedLastMinuteTask = ProcessorService.GetTotalProcessedLastMinute();
            var TotalPostedLastHourTask = ProcessorService.GetTotalPostedLastHour();
            var TotalProcessedLastHourTask = ProcessorService.GetTotalProcessedLastHour();
            var AveragePostedPerMinLastHourTask = ProcessorService.GetAveragePostedPerMinLastHour();
            var AverageProcessedPerMinLastHourTask = ProcessorService.GetAverageProcessedPerMinLastHour();
            var StatusStringTask = ProcessorService.GetStatusString();
            var NumOfActiveQueuesTask = ProcessorService.GetNumberOfActiveQueues();

            await Task.WhenAll(
                                 TotalPostedLastMinuteTask, 
                                 TotalProcessedLastMinuteTask ,
                                 TotalPostedLastHourTask, 
                                 TotalProcessedLastHourTask,
                                 AveragePostedPerMinLastHourTask, 
                                 AverageProcessedPerMinLastHourTask,
                                 StatusStringTask,
                                 NumOfActiveQueuesTask
                             );
                
            status.TotalPostedLastMinute = TotalPostedLastMinuteTask.Result;
            status.TotalProcessedLastMinute  = TotalProcessedLastMinuteTask.Result;
            status.TotalPostedLastHour  = TotalPostedLastHourTask.Result;
            status.TotalProcessedLastHour = TotalProcessedLastHourTask.Result;
            status.AveragePostedPerMinLastHour = AveragePostedPerMinLastHourTask.Result;
            status.AverageProcessedPerMinLastHour = AverageProcessedPerMinLastHourTask.Result;
            status.StatusString = StatusStringTask.Result;
            status.NumberOfActiveQueues = NumOfActiveQueuesTask.Result;


            ProcessorService.TraceWriter.TraceMessage("Completed Pause Command");
            return status;
        }
    }
}
