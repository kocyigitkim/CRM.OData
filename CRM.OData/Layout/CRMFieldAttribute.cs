using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.OData.Layout
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class CRMFieldAttribute : Attribute
    {
        #region Fields
        public string Name { get; private set; }
        public bool IsEnum { get; private set; }
        public bool FetchRuntime { get; private set; }
        #endregion
        #region Construction
        public CRMFieldAttribute(string Name, bool IsEnum = false, bool FetchRuntime = false)
        {
            this.Name = Name.Trim();
            this.IsEnum = IsEnum;
            this.FetchRuntime = FetchRuntime;
        }
        #endregion
    }
}
