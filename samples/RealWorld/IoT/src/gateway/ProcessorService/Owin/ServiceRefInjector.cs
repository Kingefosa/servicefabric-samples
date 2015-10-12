using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Dependencies;

namespace EventHubProcessor
{
    class ServiceRefInjector : IDependencyResolver, IDependencyScope
    {
        public EventHubProcessorService Svc { get; set; }

        public ServiceRefInjector(EventHubProcessorService svc)
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

            if (serviceType.GetInterfaces().Contains(typeof(IEventHubProcessorController)))
            {
                var ctrl = (IEventHubProcessorController)Activator.CreateInstance(serviceType);
                ctrl.ProcessorService = this.Svc;
                return ctrl;
            }

            return null;
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return Enumerable.Empty<object>();
        }
    }
}
