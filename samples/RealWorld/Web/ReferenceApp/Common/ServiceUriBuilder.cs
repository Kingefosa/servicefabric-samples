// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// ------------------------------------------------------------

namespace Common
{
    using System;
    using System.Fabric;
    using System.Globalization;

    public class ServiceUriBuilder
    {
        public ServiceUriBuilder(string serviceInstance)
        {
            this.ServiceInstance = serviceInstance;
        }

        public ServiceUriBuilder(string applicationInstance, string serviceInstance)
        {
            this.ApplicationInstance = applicationInstance;
            this.ServiceInstance = serviceInstance;
        }

        /// <summary>
        /// The name of the application instance that contains he service.
        /// </summary>
        public string ApplicationInstance { get; set; }

        /// <summary>
        /// The name of the service instance.
        /// </summary>
        public string ServiceInstance { get; set; }

        public Uri ToUri()
        {
            var result = String.Format(
                CultureInfo.InvariantCulture,
                "{0}/{1}",
                String.IsNullOrEmpty(this.ApplicationInstance)
                    ? FabricRuntime.GetActivationContext().ApplicationName // the ApplicationName property here automatically prepends "fabric:/" for us
                    : "fabric:/" + this.ApplicationInstance,
                this.ServiceInstance);

            return new Uri(result);
        }
    }
}