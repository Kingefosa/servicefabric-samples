using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTGateway.Common
{
    class StateManagerLease : Lease
    {
        public static readonly string DefaultEntryNameFormat = "_LEASE-{0}-{1}-{2}-{3}";
        private IReliableStateManager m_StateManager;
        IReliableDictionary<string, string>  m_StateDictionary;
        private string m_EntryName;


        public static string GetDefaultLeaseEntryName(string ServiceBusNamespace, string ConsumerGroupName, string EventHubName, string PartitionId)
        {
            return string.Format(DefaultEntryNameFormat, ServiceBusNamespace, ConsumerGroupName, EventHubName, PartitionId);
        }
        private StateManagerLease(IReliableStateManager StateManager,
                                IReliableDictionary<string, string> StateDictionary, 
                                string EntryName, 
                                string partitionId) 
        {
            m_StateManager = StateManager;
            m_StateDictionary = StateDictionary;
            m_EntryName = EntryName;
            PartitionId = partitionId;

        }

        public static Task<StateManagerLease> GetOrCreateAsync(IReliableStateManager StateManager,
                                                                     IReliableDictionary<string, string> StateDictionary,
                                                                     string ServiceBusNamespace, 
                                                                     string ConsumerGroupName, 
                                                                     string EventHubName, 
                                                                     string PartitionId)
        {
            var defaultEntryName = GetDefaultLeaseEntryName(ServiceBusNamespace, ConsumerGroupName, EventHubName, PartitionId);
            return GetOrCreateAsync(StateManager, StateDictionary, defaultEntryName, PartitionId);
        }

        public static async Task<StateManagerLease> GetOrCreateAsync(IReliableStateManager StateManager,
                                                                     IReliableDictionary<string, string> StateDictionary,
                                                                     string EntryName,
                                                                     string partitionId)
        {
            using (var tx = StateManager.CreateTransaction())
            {
                StateManagerLease lease;
                // if something has been saved before load it
                var cResults = await  StateDictionary.TryGetValueAsync(tx, EntryName);
                if (cResults.HasValue)
                {
                    lease = FromJsonString(cResults.Value);
                }
                else
                {
                    // if not create new
                    lease = new StateManagerLease(StateManager, StateDictionary, EntryName, partitionId);
                }
                await tx.CommitAsync();
                return lease;
            }
        }

        private static StateManagerLease FromJsonString(string sJson)
        {
            return (StateManagerLease) JsonConvert.DeserializeObject(sJson); 
        }
        private string ToJsonString()
        {
            return JsonConvert.SerializeObject(this);
        }
 
        public override bool IsExpired()
        {
            return false; // Service fabric lease does not expire
        }

        public async Task SaveAsync()
        {
            using (var tx = m_StateManager.CreateTransaction())
            {
                // brute force save
                var json = this.ToJsonString();
                await m_StateDictionary.AddOrUpdateAsync(tx, this.m_EntryName, json, (key, val) => { return json; });
                await tx.CommitAsync();
            }
        }
    }
}
