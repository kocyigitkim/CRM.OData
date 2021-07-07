using CRM.OData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRM.OData
{
    public static class FetchXMLExtensions
    {
        public static void AddFilter(this FetchLinkEntity filter, FetchFilter subfilter)
        {
            if (filter.Items == null) filter.Items = new object[0];
            List<object> _items = new List<object>(filter.Items);
            _items.Add(subfilter);
            filter.Items = _items.ToArray();
        }
        public static void AddCondition(this FetchLinkEntity filter, FetchCondition condition)
        {
            if (filter.Items == null) filter.Items = new object[0];
            List<object> _items = new List<object>(filter.Items);
            _items.Add(condition);
            filter.Items = _items.ToArray();
        }
        public static void AddFilter(this FetchEntity filter, FetchFilter subfilter)
        {
            if (filter.Items == null) filter.Items = new object[0];
            List<object> _items = new List<object>(filter.Items);
            _items.Add(subfilter);
            filter.Items = _items.ToArray();
        }
        public static void AddCondition(this FetchEntity filter, FetchCondition condition)
        {
            if (filter.Items == null) filter.Items = new object[0];
            List<object> _items = new List<object>(filter.Items);
            _items.Add(condition);
            filter.Items = _items.ToArray();
        }
        public static void AddFilter(this FetchExpression filter, FetchFilter subfilter)
        {
            if (filter.Items == null) filter.Items = new object[0];
            List<object> _items = new List<object>(filter.Items);
            _items.Add(subfilter);
            filter.Items = _items.ToArray();
        }
        public static void AddCondition(this FetchExpression filter, FetchCondition condition)
        {
            if (filter.Items == null) filter.Items = new object[0];
            List<object> _items = new List<object>(filter.Items);
            _items.Add(condition);
            filter.Items = _items.ToArray();
        }
        public static void AddFilter(this FetchFilter filter, FetchFilter subfilter)
        {
            if (filter.Items == null) filter.Items = new object[0];
            List<object> _items = new List<object>(filter.Items);
            _items.Add(subfilter);
            filter.Items = _items.ToArray();
        }
        public static void AddCondition(this FetchFilter filter, FetchCondition condition)
        {
            if (filter.Items == null) filter.Items = new object[0];
            List<object> _items = new List<object>(filter.Items);
            _items.Add(condition);
            filter.Items = _items.ToArray();
        }
        public static void AddFilter(this FetchWrapper filter, FetchFilter subfilter)
        {
            filter.Items.Add(subfilter);
        }
        public static void AddCondition(this FetchWrapper filter, FetchCondition condition)
        {
            filter.Items.Add(condition);
        }
    }
}
