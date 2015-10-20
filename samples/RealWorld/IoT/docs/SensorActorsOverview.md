#Sensor Actor Overview

The Sensor Actor uses [Reliable Actors](https://azure.microsoft.com/en-us/documentation/articles/service-fabric-reliable-actors-introduction/) and represents the current state of any sensor.  Basic methods for getting the last reported state of the sensor are provided in this class.  Also, the SensorActor uses [Actor.Proxy](https://msdn.microsoft.com/en-us/library/azure/dn971900.aspx) forwards the message from the sensor to the [FloorActor](./AggregatorActorsOverview.md) for aggregation and the DataArchiveActor(./DataArchiveActorsOvervie) for archival in storage for later analysis.

