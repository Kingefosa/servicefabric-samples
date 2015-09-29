// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// ------------------------------------------------------------

namespace RestockRequest.Actor
{
    using System;
    using System.Fabric;
    using System.Threading;
    using Microsoft.ServiceFabric.Actors;

    public class ServiceHost
    {
        public static void Main(string[] args)
        {
            try
            {
                using (var fabricRuntime = FabricRuntime.Create())
                {
                    fabricRuntime.RegisterActor(typeof(RestockRequestActor));

                    Thread.Sleep(Timeout.Infinite);
                }
            }
            catch (Exception e)
            {
                ActorEventSource.Current.ActorHostInitializationFailed(e);
                throw;
            }
        }
    }
}