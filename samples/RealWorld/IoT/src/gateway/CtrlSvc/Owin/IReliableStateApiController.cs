using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CtrlSvc
{
    public interface IReliableStateApiController
    {
        IReliableStateManager StateManager { get; set; }
    }
}
