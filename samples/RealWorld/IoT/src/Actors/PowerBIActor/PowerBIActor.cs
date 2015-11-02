// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace PowerBIActor
{
    using System;
    using System.Fabric.Description;
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using IoTActor.Common;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Microsoft.ServiceFabric.Actors;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    [ActorGarbageCollection(IdleTimeoutInSeconds = 60, ScanIntervalInSeconds = 10)]
    public class PowerBIActor : Actor<PowerBIActorState>, IIoTActor
    {
        private static int s_MaxEntriesPerRound = 100;
        private IActorTimer m_DequeueTimer = null;
        private bool m_DataSetCreated = false;
        private string m_DataSetID = string.Empty;
        private string m_AddRowsUrl = "https://api.powerbi.com/v1.0/myorg/datasets/{0}/tables/{1}/rows";


        // settings
        private string m_ClientId = string.Empty;
        private string m_Username = string.Empty;
        private string m_Password = string.Empty;
        private string m_PowerBiResource = string.Empty;
        private string m_Authority = string.Empty;
        private string m_PowerBIBaseUrl = string.Empty;
        private string m_DatasetName = string.Empty;
        private string m_TableName = string.Empty;
        private string m_DataSetSchema = string.Empty;

        public Task Post(string DeviceId, string EventHubName, string ServiceBusNS, byte[] Body)
        {
            return Task.Run(
                () =>
                {
                    IoTActorWorkItem Wi = new IoTActorWorkItem();
                    Wi.DeviceId = DeviceId;
                    Wi.EventHubName = EventHubName;
                    Wi.ServiceBusNS = ServiceBusNS;
                    Wi.Body = Body;

                    this.State.Queue.Enqueue(Wi);
                }
                );
        }

        public override async Task OnActivateAsync()
        {
            if (this.State == null)
                this.State = new PowerBIActorState();

            await this.SetConfig();
            this.Host.ActivationContext.ConfigurationPackageModifiedEvent += this.ConfigChanged;
            ActorEventSource.Current.ActorMessage(this, "New Actor On Activate");

            // register a call back timer, that perfoms the actual send to PowerBI
            // has to iterate in less than IdleTimeout 
            this.m_DequeueTimer = this.RegisterTimer(
                this.SendToPowerBIAsync,
                false,
                TimeSpan.FromMilliseconds(10),
                TimeSpan.FromMilliseconds(10));

            await base.OnActivateAsync();
        }


        public override async Task OnDeactivateAsync()
        {
            this.UnregisterTimer(this.m_DequeueTimer); // remove the actor timer
            await this.SendToPowerBIAsync(true); // make sure that no remaining pending records 
            await base.OnDeactivateAsync();
        }

        #region Config Management 

        private void ConfigChanged(object sender, System.Fabric.PackageModifiedEventArgs<System.Fabric.ConfigurationPackage> e)
        {
            this.SetConfig().Wait();
        }

        private async Task SetConfig()
        {
            ConfigurationSettings settingsFile = this.Host.ActivationContext.GetConfigurationPackageObject("Config").Settings;
            ConfigurationSection configSection = settingsFile.Sections["PowerBI"];

            this.m_ClientId = configSection.Parameters["ClientId"].Value;
            this.m_Username = configSection.Parameters["Username"].Value;
            this.m_Password = configSection.Parameters["Password"].Value;
            this.m_PowerBiResource = configSection.Parameters["PowerBIResourceId"].Value;
            this.m_Authority = configSection.Parameters["Authority"].Value;
            this.m_PowerBIBaseUrl = configSection.Parameters["PowerBIBaseUrl"].Value;
            this.m_DatasetName = configSection.Parameters["DatesetName"].Value;
            this.m_TableName = configSection.Parameters["TableName"].Value;


            ActorEventSource.Current.ActorMessage(
                this,
                "Config loaded \n Client:{0} \n Username:{1} \n Password:{2} \n Authority{3} \n PowerBiResource:{4} \n BaseUrl:{5} \n DataSet:{6} \n Table:{7}",
                this.m_ClientId,
                this.m_Username,
                this.m_Password,
                this.m_Authority,
                this.m_PowerBiResource,
                this.m_PowerBIBaseUrl,
                this.m_DatasetName,
                this.m_TableName);


            using (StreamReader sr = new StreamReader(this.Host.ActivationContext.GetDataPackageObject("Data").Path + @"\Datasetschema.json"))
                this.m_DataSetSchema = await sr.ReadToEndAsync();
        }

        #endregion

        #region Power BI Sending Logic

        private async Task<string> GetAuthTokenAsync()
        {
            AuthenticationContext authContext = new AuthenticationContext(this.m_Authority);
            UserCredential userCredential = new UserCredential(this.m_Username, this.m_Password);
            AuthenticationResult result = await authContext.AcquireTokenAsync(this.m_PowerBiResource, this.m_ClientId, userCredential);
            return result.AccessToken;
        }

        private async Task EnsureDataSetCreatedAsync()
        {
            if (this.m_DataSetCreated)
                return;

            HttpRequestMessage ReqMessage;
            HttpResponseMessage ResponseMessage;
            HttpClient httpClient = new HttpClient();

            string AuthToken = await this.GetAuthTokenAsync();

            // get current datasets. 

            ReqMessage = new HttpRequestMessage();
            ReqMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthToken);
            ReqMessage.Method = HttpMethod.Get;
            ReqMessage.RequestUri = new Uri(this.m_PowerBIBaseUrl);
            ResponseMessage = await httpClient.SendAsync(ReqMessage);
            ResponseMessage.EnsureSuccessStatusCode();


            JObject j = JObject.Parse(await ResponseMessage.Content.ReadAsStringAsync());
            JArray arrDs = j["value"] as JArray;

            foreach (JToken entry in arrDs)
            {
                if (null != entry["id"] && this.m_DatasetName == entry["name"].Value<string>())
                {
                    this.m_DataSetID = entry["id"].Value<string>();
                    this.m_DataSetCreated = true;
                    return;
                }
            }


            try
            {
                // not there create it 
                ReqMessage = new HttpRequestMessage();
                ReqMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthToken);
                ReqMessage.Method = HttpMethod.Post;
                ReqMessage.RequestUri = new Uri(this.m_PowerBIBaseUrl);
                ReqMessage.Content = new StringContent(this.m_DataSetSchema, Encoding.UTF8, "application/json");
                ResponseMessage = await httpClient.SendAsync(ReqMessage);
                ResponseMessage.EnsureSuccessStatusCode();

                j = JObject.Parse(await ResponseMessage.Content.ReadAsStringAsync());
                this.m_DataSetID = j["id"].Value<string>();


                ActorEventSource.Current.ActorMessage(this, "Dataset created");
            }
            catch (AggregateException aex)
            {
                AggregateException ae = aex.Flatten();

                foreach (Exception e in ae.InnerExceptions)
                    ActorEventSource.Current.ActorMessage(this, "Error creating dataset E{0} , E:{1}", e.Message, e.StackTrace);

                ActorEventSource.Current.ActorMessage(this, "Error will be ignored and actor will attempt to push the rows");
            }


            this.m_DataSetCreated = true;
        }


        private async Task SendToPowerBIAsync(object IsFinal)
        {
            if (0 == this.State.Queue.Count)
                return;

            await this.EnsureDataSetCreatedAsync();

            bool bFinal = (bool) IsFinal; // as in actor instance is about to get deactivated. 
            Task<string> tAuthToken = this.GetAuthTokenAsync();
            int nCurrent = 0;
            JArray list = new JArray();

            while ((nCurrent <= s_MaxEntriesPerRound || bFinal) && (0 != this.State.Queue.Count))
            {
                list.Add(this.State.Queue.Dequeue().toJObject());
                nCurrent++;
            }


            try
            {
                var all = new {rows = list};
                string sContent = JsonConvert.SerializeObject(all);

                this.m_AddRowsUrl = string.Format(this.m_AddRowsUrl, this.m_DataSetID, this.m_TableName);


                string AuthToken = await tAuthToken;
                HttpClient client = new HttpClient();

                HttpRequestMessage requestMessage = new HttpRequestMessage();
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthToken);
                requestMessage.Method = HttpMethod.Post;
                requestMessage.RequestUri = new Uri(this.m_AddRowsUrl);
                requestMessage.Content = new StringContent(sContent, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.SendAsync(requestMessage);

                response.EnsureSuccessStatusCode();

                ActorEventSource.Current.ActorMessage(this, "Pushed to PowerBI:{0} Remaining {1}", sContent, this.State.Queue.Count);
            }
            catch (AggregateException ae)
            {
                ActorEventSource.Current.ActorMessage(this, "Power BI Actor encontered the followong error sending and will retry ");
                foreach (Exception e in ae.Flatten().InnerExceptions)
                    ActorEventSource.Current.ActorMessage(this, "E:{0} StackTrack:{1}", e.Message, e.StackTrace);
                ActorEventSource.Current.ActorMessage(this, "end of error list ");

                throw; // this will force the actor to be activated somewhere else. 
            }
        }

        #endregion
    }
}