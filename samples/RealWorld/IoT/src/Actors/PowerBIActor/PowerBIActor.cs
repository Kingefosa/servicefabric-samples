// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Actors;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.IO;
using IoTActor.Common;

namespace PowerBIActor
{

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


        #region Config Management 
        private void ConfigChanged(object sender, System.Fabric.PackageModifiedEventArgs<System.Fabric.ConfigurationPackage> e)
        {
            SetConfig().Wait();
        }
        private async Task SetConfig()
        {
            var settingsFile = Host.ActivationContext.GetConfigurationPackageObject("Config").Settings;
            var configSection = settingsFile.Sections["PowerBI"];

            m_ClientId = configSection.Parameters["ClientId"].Value;
            m_Username = configSection.Parameters["Username"].Value;
            m_Password = configSection.Parameters["Password"].Value;
            m_PowerBiResource = configSection.Parameters["PowerBIResourceId"].Value;
            m_Authority = configSection.Parameters["Authority"].Value;
            m_PowerBIBaseUrl = configSection.Parameters["PowerBIBaseUrl"].Value;
            m_DatasetName = configSection.Parameters["DatesetName"].Value;
            m_TableName = configSection.Parameters["TableName"].Value;



            ActorEventSource.Current.ActorMessage(this, "Config loaded \n Client:{0} \n Username:{1} \n Password:{2} \n Authority{3} \n PowerBiResource:{4} \n BaseUrl:{5} \n DataSet:{6} \n Table:{7}", 
                                                         m_ClientId, m_Username,m_Password, m_Authority, m_PowerBiResource, m_PowerBIBaseUrl, m_DatasetName, m_TableName);


            using (StreamReader sr = new StreamReader(Host.ActivationContext.GetDataPackageObject("Data").Path + @"\Datasetschema.json"))
                m_DataSetSchema = await sr.ReadToEndAsync();

            
        }
        #endregion  

        public override async Task OnActivateAsync()
        {
            if (this.State == null)
                this.State = new PowerBIActorState();

            await SetConfig();
            Host.ActivationContext.ConfigurationPackageModifiedEvent += ConfigChanged;
            ActorEventSource.Current.ActorMessage(this, "New Actor On Activate");

            // register a call back timer, that perfoms the actual send to PowerBI
            // has to iterate in less than IdleTimeout 
            m_DequeueTimer = RegisterTimer(
                                            SendToPowerBIAsync,                     
                                            false,                           
                                            TimeSpan.FromMilliseconds(8),   
                                            TimeSpan.FromMilliseconds(8)); 

            await base.OnActivateAsync();
        }

        

        public override async Task OnDeactivateAsync()
        {
            UnregisterTimer(m_DequeueTimer); // remove the actor timer
            await SendToPowerBIAsync(true); // make sure that no remaining pending records 
            await base.OnDeactivateAsync();
        }

        public Task Post(string DeviceId, string EventHubName, string ServiceBusNS, byte[] Body)
        {
            return Task.Run(() =>
                        {
                            var Wi = new IoTActorWorkItem();
                            Wi.DeviceId = DeviceId;
                            Wi.EventHubName = EventHubName;
                            Wi.ServiceBusNS = ServiceBusNS;
                            Wi.Body = Body;

                            State.Queue.Enqueue(Wi);
                        }
                    );
        }



        #region Power BI Sending Logic
        private async Task<string> GetAuthTokenAsync()
        {
            var authContext = new AuthenticationContext(m_Authority);
            var userCredential = new UserCredential(m_Username, m_Password);
            var result = await authContext.AcquireTokenAsync(m_PowerBiResource, m_ClientId, userCredential);
            return result.AccessToken;
        }
        private async Task EnsureDataSetCreatedAsync()
        {
            if (m_DataSetCreated)
                return;

            HttpRequestMessage ReqMessage;
            HttpResponseMessage ResponseMessage;
            HttpClient httpClient = new HttpClient();

            var AuthToken = await GetAuthTokenAsync();

            // get current datasets. 

            ReqMessage = new HttpRequestMessage();
            ReqMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthToken);
            ReqMessage.Method = HttpMethod.Get;
            ReqMessage.RequestUri = new Uri(m_PowerBIBaseUrl);
            ResponseMessage = await httpClient.SendAsync(ReqMessage);
            ResponseMessage.EnsureSuccessStatusCode();



            JObject j = JObject.Parse(await ResponseMessage.Content.ReadAsStringAsync());
            var arrDs = j["value"] as JArray;

            foreach (var entry in arrDs)
            {
                if (null != entry["id"] && m_DatasetName == entry["name"].Value<string>())
                {
                    m_DataSetID = entry["id"].Value<string>();
                    m_DataSetCreated = true;
                    return;
                }

            }



            try
            {
                // not there create it 
                ReqMessage = new HttpRequestMessage();
                ReqMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthToken);
                ReqMessage.Method = HttpMethod.Post;
                ReqMessage.RequestUri = new Uri(m_PowerBIBaseUrl);
                ReqMessage.Content = new StringContent(m_DataSetSchema, Encoding.UTF8, "application/json");
                ResponseMessage = await httpClient.SendAsync(ReqMessage);
                ResponseMessage.EnsureSuccessStatusCode();

                j = JObject.Parse(await ResponseMessage.Content.ReadAsStringAsync());
                m_DataSetID = j["id"].Value<string>();


                ActorEventSource.Current.ActorMessage(this, "Dataset created");


            }
            catch (AggregateException aex)
            {
                var ae = aex.Flatten();

                foreach(var e in ae.InnerExceptions)
                    ActorEventSource.Current.ActorMessage(this, "Error creating dataset E{0} , E:{1}", e.Message, e.StackTrace);

                ActorEventSource.Current.ActorMessage(this, "Error will be ignored and actor will attempt to push the rows");


            }


            m_DataSetCreated = true;
        }


        private async Task SendToPowerBIAsync(object IsFinal)
        {
            if (0 == State.Queue.Count )
                return;

            await EnsureDataSetCreatedAsync();

            var bFinal = (bool) IsFinal; // as in actor instance is about to get deactivated. 
            var tAuthToken = GetAuthTokenAsync(); 
            var nCurrent = 0;
            var list = new JArray();

             while ((nCurrent <= s_MaxEntriesPerRound || bFinal) && (0 != State.Queue.Count))
            {
                list.Add(State.Queue.Dequeue().toJObject());
                nCurrent++;                   
            }


            try
            {
                var all = new { rows = list };
                var sContent = JsonConvert.SerializeObject(all);

                m_AddRowsUrl = string.Format(m_AddRowsUrl,m_DataSetID, m_TableName);


                var AuthToken = await tAuthToken;
                var client = new HttpClient();
                
                HttpRequestMessage requestMessage = new HttpRequestMessage();
                requestMessage.Headers.Authorization =  new AuthenticationHeaderValue("Bearer", AuthToken);
                requestMessage.Method = HttpMethod.Post;
                requestMessage.RequestUri = new Uri(m_AddRowsUrl);
                requestMessage.Content = new StringContent(sContent, Encoding.UTF8, "application/json");
                var response = await client.SendAsync(requestMessage);

                response.EnsureSuccessStatusCode();

                ActorEventSource.Current.ActorMessage(this, "Pushed to PowerBI:{0} Remaining {1}", sContent, State.Queue.Count);

            }
            catch (AggregateException ae)
            {
                ActorEventSource.Current.ActorMessage(this, "Power BI Actor encontered the followong error sending and will retry ");
                foreach (var e in ae.Flatten().InnerExceptions)
                ActorEventSource.Current.ActorMessage(this, "E:{0} StackTrack:{1}", e.Message, e.StackTrace);
                ActorEventSource.Current.ActorMessage(this, "end of error list ");

                throw; // this will force the actor to be activated somewhere else. 
            }
        }



        #endregion
    }
}
