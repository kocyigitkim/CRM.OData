using CRM.OData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRM.OData.DataTypes
{
    public class CRMOptionSet
    {
        #region Fields
        public int Value { get; set; }
        #endregion
        #region Construction
        public CRMOptionSet() { }
        public CRMOptionSet(int Value) { this.Value = Value; }
        public CRMOptionSet(OptionSetValue optionset) { this.Value = optionset.Value; }
        #endregion
        #region Type Conversations
        public static implicit operator OptionSetValue(CRMOptionSet optionset)
        {
            return new OptionSetValue(optionset.Value);
        }
        public static implicit operator int(CRMOptionSet optionset)
        {
            return optionset.Value;
        }
        public static implicit operator CRMOptionSet(int value)
        {
            return new CRMOptionSet(value);
        }
        public override string ToString()
        {
            return Value.ToString();
        }
        #endregion
    }
}
