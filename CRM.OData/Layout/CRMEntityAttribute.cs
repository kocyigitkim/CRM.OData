using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CRM.OData.Layout
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
    public class CRMEntityAttribute : Attribute
    {
        public string Name { get; private set; }
        public string[] Names { get; private set; }
        public CRMEntityAttribute(params string[] names)
        {
            this.Name = names.FirstOrDefault().Trim();
            this.Names = names.Select(p => p.Trim()).ToArray();
        }
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class DisableEntityNameConvertion : Attribute { }
}
