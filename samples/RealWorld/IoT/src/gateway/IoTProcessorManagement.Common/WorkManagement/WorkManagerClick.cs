// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTProcessorManagement.Common
{
    public enum WorkerManagerClickType
    {
        Posted,
        Processed
    }
    class WorkManagerClick : IClick<int>, ICloneable
    {
        public WorkerManagerClickType ClickType { get; set; }

        public IClick<int> Next
        {
            get;
            set;
        }

        public int Value
        {
            get;
            set;
        }

        public long When
        {
            get;
            set;
        }

        public  object Clone()
        {
            return new WorkManagerClick()
                                        {
                                            ClickType = this.ClickType,
                                            Value = this.Value,
                                            When = this.When  
                                        };
 
        }
    }
}
