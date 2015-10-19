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
        // this reference is set by dependancy injection built in OWIN pipeline
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

        [HttpPut]
        [Route("eventhubprocessor/")]
        public async Task Update(Processor newProcessor)
        {
            await ProcessorService.SetAssignedProcessorAsync(newProcessor);
        }

        [HttpGet]
        [Route("eventhubprocessor/")]
        public async Task<ProcessorRuntimeStatus> getStatus()
        {

            ProcessorService.TraceWriter.TraceMessage("Recevied GetStatus Command");
            //scater gather status for each reading. 

            var status = new ProcessorRuntimeStatus();

            status.TotalPostedLastMinute  = await ProcessorService.GetTotalPostedLastMinuteAsync();
            status.TotalProcessedLastMinute = await ProcessorService.GetTotalProcessedLastMinuteAsync();
            status.TotalPostedLastHour = await ProcessorService.GetTotalPostedLastHourAsync();
            status.TotalProcessedLastHour= await ProcessorService.GetTotalProcessedLastHourAsync();
            status.AveragePostedPerMinLastHour = await ProcessorService.GetAveragePostedPerMinLastHourAsync();
            status.AverageProcessedPerMinLastHour = await ProcessorService.GetAverageProcessedPerMinLastHourAsync();
            status.StatusString = await ProcessorService.GetStatusStringAsync();
            status.NumberOfActiveQueues = await ProcessorService.GetNumberOfActiveQueuesAsync();
            status.NumberOfBufferedItems = await ProcessorService.GetNumOfBufferedItemsAsync();


            status.IsInErrorState = ProcessorService.IsInErrorState;
            status.ErrorMessage = ProcessorService.ErrorMessage;
            
            ProcessorService.TraceWriter.TraceMessage("Completed get status Command");
            return status;
        }
    }
}
