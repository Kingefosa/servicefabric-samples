﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// ------------------------------------------------------------

namespace RestockRequest.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Services;
    using RestockRequest.Domain;
    using RestockRequestManager.Domain;

    internal class Program
    {
        private const int MaxNumberOfItems = 100000;
        private const int ParallelRequests = 100;
        public static int GenerateDataIntervalInMsec = 30*1000;
        private static Uri RestockRequestManagerServiceName = new Uri("fabric:/FabrikamReferenceApplication/RestockRequestManager");
        private static Random random = new Random((int) DateTime.Now.Ticks);
        private static List<Guid> Items;

        private static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                var endpoint = args[0];
                Console.WriteLine("Conencting to cluster: " + endpoint);
                var resolver = new ServicePartitionResolver(endpoint);
                ServicePartitionResolver.SetDefault(resolver);
            }

            Items = new List<Guid>();
            for (var i = 0; i < MaxNumberOfItems; i++)
            {
                Items.Add(Guid.NewGuid());
            }

            var timer = new Timer(new TimerCallback(GenerateData), null, 0, GenerateDataIntervalInMsec);
            Console.ReadLine();
            timer.Dispose();
        }

        private static void GenerateData(object state)
        {
            AddRestockRequestsAsync();
        }

        private static async void AddRestockRequestsAsync()
        {
            // TODO: need to go to correct partition
            // For now, the inventory is not partitioned, so always go to first partition
            var restockRequestService = ServiceProxy.Create<IRestockRequestManager>(0, RestockRequestManagerServiceName);

            IList<Task> tasks = new List<Task>();
            for (var i = 0; i < ParallelRequests; i++)
            {
                var request = GenerateRandomRequest();
                Console.WriteLine("Add request {0}", request);

                tasks.Add(restockRequestService.AddRestockRequestAsync(request));
            }

            await Task.WhenAll(tasks);
        }

        private static RestockRequest GenerateRandomRequest()
        {
            var index = random.Next(0, MaxNumberOfItems);
            return new RestockRequest(Items[index], random.Next(10, 100000));
        }
    }
}