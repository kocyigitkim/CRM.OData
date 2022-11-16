using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamics365.OData.DataTypes
{
    public class NullBase
    {
        #region Construction
        public NullBase() { Value = null; }
        public NullBase(object value) { this.Value = value; }
        #endregion
        #region Fields
        public bool HasValue => this != null && Value != null;
        public virtual object Value { get; set; }
        #endregion
        #region Type Conversations
        public override string ToString()
        {
            return HasValue ? Value.ToString() : "null";
        }
        #endregion
    }
}
