using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTGateway.Common
{
    public interface IOwinListenerSpec
    {
        void CreateOwinPipeline(IAppBuilder app);
    }
}
