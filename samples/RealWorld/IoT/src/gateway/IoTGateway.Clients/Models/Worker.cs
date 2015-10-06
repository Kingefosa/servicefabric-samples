using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTGateway.Clients
{
    
    public class Worker
    {
        public string Name { get; set; }
        public string ServiceFabricAppTypeName { get; set; }
        public string ServiceFabricAppTypeVersion { get; set; }
        public string ServiceFabricAppName { get; set; }
        public int MaxNumOfWorker { get; set; }
        public WorkerStatus WorkerStatus { get; set; }
        public string ErrorMessage { get; set; }
        public List<EventHubDefinition> Hubs { get; set; }

        public Worker()
        {
            this.WorkerStatus = WorkerStatus.New;
            this.Hubs = new List<EventHubDefinition>();
        }

        public string AsJsonString()
        {
            return  JsonConvert.SerializeObject(this);
        }
        public byte[] AsBytes()
        {
         
            return Encoding.UTF8.GetBytes(AsJsonString());
        }

        public static Worker FromBytes(byte[] bytes)
        {
            var s = Encoding.UTF8.GetString(bytes);
            return Worker.FromJsonString(s);
        }

        public static Worker FromJsonString(string Json)
        {
            return (Worker)JsonConvert.DeserializeObject(Json);
        }

        
        public string[] Validate()
        {
            //todo: validate
            return null;
        }

        public static string[] ValidateWorkerName(string WorkerName)
        {
            if (string.IsNullOrEmpty(WorkerName) || string.IsNullOrWhiteSpace(WorkerName))
                return new string[] { string.Format("bad worker name: {0} ", WorkerName) };

            return null;
        }
    }
}
