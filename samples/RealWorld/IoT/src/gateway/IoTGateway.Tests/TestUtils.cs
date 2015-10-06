using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Fabric.Description;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IoTGateway.Tests
{
    static class TestStrings
    {
        public static readonly string FabricEndPoint       = "localhost:19000"; // update if you are using a remote cluster
        public static readonly string GwCtrlAppTypeName    = "IoTGateway";
        public static readonly string GwCtrlAppTypeVersion = "1.0.0.0";
        public static readonly string GwCtrlAppPackagePath = @"C:\repos\ServiceFabricIoT\src\gateway\IoTGatewayCtrl\pkg\Debug"; // update to match your local path



    }
    static class TestUtils
    {
    


        public static async Task<string> GetImageStoreConnectionString(FabricClient fabricClient)
        {
            var clusterManifest = await fabricClient.ClusterManager.GetClusterManifestAsync();
            var doc = XDocument.Parse(clusterManifest);

            var connectionString = doc.Descendants().Single(d => d.Name.LocalName == "Parameter" && d.Attribute("Name").Value == "ImageStoreConnectionString").Attribute("Value").Value;

            Trace.WriteLine(string.Format("Image Store Connection: {0}", connectionString));


            return connectionString;
            
        }

        public static async Task DeployApplicationPackage(FabricClient fabricClient, string PackagePath, string InStorePackagePath)
        {
              var StoreCS = await GetImageStoreConnectionString(fabricClient);
              fabricClient.ApplicationManager.CopyApplicationPackage(StoreCS, PackagePath, InStorePackagePath);
        }

        public static async Task RegisterApplicationType(FabricClient fabricClient, string InStorePackegePath)
        {
            await fabricClient.ApplicationManager.ProvisionApplicationAsync(InStorePackegePath);

        }


        public static async Task CreateAppInstance(FabricClient fabricClient, string sUri, string AppTypeName, string AppTypeVersion)
        {
            ApplicationDescription appDesc = new ApplicationDescription(new Uri(sUri), AppTypeName, AppTypeVersion); 
            await fabricClient.ApplicationManager.CreateApplicationAsync(appDesc);
        }

        public static async Task MakeApp(FabricClient fabricClient, string PackagePath, string InStorePackagePath, string sUri, string AppTypeName, string AppTypeVersion)
        {
            await DeployApplicationPackage(fabricClient, PackagePath, InStorePackagePath);
            await RegisterApplicationType(fabricClient, InStorePackagePath);
            await CreateAppInstance(fabricClient, sUri, AppTypeName, AppTypeVersion);
        }



        public static async Task DeleteAppInstance(FabricClient fabricClient, string sUri)
        {
            await fabricClient.ApplicationManager.DeleteApplicationAsync(new Uri(sUri));
        }

        public static async Task RemoveApplicationPackage(FabricClient fabricClient, string PackagePath)
        {
            
                var storeCS = await GetImageStoreConnectionString(fabricClient);
                fabricClient.ApplicationManager.RemoveApplicationPackage(storeCS, PackagePath);
            
        }

        public static async Task DestroyApp(FabricClient fabricClient, string AppTypeName, string AppTypeVersion, string PackagePath)
        {
            
            // todo: find app instances uusing type name and version and remove them    
            await fabricClient.ApplicationManager.UnprovisionApplicationAsync(AppTypeName, AppTypeVersion);
            await RemoveApplicationPackage(fabricClient, PackagePath);
        }
    }
}
