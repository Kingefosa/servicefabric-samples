﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTGateway.Common
{
    public interface IWorkItemHandler<Wi> where Wi : IWorkItem 
    {
        Task<Wi> HandleWorkItem(Wi workItem);
    }
}
