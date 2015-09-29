// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// ------------------------------------------------------------

namespace CustomerOrder.Actor
{
    using System;
    using System.Fabric;
    using System.Threading;
    using Microsoft.ServiceFabric.Actors;

    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                using (var fabricRuntime = FabricRuntime.Create())
                {
                    fabricRuntime.RegisterActor(typeof(CustomerOrderActor));

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