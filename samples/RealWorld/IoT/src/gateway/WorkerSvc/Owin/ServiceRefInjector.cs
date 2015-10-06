using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Dependencies;

namespace WorkerSvc
{
    class ServiceRefInjector : IDependencyResolver, IDependencyScope
    {
        public WorkerSvc Svc { get; set; }

        public ServiceRefInjector(WorkerSvc svc)
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

            if (serviceType.GetInterfaces().Contains(typeof(IWorkerController)))
            {
                var ctrl = (IWorkerController)Activator.CreateInstance(serviceType);
                ctrl.WorkerService = this.Svc;
                return ctrl;
            }

            return null;
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return Enumerable.Empty<object>();
        }
    }
