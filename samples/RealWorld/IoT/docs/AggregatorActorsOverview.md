#Aggregator Actor Overview#

The FloorActor uses [Reliable Actors](https://azure.microsoft.com/en-us/documentation/articles/service-fabric-reliable-actors-introduction/) and represents the recent history of a group of sensors. A timer is used to routinely purge old data. 

The FloorActor keeps a list of recent sensor messages for a specific floor.  There is one FloorActor for each floor.  [Actor.Proxy](https://msdn.microsoft.com/en-us/library/azure/dn971900.aspx) is used to connect to a specific floor by passing in an actor id based on the floor id.  

