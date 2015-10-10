using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Fabric;
using System.Diagnostics;

namespace IoTProcessorManagement.Tests
{
    [TestClass]
    public class IoTManagementTest
    {
        private static string s_GWAppPackageInStorePath = string.Concat(@"Test\", TestStrings.GwCtrlAppTypeName, TestStrings.GwCtrlAppTypeVersion);
        private static string s_GwAppInstanceName = "fabric:/IoTGatewatyApp";



        private async Task cleanFabric()
        {


            FabricClient fc = new FabricClient(TestStrings.FabricEndPoint);
            try
            {
                await TestUtils.DeleteAppInstance(fc, s_GwAppInstanceName);
                await TestUtils.DestroyApp(fc, TestStrings.GwCtrlAppTypeName, TestStrings.GwCtrlAppTypeVersion, s_GWAppPackageInStorePath);
            }
            catch (FabricElementNotFoundException e)
            {
                Trace.WriteLine(string.Format("Attempting to clean Fabric from apps {0} resuled it in an expected error, will keep working normally E:{1} Stack Trace:{2}", s_GwAppInstanceName, e.Message, e.StackTrace));
            }
            catch (Exception E)
            {
                Trace.WriteLine(string.Format("Attempting to clean Fabric from apps {0} resuled it in an unexpected error, will EXIT! E:{1} Stack Trace:{2}", s_GwAppInstanceName, E.Message, E.StackTrace));
                throw;
            }

        }
        private async Task setFabric()
        {
            await cleanFabric();
            /*
            FabricClient fc = new FabricClient(TestStrings.FabricEndPoint);
            //1- copy the gateway package
            await TestUtils.DeployApplicationPackage(fc, TestStrings.GwCtrlAppPackagePath, s_GWAppPackageInStorePath);
            // 1.1 reguster app type
            await TestUtils.RegisterApplicationType(fc, s_GWAppPackageInStorePath);
            // 2- create instance of it
            await TestUtils.CreateAppInstance(fc, s_GwAppInstanceName, TestStrings.GwCtrlAppTypeName, TestStrings.GwCtrlAppTypeVersion);
            // 3- give it 10 seconds to start.
            await Task.Delay(10 * 1000);
            */
        }


        [TestInitialize]
        public void Setup()
        {
            setFabric().Wait();

        }

        [TestCleanup]
        public void Clean()
        {
            cleanFabric().Wait();
        }



        [TestMethod]
        public void TestMethod1()
        {
            

        }
    }
}
