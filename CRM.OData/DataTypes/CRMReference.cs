using Dynamics365.OData;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dynamics365.OData.DataTypes
{
    public struct CRMReference
    {
        #region Construction
        public CRMReference(CRMId id, string name) : this()
        {
            this.Id = id;
            this.LogicalName = name;
        }
        #endregion
        #region Fields
        public static CRMReference Null => new CRMReference();
        public string LogicalName { get; internal set; }
        public CRMId Id { get; internal set; }
        #endregion
        #region Type Conversations
        public static implicit operator CRMId(CRMReference reference)
        {
            return reference.Id;
        }
        public static implicit operator EntityReference(CRMReference reference)
        {
            if (reference == null) return null;
            return new EntityReference(reference.LogicalName, reference.Id);
        }
        public static implicit operator CRMReference(EntityReference reference)
        {
            if (reference == null) return CRMReference.Null;
            return new CRMReference(reference.Id, reference.LogicalName);
        }
        public override string ToString()
        {
            return $"{LogicalName}:{Id}";
        }
        #endregion
        #region Comparison
        public static bool operator ==(CRMReference left, CRMReference right)
        {
            return left.LogicalName == right.LogicalName && left.Id == right.Id;
        }
        public static bool operator !=(CRMReference left, CRMReference right)
        {
            return left.LogicalName != right.LogicalName && left.Id != right.Id;
        }
        public static bool operator ==(CRMReference left, object right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(CRMReference left, object right)
        {
            return left.Equals(right) == false;
        }

        public override bool Equals(object right)
        {
            if (right == null) return false;
            if (right.GetType() == typeof(CRMReference))
            {
                var rightRef = (CRMReference)(right);
                return this.Equals(rightRef);
            }
            else if (right.GetType() == typeof(EntityReference))
            {
                var rightRef = (CRMReference)(EntityReference)(right);
                return rightRef.Equals(this);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (int)((long)this.Id.Id.GetHashCode() + (long)this.LogicalName.GetHashCode());
        }
        #endregion
        #region Methods
        public bool Exists()
        {
            return this != null && this.Id.Id != Guid.Empty && !string.IsNullOrWhiteSpace(this.LogicalName);
        }
        #endregion
    }
}
