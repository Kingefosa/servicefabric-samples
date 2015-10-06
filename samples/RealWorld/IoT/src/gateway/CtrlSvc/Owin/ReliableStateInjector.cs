using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Dependencies;

namespace CtrlSvc
{
    class ReliableStateInjector : IDependencyResolver, IDependencyScope
    {
        public IReliableStateManager StateManager { get; set; }

        public ReliableStateInjector(IReliableStateManager stateManager)
        {
            StateManager = stateManager;
        }

        public IDependencyScope BeginScope()
        {
            return this;
        }

        public void Dispose()
        {
            // no op
        }

        public object GetService(Type serviceType)
        {
            
            if (serviceType.GetInterfaces().Contains(typeof(IReliableStateApiController)))
            {
                var reliableStateCtrl = (IReliableStateApiController)Activator.CreateInstance(serviceType);
                reliableStateCtrl.StateManager = this.StateManager;
                return reliableStateCtrl;
            }

                return null;
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return Enumerable.Empty<object>();
        }
    }
}
