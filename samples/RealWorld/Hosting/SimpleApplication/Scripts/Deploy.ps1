

<#Deploy SimpleWebServer.exe locally#>
Connect-ServiceFabricCluster localhost:19000

Write-Host 'Copying application package...'
Copy-ServiceFabricApplicationPackage -ApplicationPackagePath '..\Service Fabric Package\SingleApplication' -ImageStoreConnectionString 'file:C:\SfDevCluster\Data\ImageStore' -ApplicationPackagePathInImageStore 'Store\WebServer'

Write-Host 'Registering application type...'
Register-ServiceFabricApplicationType -ApplicationPathInImageStore 'Store\WebServer'

New-ServiceFabricApplication -ApplicationName 'fabric:/WebServer' -ApplicationTypeName 'WebServerType' -ApplicationTypeVersion 1.0 


<#Deploy SimpleWebServer.exe to Azure#>
Connect-ServiceFabricCluster yourclusterendpoint

Write-Host 'Copying application package...'
Copy-ServiceFabricApplicationPackage -ApplicationPackagePath '..\Service Fabric Package\SingleApplication' -ImageStoreConnectionString 'fabric:imagestore' -ApplicationPackagePathInImageStore 'WebServer'

Write-Host 'Registering application type...'
Register-ServiceFabricApplicationType -ApplicationPathInImageStore 'WebServer'

New-ServiceFabricApplication -ApplicationName 'fabric:/WebServer' -ApplicationTypeName 'DemoAppType' -ApplicationTypeVersion 1.0