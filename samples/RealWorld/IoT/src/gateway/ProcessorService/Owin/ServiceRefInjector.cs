// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

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
        public IoTEventHubProcessorService Svc { get; set; }

        public ServiceRefInjector(IoTEventHubProcessorService svc)
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
