﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamics365.OData.Layout
{
    public class CRMStatusCodeAttribute : Attribute
    {
        public string Name { get; }
        public CRMStatusCodeAttribute(string name = null)
        {
            this.Name = name;
        }
    }
}
