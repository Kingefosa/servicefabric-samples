using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Dependencies;

namespace IoTProcessorManagementService
{
    class ProcessorManagementServiceRefInjector : IDependencyResolver, IDependencyScope
    {
        public ProcessorManagementService Svc { get; set; }

        public ProcessorManagementServiceRefInjector(ProcessorManagementService svc    )
        {
            Svc = svc;

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
            
            if (serviceType.GetInterfaces().Contains(typeof(ProcessorManagementServiceApiController)))
            {
                var reliableStateCtrl = (ProcessorManagementServiceApiController)Activator.CreateInstance(serviceType);
                reliableStateCtrl.Svc = this.Svc;
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
