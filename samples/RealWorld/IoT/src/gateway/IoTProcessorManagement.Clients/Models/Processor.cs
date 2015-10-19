using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTProcessorManagement.Clients
{
    
    public class Processor
    {
        public string Name { get; set; }
        public string ServiceFabricAppTypeName { get; set; }
        public string ServiceFabricAppTypeVersion { get; set; }
        public string ServiceFabricAppInstanceName { get; set; }
        public string ServiceFabricServiceName { get; set; }
        public ProcessorStatus ProcessorStatus { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string ProcessorStatusString
        {
            get
            {
                string statusElement = " {0} ";
                var sb = new StringBuilder();


                if ((this.ProcessorStatus & ProcessorStatus.New) == ProcessorStatus.New)
                    sb.Append(string.Format(statusElement, ProcessorStatus.New));

                if ((this.ProcessorStatus & ProcessorStatus.Provisioned  ) == ProcessorStatus.Provisioned  )
                    sb.Append(string.Format(statusElement, ProcessorStatus.Provisioned ));

                if ((this.ProcessorStatus & ProcessorStatus.PendingDelete) == ProcessorStatus.PendingDelete )
                    sb.Append(string.Format(statusElement, ProcessorStatus.PendingDelete));

                if ((this.ProcessorStatus & ProcessorStatus.Deleted ) == ProcessorStatus.Deleted )
                    sb.Append(string.Format(statusElement, ProcessorStatus.Deleted));

                if ((this.ProcessorStatus & ProcessorStatus.PendingPause)== ProcessorStatus.PendingPause)
                    sb.Append(string.Format(statusElement, ProcessorStatus.PendingPause));

                if ((this.ProcessorStatus & ProcessorStatus.Paused) == ProcessorStatus.Paused)
                    sb.Append(string.Format(statusElement, ProcessorStatus.Paused));

                if ((this.ProcessorStatus & ProcessorStatus.PendingStop) == ProcessorStatus.PendingStop)
                    sb.Append(string.Format(statusElement, ProcessorStatus.PendingStop));

                if ((this.ProcessorStatus & ProcessorStatus.Stopped ) == ProcessorStatus.Stopped)
                    sb.Append(string.Format(statusElement, ProcessorStatus.Stopped));

                if ((this.ProcessorStatus & ProcessorStatus.PendingResume) == ProcessorStatus.PendingResume)
                    sb.Append(string.Format(statusElement, ProcessorStatus.PendingResume));

                if ((this.ProcessorStatus & ProcessorStatus.PendingDrainStop ) == ProcessorStatus.PendingDrainStop)
                    sb.Append(string.Format(statusElement, ProcessorStatus.PendingDrainStop));

                if ((this.ProcessorStatus & ProcessorStatus.ProvisionError ) == ProcessorStatus.ProvisionError )
                    sb.Append(string.Format(statusElement, ProcessorStatus.ProvisionError));

                return sb.ToString();
            }


        }
        public string ErrorMessage { get; set; }
        public List<EventHubDefinition> Hubs { get; set; }

        public bool IsOkToQueueOperation()
        {
            return (
                     (this.ProcessorStatus & ProcessorStatus.PendingDelete) == ProcessorStatus.PendingDelete
                      ||
                     (this.ProcessorStatus & ProcessorStatus.Deleted) == ProcessorStatus.Deleted
                     ||
                     (this.ProcessorStatus & ProcessorStatus.ProvisionError) == ProcessorStatus.ProvisionError

                     );
        }

        public bool IsOkToDelete()
        {
            return (
                     (this.ProcessorStatus & ProcessorStatus.PendingDelete) == ProcessorStatus.PendingDelete
                      ||
                     (this.ProcessorStatus & ProcessorStatus.Deleted) == ProcessorStatus.Deleted
                     );
        }




        public void SafeUpdate(Processor other, bool OverwriteServiceFabricNames = false , bool OverrideHubsConfig = false)
        {
            if (this.Name != other.Name)
                throw new InvalidOperationException(string.Format("Safe update failed: processor name {0}  != other name {1}", this.Name, other.Name));

            if (this.ServiceFabricAppTypeName != other.ServiceFabricAppTypeName)
                this.ServiceFabricAppTypeName = other.ServiceFabricAppTypeName;

            if (OverwriteServiceFabricNames)
            { 
                if (this.ServiceFabricAppTypeVersion != other.ServiceFabricAppTypeVersion)
                    this.ServiceFabricAppTypeVersion = other.ServiceFabricAppTypeVersion;

                if (this.ServiceFabricAppInstanceName != other.ServiceFabricAppInstanceName)
                    this.ServiceFabricAppInstanceName = other.ServiceFabricAppInstanceName;

                if (this.ServiceFabricServiceName != other.ServiceFabricServiceName)
                    this.ServiceFabricServiceName = other.ServiceFabricServiceName;
            }

            if (OverrideHubsConfig)
                this.Hubs = other.Hubs;
            


            this.ProcessorStatus |= other.ProcessorStatus;

            if (this.ErrorMessage != other.ErrorMessage)
                this.ErrorMessage = other.ErrorMessage;
        }


        public Processor()
        {
            this.ProcessorStatus = ProcessorStatus.New;
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

        
        
        public string[] Validate()
        {
            var errors = new List<string>();

            var NameValidationErrors = ValidateProcessName(this.Name);

            if (null != NameValidationErrors)
                errors.AddRange(NameValidationErrors);

            if (0 == Hubs.Count)
                errors.Add("Worker does not contain any Event Hub Definition(s), at least one is needed");

            var count = 1;
            foreach (var hub in Hubs)
            {
                if (string.IsNullOrEmpty(hub.EventHubName) || string.IsNullOrWhiteSpace(hub.EventHubName))
                    errors.Add(string.Format("Event Hub Definition {0} has empty Name", count));

                if (string.IsNullOrEmpty(hub.ConnectionString) || string.IsNullOrWhiteSpace(hub.ConnectionString))
                    errors.Add(string.Format("Event Hub Definition {0} has empty Name", count));

                count++;
            }


            return errors.Count > 0 ? errors.ToArray() : null;
        }

        public static string[] ValidateProcessName(string WorkerName)
        {
            if (string.IsNullOrEmpty(WorkerName) || string.IsNullOrWhiteSpace(WorkerName))
                return new string[] { string.Format("bad worker name: {0} ", WorkerName) };

            return null;
        }

        public static Processor FromBytes(byte[] bytes)
        {
            var s = Encoding.UTF8.GetString(bytes);
            return Processor.FromJsonString(@s);
        }

        public static Processor FromJsonString(string Json)
        {
            return JsonConvert.DeserializeObject<Processor>(Json);
        }

    }
}
