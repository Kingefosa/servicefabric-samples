<#Deploy package SimpleWebServer.exe#>
ServiceFabricAppPackageUtil.exe /source:[directory of SimpleWebServer.exe] /target:[directory that will contain the package] /appname:WebServer /exe:SimpleWebServer.exe




<#Deploy SimpleWebServer.exe locally#>
Connect-ServiceFabricCluster localhost:19000

Write-Host 'Copying application package...'
Copy-ServiceFabricApplicationPackage -ApplicationPackagePath '\WebServerPackage' -ImageStoreConnectionString 'file:C:\SfDevCluster\Data\ImageStore' -ApplicationPackagePathInImageStore 'Store\WebServer'

Write-Host 'Registering application type...'
Register-ServiceFabricApplicationType -ApplicationPathInImageStore 'Store\WebServer'

New-ServiceFabricApplication -ApplicationName 'fabric:/WebServer' -ApplicationTypeName 'WebServerType' -ApplicationTypeVersion 1.0 


<#Deploy SimpleWebServer.exe to Azure#>
Connect-ServiceFabricCluster yourclusterendpoint

Write-Host 'Copying application package...'
Copy-ServiceFabricApplicationPackage -ApplicationPackagePath 'D:\Dev\DevTrainingDemoApp' -ImageStoreConnectionString 'fabric:imagestore' -ApplicationPackagePathInImageStore 'DemoApp'

Write-Host 'Registering application type...'
Register-ServiceFabricApplicationType -ApplicationPathInImageStore 'DemoApp'

New-ServiceFabricApplication -ApplicationName 'fabric:/WebServer' -ApplicationTypeName 'DemoAppType' -ApplicationTypeVersion 1.0