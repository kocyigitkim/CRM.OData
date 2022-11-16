using Dynamics365.OData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamics365.OData.DataTypes
{
    public class CRMMoney
    {
        #region Fields
        public decimal Value { get; set; }
        #endregion
        #region Construction
        public CRMMoney() { }
        public CRMMoney(decimal Value) { this.Value = Value; }
        public CRMMoney(Money money) { this.Value = money.Value; }
        #endregion
        #region Type Conversation
        public static implicit operator Money(CRMMoney money)
        {
            return new Money(money.Value);
        }
        public static implicit operator decimal(CRMMoney money)
        {
            return money.Value;
        }
        public static implicit operator CRMMoney(decimal money)
        {
            return new CRMMoney(money);
        }
        public override string ToString()
        {
            return Value.ToString();
        }
        #endregion
    }
}
