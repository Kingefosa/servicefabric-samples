using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTProcessorManagement
{
    public static class Ext
    {
        public static void ThrowPowerShell(this AggregateException ae)
        {
            var aeEx = ae.Flatten();

            foreach (var e in aeEx.InnerExceptions)
            {
                Console.WriteLine("Exception:");
                Console.WriteLine(string.Format("Mesage:{0}", e.Message));
                Console.WriteLine(string.Format("StackTrace:{0}", e.StackTrace));
                Console.WriteLine("");

               
            }

            throw new Exception("One or more errors have occured, errors are above");

        }
        
    }
}
