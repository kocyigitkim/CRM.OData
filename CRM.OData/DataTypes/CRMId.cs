using System;
using System.Collections.Generic;
using System.Text;

namespace Dynamics365.OData.DataTypes
{
    public struct CRMId
    {
        #region Fields
        public static CRMId Empty { get; private set; } = new CRMId();
        public Guid Id { get; private set; }
        public string RawId => Id.ToString();
        #endregion
        #region Construction
        public CRMId(Guid id)
        {
            this.Id = id;
        }
        #endregion
        #region Type Conversations
        public static implicit operator byte[](CRMId id)
        {
            return id.Id.ToByteArray();
        }
        public static implicit operator CRMId(byte[] id)
        {
            try
            {
                return new Guid(id);
            }
            catch (Exception)
            {
                return CRMId.Empty;
            }
        }
        public static implicit operator CRMId(string guidString)
        {
            return Guid.TryParse(guidString, out var id) ? new CRMId(id) : CRMId.Empty;
        }
        public static implicit operator CRMId(Guid id)
        {
            return new CRMId(id);
        }
        public static implicit operator string(CRMId id)
        {
            return id.RawId;
        }
        public static implicit operator Guid(CRMId id)
        {
            return id.Id;
        }
        public override string ToString()
        {
            return this.Id.ToString();
        }
        #endregion
        #region Comparison
        public static bool operator ==(CRMId l, CRMId r)
        {
            return l.Equals(r);
        }
        public static bool operator !=(CRMId l, CRMId r)
        {
            return l.Equals(r) == false;
        }
        public override bool Equals(object obj)
        {
            if (obj is CRMId)
            {
                return this.RawId == ((CRMId)obj).RawId;
            }
            else if (obj is string)
            {
                return this.RawId == (string)obj;
            }
            else if (obj is byte[])
            {
                return this.Equals((CRMId)(byte[])obj);
            }
            else
            {
                return false;
            }
        }
        #endregion
        #region Hashing
        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }
        #endregion
        #region Methods
        public bool Exists()
        {
            return this.Id != Guid.Empty;
        }
        #endregion
    }
}
