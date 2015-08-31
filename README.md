# Azure Service Fabric Samples (*PREVIEW Branch*)

This branch contains version of the Service Fabric samples that have been updated to work with the latest private preview release of the Service Fabric SDK. If you are using the public SDK (linked from http://aka.ms/servicefabricsdk), please use the master branch. 

## How the samples are organized

### By Programming Model
The repo is divided into Actor samples and Services samples, corresponding to the two programming models available for Service Fabric, the Reliable Actors API and the Reliable Services API. You may want to read up on [how the two models compare][2] before diving in.

### By Development Environment

The samples are further divided into Visual Studio 2015 and Visual Studio 2013. Only VS 2015 supports the tooling add-in that enables packaging, deployment, and debugging directly from Visual Studio on your local cluster, so that is the preferred approach and offers the greatest number of samples. A subset of the VS 2015 samples are provided for VS 2013 but these require manual deployment from PowerShell and do not offer automatic debugger integration.

To set up your dev environment for use with Visual Studio 2015, simply follow the [getting started guide][3].

To use Visual Studio 2013, follow the same instructions but download the core SDK from [here][4] instead of the full SDK linked in the getting started guide. All other instructions remain the same. Each VS 2013 solution contains a Scripts directory that includes Deploy and Remove scripts that you can use to manage the apps in PowerShell.


## Learn more

To learn more about Service Fabric, check out the [platform documentation](http://aka.ms/servicefabricdocs) on azure.com.

[1]: http://aka.ms/servicefabric "Service Fabric campaign page"
[2]: http://azure.microsoft.com/en-us/documentation/articles/service-fabric-choose-framework/ "Choose framework article"
[3]: http://aka.ms/servicefabricsdk "Setup with VS 2015"
[4]: http://www.microsoft.com/web/handlers/webpi.ashx?command=getinstallerredirect&appid=ServiceFabricSDK "Core SDK download"
