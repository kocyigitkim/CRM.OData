using System;
using System.Collections.Generic;
using System.Reflection;

namespace Dynamics365.OData.Layout
{
    public class CRMEntityInformation
    {
        internal CRMEntityAttribute Entity;
        internal Dictionary<string, Tuple<CRMFieldAttribute, MemberInfo>> Fields;
        internal Dictionary<string, Tuple<CRMRelationshipAttribute, MemberInfo>> Relationships;
        internal string StateCodeFieldName, StatusCodeFieldName;
        internal List<CRMEntityLink> Links;
        internal bool IsDisabledEntityNameConvertion;
    }
}