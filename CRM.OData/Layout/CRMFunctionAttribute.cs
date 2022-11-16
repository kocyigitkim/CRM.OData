﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamics365.OData.Layout
{
    public class CRMFunctionAttribute : Attribute
    {
        public string Name { get; }
        public CRMFunctionAttribute(string Name)
        {
            this.Name = Name;
        }
    }
}
