using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services;
using FabrikamCommon;


//TODO: CONFIRM IF WE ARE USING THIS SERVICE AT ALL... 
namespace StoreView
{
    public interface IStoreView : IService
    {
        Task <IEnumerable<InventoryItemView>> GetViews();
    }
    public sealed class StoreView : StatefulService, IStoreView
    {
        
        private IReliableDictionary<Guid, InventoryItemView> m_inventoryItemViews;
        Task <IEnumerable<InventoryItemView>> IStoreView.GetViews()
        {
            return Task.FromResult(m_inventoryItemViews.Select(kvp => kvp.Value));
        }

        protected override ICommunicationListener CreateCommunicationListener()
        {
            return new ServiceCommunicationListener<IStoreView>(this);
        }


        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            m_inventoryItemViews = await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, InventoryItemView>>("IventoryItemViews");

            /*while (!cancellationToken.IsCancellationRequested)
            {
                using (var tx = this.StateManager.CreateTransaction())
                {
                    var result = await m_inventoryItemViews.TryGetValueAsync(tx, "Counter-1");
                    ServiceEventSource.Current.ServiceMessage(
                        this,
                        "Current Counter Value: {0}",
                        result.HasValue ? result.Value.ToString() : "Value does not exist.");

                    await m_inventoryItemViews.AddOrUpdateAsync(tx, "Counter-1", 0, (k, v) => ++v);

                    await tx.CommitAsync();
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }*/
        }
    }
}
