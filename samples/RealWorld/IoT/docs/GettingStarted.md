# Getting Started#

## Set up your machine ##
1. Install [Visual Studio 2015 RC](http://go.microsoft.com/fwlink/?LinkId=517106)
2. Install [Service Fabric SDK](https://azure.microsoft.com/en-us/documentation/articles/service-fabric-get-started/)

## Get the source code ##
1. Open Visual Studio 2015
2. Go to View -> Team Explorer
3. On the Connect page of the Team Explorer window, click the Clone dropdown located under the Local Git Repositories section
4. Enter the URL https://github.com/Azure-Samples/*(TODO:get final url)*
5. If desired, change the local repository path
6. Click the Clone button

## Begin working with the IoT solution ##
1. On the Home page of the Team Explorer window, open the solution by double-clicking IoT.sln listed under the Solutions section.  If you do not see it listed under the Solutions section, click the Open... link and navigate the local repository folder to open src/IoT.sln.
2. After opening the solution, wait for the Output window Package Manager pane to show "Restore complete" and "Total time" messages.
3. Go to Build -> Build Solution.
4. Go to Debug -> Start Debugging to launch the solution.  See [how to use sample](./HowTo.md) for further instructions.

*TODO: Add instructions for creating Azure assets (storage, event hub, etc) used by solution*

## Resources ##
- [How to use sample](./HowTo.md) *TODO*
- [Architecture of solution](./Architecture.md)
	- [Management Gateway overview](./GatewayOverview.md) *TODO*
	- [Sensor actors overview](./SensorActorsOverview.md) *TODO*
	- [Aggregator actors overview](./AggregatorActorsOverview.md) *TODO*
	- [Storage actors overview](./DataArchiveActorsOverview.md) *TODO*
	- [PowerBI actors overview](./PowerBIActorsOverview.md) *TODO*
- Solution Deployment *TODO*
- [Service Fabric documentation](https://azure.microsoft.com/en-us/documentation/services/service-fabric/)
- [Azure Code Samples](https://azure.microsoft.com/en-us/documentation/samples/)

