using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRM.OData.Layout
{
    public class CRMStateCodeAttribute : Attribute
    {
        public string Name { get; }
        public CRMStateCodeAttribute(string name = null)
        {
            this.Name = name;
        }
    }
}
