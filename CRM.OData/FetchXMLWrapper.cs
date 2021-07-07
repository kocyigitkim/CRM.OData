using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Xml.Serialization;
using System.IO;
using System.Runtime.Serialization;

namespace CRM.OData
{
    [System.Serializable()]
    public class FetchWrapper
    {
        private FetchExpression fetchExpression = new FetchExpression();
        private FetchEntity fetchEntity = new FetchEntity();
        private List<object> fetchEntityItems = new List<object>();
        public IList<Object> Items => fetchEntityItems;
        /// <summary>
        /// Initializes a new instance of the <see cref="FetchWrapper"/> class.
        /// </summary>
        /// <param name="query">The query.</param>
        public FetchWrapper(FetchExpression fetch)
        {
            this.fetchExpression = fetch;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="FetchWrapper"/> class.
        /// </summary>
        /// <param name="entityName">Name of the entity.</param>
        public FetchWrapper(string entityName)
        {
            fetchEntity.name = entityName;
            fetchExpression.Items = new FetchEntity[] { fetchEntity };
            fetchExpression.mapping = FetchMapping.logical;
            this.AddFetchEntityItem(new AllAttributes());
        }
        /// <summary>
        /// Adds an item to the entity node.
        /// </summary>
        /// <param name="item">The item to add</param>
        protected void AddFetchEntityItem(object item)
        {
            this.fetchEntityItems.Add(item);
            fetchEntity.Items = this.fetchEntityItems.ToArray();
        }

        protected void RemoveFetchEntityItem(Type typeToRemove)
        {
            for (int counter = fetchEntityItems.Count - 1; counter >= 0; counter--)
            {
                if (fetchEntityItems[counter].GetType() == typeToRemove)
                {
                    fetchEntityItems.RemoveAt(counter);
                }
            }
            this.fetchEntity.Items = fetchEntityItems.ToArray();
        }

        /// <summary>
        /// Gets the query expression.
        /// </summary>
        /// <value>The query expression.</value>
        public FetchExpression FetchExpression
        {
            get
            {
                return fetchExpression;
            }
        }

        public void AddColumn(string columnName, Aggregate aggregate, string alias)
        {
            this.FetchExpression.aggregate = true;
            this.RemoveFetchEntityItem(typeof(AllAttributes));
            FetchAttribute att = new FetchAttribute();
            att.name = columnName;
            att.aggregate = aggregate;
            att.alias = alias;
            this.RemoveColumn(columnName);
            this.AddFetchEntityItem(att);
        }
        /// <summary>
        /// Adds the column.
        /// </summary>
        /// <param name="columnName">Name of the column.</param>
        public void AddColumn(string columnName)
        {
            this.RemoveFetchEntityItem(typeof(AllAttributes));
            FetchAttribute att = new FetchAttribute();
            att.name = columnName;
            this.RemoveColumn(columnName);
            this.AddFetchEntityItem(att);
        }

        /// <summary>
        /// Removes the column.
        /// </summary>
        /// <param name="columnName">Name of the column.</param>
        public void RemoveColumn(string columnName)
        {
            for (int i = this.fetchEntityItems.Count - 1; i >= 0; i--)
            {
                if (this.fetchEntityItems[i] is FetchAttribute && ((FetchAttribute)this.fetchEntityItems[i]).name == columnName)
                {
                    this.fetchEntityItems.RemoveAt(i);
                }
            }

            this.fetchEntity.Items = this.fetchEntityItems.ToArray();
        }


        /// <summary>
        /// Clears the columns.
        /// </summary>
        public void ClearColumns()
        {
            this.RemoveFetchEntityItem(typeof(AllAttributes));
            this.RemoveFetchEntityItem(typeof(FetchAttribute));
        }
        /// <summary>
        /// true if there are no shared locks are issued against the data that would prohibit other transactions from modifying the data in the records returned from the query; otherwise, false.
        /// </summary>
        public bool NoLock
        {
            get
            {
                return this.fetchExpression.nolock;
            }
            set
            {
                this.fetchExpression.nolock = value;
            }
        }
        /// <summary>
        /// Gets or sets the columns.
        /// </summary>
        /// <value>The columns.</value>
        public string[] Columns
        {
            get
            {
                ArrayList columns = new ArrayList();
                if (this.fetchEntityItems.Where(t => t.GetType() == typeof(AllAttributes)).Count() > 0)
                {
                    //TODO: Return all of the attributes from the metadata
                }
                else
                {
                    foreach (object item in this.fetchEntityItems)
                    {
                        FetchAttribute att = item as FetchAttribute;
                        if (att != null)
                        {
                            columns.Add(att.name);
                        }
                    }
                }
                return (string[])columns.ToArray(typeof(string));
            }

            set
            {
                foreach (string col in value)
                {
                    this.AddColumn(col);
                }
            }
        }
        /// <summary>
        /// Converts to fetch XML.
        /// </summary>
        /// <returns></returns>
        public string ConvertToFetchXml()
        {
            return this.fetchExpression.Serialize();
        }

        /// <summary>
        /// Appends the order.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="orderType">Type of the order.</param>
        public void AppendOrder(string attributeName, bool descending)
        {
            FetchOrder order = new FetchOrder();
            order.attribute = attributeName;
            order.descending = descending;
            this.AddFetchEntityItem(order);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        public void AppendFilter(string attributeName, Guid attributeValue)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq), FetchFilterType.and);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        public void AppendFilter(string attributeName, string attributeValue)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq), FetchFilterType.and);
        }
        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        public void AppendFilter(string attributeName, int attributeValue)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq), FetchFilterType.and);
        }
        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        public void AppendFilter(string attributeName, double attributeValue)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq), FetchFilterType.and);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        public void AppendFilter(string attributeName, float attributeValue)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq), FetchFilterType.and);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        public void AppendFilter(string attributeName, decimal attributeValue)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq), FetchFilterType.and);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">if set to <c>true</c> [attribute value].</param>
        public void AppendFilter(string attributeName, bool attributeValue)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq), FetchFilterType.and);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="logicalOperator">The logical operator.</param>
        public void AppendFilter(string attributeName, Guid attributeValue, FetchFilterType logicalOperator)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq), logicalOperator);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="logicalOperator">The logical operator.</param>
        public void AppendFilter(string attributeName, string attributeValue, FetchFilterType logicalOperator)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq), logicalOperator);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="logicalOperator">The logical operator.</param>
        public void AppendFilter(string attributeName, int attributeValue, FetchFilterType logicalOperator)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq), logicalOperator);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="logicalOperator">The logical operator.</param>
        public void AppendFilter(string attributeName, double attributeValue, FetchFilterType logicalOperator)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq), logicalOperator);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="logicalOperator">The logical operator.</param>
        public void AppendFilter(string attributeName, float attributeValue, FetchFilterType logicalOperator)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq), logicalOperator);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="logicalOperator">The logical operator.</param>
        public void AppendFilter(string attributeName, decimal attributeValue, FetchFilterType logicalOperator)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq), logicalOperator);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">if set to <c>true</c> [attribute value].</param>
        /// <param name="logicalOperator">The logical operator.</param>
        public void AppendFilter(string attributeName, bool attributeValue, FetchFilterType logicalOperator)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq), logicalOperator);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="conditionOperator">The condition operator.</param>
        /// <param name="logicalOperator">The logical operator.</param>
        public void AppendFilter(string attributeName, Guid attributeValue, FetchOperator fetchOperator, FetchFilterType logicalOperator)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, fetchOperator), logicalOperator);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="conditionOperator">The condition operator.</param>
        /// <param name="logicalOperator">The logical operator.</param>
        public void AppendFilter(string attributeName, string attributeValue, FetchOperator fetchOperator, FetchFilterType logicalOperator)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, fetchOperator), logicalOperator);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="conditionOperator">The condition operator.</param>
        /// <param name="logicalOperator">The logical operator.</param>
        public void AppendFilter(string attributeName, int attributeValue, FetchOperator fetchOperator, FetchFilterType logicalOperator)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, fetchOperator), logicalOperator);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="conditionOperator">The condition operator.</param>
        /// <param name="logicalOperator">The logical operator.</param>
        public void AppendFilter(string attributeName, double attributeValue, FetchOperator fetchOperator, FetchFilterType logicalOperator)
        {
            this.InternalAppendFilter(attributeName, attributeValue.ToString().Replace(",","."), FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, fetchOperator), logicalOperator);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="conditionOperator">The condition operator.</param>
        /// <param name="logicalOperator">The logical operator.</param>
        public void AppendFilter(string attributeName, float attributeValue, FetchOperator fetchOperator, FetchFilterType logicalOperator)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, fetchOperator), logicalOperator);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="conditionOperator">The condition operator.</param>
        /// <param name="logicalOperator">The logical operator.</param>
        public void AppendFilter(string attributeName, decimal attributeValue, FetchOperator fetchOperator, FetchFilterType logicalOperator)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, fetchOperator), logicalOperator);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">if set to <c>true</c> [attribute value].</param>
        /// <param name="conditionOperator">The condition operator.</param>
        /// <param name="logicalOperator">The logical operator.</param>
        public void AppendFilter(string attributeName, bool attributeValue, FetchOperator fetchOperator, FetchFilterType logicalOperator)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, fetchOperator), logicalOperator);
        }

        /// <summary>
        /// Internals the append filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="conditionExpression">The condition expression.</param>
        /// <param name="logicalOperator">The logical operator.</param>
        protected void InternalAppendFilter(string attributeName, object attributeValue, FetchCondition conditionExpression, FetchFilterType logicalOperator)
        {
            FetchFilter filter = new FetchFilter();
            filter.type = logicalOperator;
            List<object> items = new List<object>();
            items.Add(conditionExpression);
            items.AddRange(fetchEntity.Items.Where(f => f.GetType() == typeof(FetchFilter)).ToList());
            filter.Items = items.ToArray();
            this.RemoveFetchEntityItem(typeof(FetchFilter));
            this.AddFetchEntityItem(filter);
        }



        /// <summary>
        /// Appends the link.
        /// </summary>
        /// <param name="link">The link.</param>
        public void AppendLink(FetchLinkWrapper link)
        {
            this.AddFetchEntityItem(link.LinkEntity);
        }


        #region Public Methods


        #endregion
    }

    [System.Serializable()]
    public class FetchLinkWrapper
    {
        private FetchLinkEntity linkEntity;
        private List<object> linkEntityItems;

        /// <summary>
        /// Initializes a new instance of the <see cref="FetchLinkWrapper"/> class.
        /// </summary>
        /// <param name="fromEntityName">Name of from entity.</param>
        /// <param name="fromAttributeName">Name of from attribute.</param>
        /// <param name="toEntityName">Name of to entity.</param>
        /// <param name="toAttributeName">Name of to attribute.</param>
        public FetchLinkWrapper(string fromAttributeName, string toEntityName, string toAttributeName, LinkType linkType = LinkType.inner)
        {
            linkEntity = new FetchLinkEntity();
            linkEntityItems = new List<object>();
            linkEntity.name = toEntityName;
            linkEntity.from = fromAttributeName;
            linkEntity.to = toAttributeName;
            linkEntity.linktype = linkType.ToString();
        }


        public void AddColumn(string columnName, Aggregate aggregate, string alias)
        {
            this.RemoveLinkEntityItem(typeof(AllAttributes));
            FetchAttribute att = new FetchAttribute();
            att.name = columnName;
            att.aggregate = aggregate;
            att.alias = alias;
            this.RemoveColumn(columnName);
            this.AddLinkEntityItem(att);
        }
        /// <summary>
        /// Adds the column.
        /// </summary>
        /// <param name="columnName">Name of the column.</param>
        public void AddColumn(string columnName)
        {
            this.RemoveLinkEntityItem(typeof(AllAttributes));
            FetchAttribute att = new FetchAttribute();
            att.name = columnName;
            this.RemoveColumn(columnName);
            this.AddLinkEntityItem(att);
        }

        /// <summary>
        /// Removes the column.
        /// </summary>
        /// <param name="columnName">Name of the column.</param>
        public void RemoveColumn(string columnName)
        {
            for (int i = this.linkEntityItems.Count - 1; i >= 0; i--)
            {
                if (this.linkEntityItems[i] is FetchAttribute && ((FetchAttribute)this.linkEntityItems[i]).name == columnName)
                {
                    this.linkEntityItems.RemoveAt(i);
                }
            }

            this.LinkEntity.Items = this.linkEntityItems.ToArray();
        }


        /// <summary>
        /// Clears the columns.
        /// </summary>
        public void ClearColumns()
        {
            this.RemoveLinkEntityItem(typeof(AllAttributes));
            this.RemoveLinkEntityItem(typeof(FetchAttribute));
        }

        /// <summary>
        /// Gets or sets the columns.
        /// </summary>
        /// <value>The columns.</value>
        public string[] Columns
        {
            get
            {
                ArrayList columns = new ArrayList();
                if (this.linkEntityItems.Where(t => t.GetType() == typeof(AllAttributes)).Count() > 0)
                {
                    //TODO: Return all of the attributes from the metadata
                }
                else
                {
                    foreach (object item in this.linkEntityItems)
                    {
                        FetchAttribute att = item as FetchAttribute;
                        if (att != null)
                        {
                            columns.Add(att.name);
                        }
                    }
                }
                return (string[])columns.ToArray(typeof(string));
            }

            set
            {
                foreach (string col in value)
                {
                    this.AddColumn(col);
                }
            }
        }

        public string EntityAlias
        {
            get
            {
                return this.LinkEntity.alias;
            }
            set
            {
                this.LinkEntity.alias = value;
            }
        }
        /// <summary>
        /// Gets the link entity.
        /// </summary>
        /// <value>The link entity.</value>
        public FetchLinkEntity LinkEntity
        {
            get
            {
                return linkEntity;
            }
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        public void AppendFilter(string attributeName, Guid attributeValue)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq), FetchFilterType.and);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        public void AppendFilter(string attributeName, string attributeValue)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq), FetchFilterType.and);
        }
        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        public void AppendFilter(string attributeName, int attributeValue)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq), FetchFilterType.and);
        }
        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        public void AppendFilter(string attributeName, double attributeValue)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq), FetchFilterType.and);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        public void AppendFilter(string attributeName, float attributeValue)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq), FetchFilterType.and);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        public void AppendFilter(string attributeName, decimal attributeValue)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq), FetchFilterType.and);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">if set to <c>true</c> [attribute value].</param>
        public void AppendFilter(string attributeName, bool attributeValue)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq), FetchFilterType.and);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="logicalOperator">The logical operator.</param>
        public void AppendFilter(string attributeName, Guid attributeValue, FetchFilterType logicalOperator)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq), logicalOperator);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="logicalOperator">The logical operator.</param>
        public void AppendFilter(string attributeName, string attributeValue, FetchFilterType logicalOperator)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq), logicalOperator);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="logicalOperator">The logical operator.</param>
        public void AppendFilter(string attributeName, int attributeValue, FetchFilterType logicalOperator)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq), logicalOperator);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="logicalOperator">The logical operator.</param>
        public void AppendFilter(string attributeName, double attributeValue, FetchFilterType logicalOperator)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq), logicalOperator);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="logicalOperator">The logical operator.</param>
        public void AppendFilter(string attributeName, float attributeValue, FetchFilterType logicalOperator)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq), logicalOperator);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="logicalOperator">The logical operator.</param>
        public void AppendFilter(string attributeName, decimal attributeValue, FetchFilterType logicalOperator)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq), logicalOperator);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">if set to <c>true</c> [attribute value].</param>
        /// <param name="logicalOperator">The logical operator.</param>
        public void AppendFilter(string attributeName, bool attributeValue, FetchFilterType logicalOperator)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq), logicalOperator);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="conditionOperator">The condition operator.</param>
        /// <param name="logicalOperator">The logical operator.</param>
        public void AppendFilter(string attributeName, Guid attributeValue, FetchOperator fetchOperator, FetchFilterType logicalOperator)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, fetchOperator), logicalOperator);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="conditionOperator">The condition operator.</param>
        /// <param name="logicalOperator">The logical operator.</param>
        public void AppendFilter(string attributeName, string attributeValue, FetchOperator fetchOperator, FetchFilterType logicalOperator)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, fetchOperator), logicalOperator);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="conditionOperator">The condition operator.</param>
        /// <param name="logicalOperator">The logical operator.</param>
        public void AppendFilter(string attributeName, int attributeValue, FetchOperator fetchOperator, FetchFilterType logicalOperator)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, fetchOperator), logicalOperator);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="conditionOperator">The condition operator.</param>
        /// <param name="logicalOperator">The logical operator.</param>
        public void AppendFilter(string attributeName, double attributeValue, FetchOperator fetchOperator, FetchFilterType logicalOperator)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, fetchOperator), logicalOperator);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="conditionOperator">The condition operator.</param>
        /// <param name="logicalOperator">The logical operator.</param>
        public void AppendFilter(string attributeName, float attributeValue, FetchOperator fetchOperator, FetchFilterType logicalOperator)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, fetchOperator), logicalOperator);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="conditionOperator">The condition operator.</param>
        /// <param name="logicalOperator">The logical operator.</param>
        public void AppendFilter(string attributeName, decimal attributeValue, FetchOperator fetchOperator, FetchFilterType logicalOperator)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, fetchOperator), logicalOperator);
        }

        /// <summary>
        /// Appends the filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">if set to <c>true</c> [attribute value].</param>
        /// <param name="conditionOperator">The condition operator.</param>
        /// <param name="logicalOperator">The logical operator.</param>
        public void AppendFilter(string attributeName, bool attributeValue, FetchOperator fetchOperator, FetchFilterType logicalOperator)
        {
            this.InternalAppendFilter(attributeName, attributeValue, FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, fetchOperator), logicalOperator);
        }

        /// <summary>
        /// Internals the append filter.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="conditionExpression">The condition expression.</param>
        /// <param name="logicalOperator">The logical operator.</param>
        protected void InternalAppendFilter(string attributeName, object attributeValue, FetchCondition conditionExpression, FetchFilterType logicalOperator)
        {
            FetchFilter filter = new FetchFilter();
            filter.type = logicalOperator;
            List<object> items = new List<object>();
            items.Add(conditionExpression);
            items.AddRange(linkEntity.Items.Where(f => f.GetType() == typeof(FetchFilter)).ToList());
            filter.Items = items.ToArray();
            this.RemoveLinkEntityItem(typeof(FetchFilter));
            this.AddLinkEntityItem(filter);
        }

        /// <summary>
        /// Appends the link.
        /// </summary>
        /// <param name="link">The link.</param>
        public void AppendLink(FetchLinkWrapper link)
        {
            this.AddLinkEntityItem(link.LinkEntity);
        }

        /// <summary>
        /// Adds an item to the entity node.
        /// </summary>
        /// <param name="item">The item to add</param>
        protected void AddLinkEntityItem(object item)
        {
            this.linkEntityItems.Add(item);
            this.LinkEntity.Items = this.linkEntityItems.ToArray();
        }

        protected void RemoveLinkEntityItem(Type typeToRemove)
        {
            for (int counter = linkEntityItems.Count - 1; counter >= 0; counter--)
            {
                if (linkEntityItems[counter].GetType() == typeToRemove)
                {
                    linkEntityItems.RemoveAt(counter);
                }
            }
            this.LinkEntity.Items = linkEntityItems.ToArray();
        }

    }

    [System.Serializable()]
    public class FetchWrapperHelper
    {

        /// <summary>
        /// Creates the condition expression.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <returns></returns>
        public static FetchCondition CreateFetchCondition(string attributeName, Guid attributeValue)
        {
            return FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq);
        }

        /// <summary>
        /// Creates the condition expression.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <returns></returns>
        public static FetchCondition CreateFetchCondition(string attributeName, string attributeValue)
        {
            return FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq);
        }

        /// <summary>
        /// Creates the condition expression.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <returns></returns>
        public static FetchCondition CreateFetchCondition(string attributeName, int attributeValue)
        {
            return FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq);
        }

        /// <summary>
        /// Creates the condition expression.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <returns></returns>
        public static FetchCondition CreateFetchCondition(string attributeName, float attributeValue)
        {
            return FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq);
        }

        /// <summary>
        /// Creates the condition expression.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <returns></returns>
        public static FetchCondition CreateFetchCondition(string attributeName, double attributeValue)
        {
            return FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq);
        }

        /// <summary>
        /// Creates the condition expression.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <returns></returns>
        public static FetchCondition CreateFetchCondition(string attributeName, decimal attributeValue)
        {
            return FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq);
        }

        /// <summary>
        /// Creates the condition expression.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">if set to <c>true</c> [attribute value].</param>
        /// <returns></returns>
        public static FetchCondition CreateFetchCondition(string attributeName, bool attributeValue)
        {
            return FetchWrapperHelper.CreateFetchCondition(attributeName, attributeValue, FetchOperator.eq);
        }

        /// <summary>
        /// Internals the create condition expression.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <returns></returns>
        protected static FetchCondition InternalCreateFetchCondition(string attributeName, object attributeValue)
        {
            return FetchWrapperHelper.InternalCreateFetchCondition(attributeName, attributeValue, FetchOperator.eq);
        }

        /// <summary>
        /// Creates the condition expression.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="conditionOperator">The condition operator.</param>
        /// <returns></returns>
        public static FetchCondition CreateFetchCondition(string attributeName, Guid attributeValue, FetchOperator fetchOperator)
        {
            return InternalCreateFetchCondition(attributeName, attributeValue, fetchOperator);
        }

        /// <summary>
        /// Creates the condition expression.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="conditionOperator">The condition operator.</param>
        /// <returns></returns>
        public static FetchCondition CreateFetchCondition(string attributeName, string attributeValue, FetchOperator fetchOperator)
        {
            return InternalCreateFetchCondition(attributeName, attributeValue, fetchOperator);
        }

        /// <summary>
        /// Creates the condition expression.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="conditionOperator">The condition operator.</param>
        /// <returns></returns>
        public static FetchCondition CreateFetchCondition(string attributeName, int attributeValue, FetchOperator fetchOperator)
        {
            return InternalCreateFetchCondition(attributeName, attributeValue, fetchOperator);
        }

        /// <summary>
        /// Creates the condition expression.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="conditionOperator">The condition operator.</param>
        /// <returns></returns>
        public static FetchCondition CreateFetchCondition(string attributeName, double attributeValue, FetchOperator fetchOperator)
        {
            return InternalCreateFetchCondition(attributeName, attributeValue, fetchOperator);
        }

        /// <summary>
        /// Creates the condition expression.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="conditionOperator">The condition operator.</param>
        /// <returns></returns>
        public static FetchCondition CreateFetchCondition(string attributeName, float attributeValue, FetchOperator fetchOperator)
        {
            return InternalCreateFetchCondition(attributeName, attributeValue, fetchOperator);
        }

        /// <summary>
        /// Creates the condition expression.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="conditionOperator">The condition operator.</param>
        /// <returns></returns>
        public static FetchCondition CreateFetchCondition(string attributeName, decimal attributeValue, FetchOperator fetchOperator)
        {
            return InternalCreateFetchCondition(attributeName, attributeValue, fetchOperator);
        }

        /// <summary>
        /// Creates the condition expression.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">if set to <c>true</c> [attribute value].</param>
        /// <param name="conditionOperator">The condition operator.</param>
        /// <returns></returns>
        public static FetchCondition CreateFetchCondition(string attributeName, bool attributeValue, FetchOperator fetchOperator)
        {
            return InternalCreateFetchCondition(attributeName, attributeValue, fetchOperator);
        }

        /// <summary>
        /// Internals the create condition expression.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="conditionOperator">The condition operator.</param>
        /// <returns></returns>
        protected static FetchCondition InternalCreateFetchCondition(string attributeName, object attributeValue, FetchOperator fetchOperator)
        {
            FetchCondition conditionExpression = new FetchCondition();
            conditionExpression.attribute = attributeName;
            conditionExpression.@operator = fetchOperator;
            if (attributeValue != null)
            {
                conditionExpression.value = attributeValue.ToString();
            }

            return conditionExpression;
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class FetchCondition
    {

        private conditionValue[] itemsField;

        private string columnField;

        private string attributeField;

        private string entitynameField;

        private FetchOperator operatorField;

        private string valueField;

        private Aggregate aggregateField;

        private bool aggregateFieldSpecified;

        private RowAggregateType rowaggregateField;

        private bool rowaggregateFieldSpecified;

        private string aliasField;

        private string uinameField;

        private string uitypeField;

        private TrueFalse uihiddenField;

        private bool uihiddenFieldSpecified;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("value")]
        public conditionValue[] Items
        {
            get
            {
                return this.itemsField;
            }
            set
            {
                this.itemsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string column
        {
            get
            {
                return this.columnField;
            }
            set
            {
                this.columnField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string attribute
        {
            get
            {
                return this.attributeField;
            }
            set
            {
                this.attributeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string entityname
        {
            get
            {
                return this.entitynameField;
            }
            set
            {
                this.entitynameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public FetchOperator @operator
        {
            get
            {
                return this.operatorField;
            }
            set
            {
                this.operatorField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public Aggregate aggregate
        {
            get
            {
                return this.aggregateField;
            }
            set
            {
                this.aggregateField = value;
                this.aggregateFieldSpecified = true;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool aggregateSpecified
        {
            get
            {
                return this.aggregateFieldSpecified;
            }
            set
            {
                this.aggregateFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public RowAggregateType rowaggregate
        {
            get
            {
                return this.rowaggregateField;
            }
            set
            {
                this.rowaggregateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool rowaggregateSpecified
        {
            get
            {
                return this.rowaggregateFieldSpecified;
            }
            set
            {
                this.rowaggregateFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string alias
        {
            get
            {
                return this.aliasField;
            }
            set
            {
                this.aliasField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string uiname
        {
            get
            {
                return this.uinameField;
            }
            set
            {
                this.uinameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string uitype
        {
            get
            {
                return this.uitypeField;
            }
            set
            {
                this.uitypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public TrueFalse uihidden
        {
            get
            {
                return this.uihiddenField;
            }
            set
            {
                this.uihiddenField = value;
                this.uihiddenFieldSpecified = true;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool uihiddenSpecified
        {
            get
            {
                return this.uihiddenFieldSpecified;
            }
            set
            {
                this.uihiddenFieldSpecified = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class conditionValue
    {

        private string uinameField;

        private string uitypeField;

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string uiname
        {
            get
            {
                return this.uinameField;
            }
            set
            {
                this.uinameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string uitype
        {
            get
            {
                return this.uitypeField;
            }
            set
            {
                this.uitypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class FieldXmlFieldUI
    {

        private string idField;

        private string descriptionField;

        private string languagecodeField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string description
        {
            get
            {
                return this.descriptionField;
            }
            set
            {
                this.descriptionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "positiveInteger")]
        public string languagecode
        {
            get
            {
                return this.languagecodeField;
            }
            set
            {
                this.languagecodeField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class SerializedTrueFalse
    {

        private string nameField;

        private TrueFalse valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public TrueFalse Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    public enum TrueFalse
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("0")]
        Item0,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("1")]
        Item1,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class SerializedInteger
    {

        private string formattedvalueField;

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string formattedvalue
        {
            get
            {
                return this.formattedvalueField;
            }
            set
            {
                this.formattedvalueField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute(DataType = "nonNegativeInteger")]
        public string Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class FetchLinkEntity
    {

        private object[] itemsField;

        private string nameField;

        private string toField;

        private string fromField;

        private string aliasField;

        private string linktypeField;

        private bool visibleField;

        private bool visibleFieldSpecified;

        private bool intersectField;

        private bool intersectFieldSpecified;

        private bool enableprefilteringField;

        private bool enableprefilteringFieldSpecified;

        private string prefilterparameternameField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("all-attributes", typeof(AllAttributes))]
        [System.Xml.Serialization.XmlElementAttribute("attribute", typeof(FetchAttribute))]
        [System.Xml.Serialization.XmlElementAttribute("filter", typeof(FetchFilter))]
        [System.Xml.Serialization.XmlElementAttribute("link-entity", typeof(FetchLinkEntity))]
        [System.Xml.Serialization.XmlElementAttribute("order", typeof(FetchOrder))]
        public object[] Items
        {
            get
            {
                return this.itemsField;
            }
            set
            {
                this.itemsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string to
        {
            get
            {
                return this.toField;
            }
            set
            {
                this.toField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string from
        {
            get
            {
                return this.fromField;
            }
            set
            {
                this.fromField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string alias
        {
            get
            {
                return this.aliasField;
            }
            set
            {
                this.aliasField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("link-type")]
        public string linktype
        {
            get
            {
                return this.linktypeField;
            }
            set
            {
                this.linktypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool visible
        {
            get
            {
                return this.visibleField;
            }
            set
            {
                this.visibleField = value;
                this.visibleFieldSpecified = true;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool visibleSpecified
        {
            get
            {
                return this.visibleFieldSpecified;
            }
            set
            {
                this.visibleFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool intersect
        {
            get
            {
                return this.intersectField;
            }
            set
            {
                this.intersectField = value;
                this.intersectFieldSpecified = true;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool intersectSpecified
        {
            get
            {
                return this.intersectFieldSpecified;
            }
            set
            {
                this.intersectFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool enableprefiltering
        {
            get
            {
                return this.enableprefilteringField;
            }
            set
            {
                this.enableprefilteringField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool enableprefilteringSpecified
        {
            get
            {
                return this.enableprefilteringFieldSpecified;
            }
            set
            {
                this.enableprefilteringFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string prefilterparametername
        {
            get
            {
                return this.prefilterparameternameField;
            }
            set
            {
                this.prefilterparameternameField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute("all-attributes", Namespace = "", IsNullable = false)]
    public partial class AllAttributes
    {
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class FetchAttribute
    {

        private string nameField;

        private build buildField;

        private bool buildFieldSpecified;

        private string addedbyField;

        private string aliasField;

        private Aggregate aggregateField;

        private bool aggregateFieldSpecified;

        private FetchBool groupbyField;

        private bool groupbyFieldSpecified;

        private DateGrouping dategroupingField;

        private bool dategroupingFieldSpecified;

        private FetchBool usertimezoneField;

        private bool usertimezoneFieldSpecified;

        private FetchBool distinctField;

        private bool distinctFieldSpecified;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public build build
        {
            get
            {
                return this.buildField;
            }
            set
            {
                this.buildField = value;
                this.buildFieldSpecified = true;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool buildSpecified
        {
            get
            {
                return this.buildFieldSpecified;
            }
            set
            {
                this.buildFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string addedby
        {
            get
            {
                return this.addedbyField;
            }
            set
            {
                this.addedbyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string alias
        {
            get
            {
                return this.aliasField;
            }
            set
            {
                this.aliasField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public Aggregate aggregate
        {
            get
            {
                return this.aggregateField;
            }
            set
            {
                this.aggregateField = value;
                this.aggregateFieldSpecified = true;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool aggregateSpecified
        {
            get
            {
                return this.aggregateFieldSpecified;
            }
            set
            {
                this.aggregateFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public FetchBool groupby
        {
            get
            {
                return this.groupbyField;
            }
            set
            {
                this.groupbyField = value;
                this.groupbyFieldSpecified = true;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool groupbySpecified
        {
            get
            {
                return this.groupbyFieldSpecified;
            }
            set
            {
                this.groupbyFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public DateGrouping dategrouping
        {
            get
            {
                return this.dategroupingField;
            }
            set
            {
                this.dategroupingField = value;
                this.dategroupingFieldSpecified = true;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool dategroupingSpecified
        {
            get
            {
                return this.dategroupingFieldSpecified;
            }
            set
            {
                this.dategroupingFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public FetchBool usertimezone
        {
            get
            {
                return this.usertimezoneField;
            }
            set
            {
                this.usertimezoneField = value;
                this.usertimezoneFieldSpecified = true;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool usertimezoneSpecified
        {
            get
            {
                return this.usertimezoneFieldSpecified;
            }
            set
            {
                this.usertimezoneFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public FetchBool distinct
        {
            get
            {
                return this.distinctField;
            }
            set
            {
                this.distinctField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool distinctSpecified
        {
            get
            {
                return this.distinctFieldSpecified;
            }
            set
            {
                this.distinctFieldSpecified = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    public enum build
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("1.504021")]
        Item1504021,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("1.003017")]
        Item1003017,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    public enum Aggregate
    {

        /// <remarks/>
        count,

        /// <remarks/>
        countcolumn,

        /// <remarks/>
        sum,

        /// <remarks/>
        avg,

        /// <remarks/>
        min,

        /// <remarks/>
        max,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    public enum FetchBool
    {

        /// <remarks/>
        @true,

        /// <remarks/>
        @false,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("1")]
        Item1,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("0")]
        Item0,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    public enum DateGrouping
    {

        /// <remarks/>
        day,

        /// <remarks/>
        week,

        /// <remarks/>
        month,

        /// <remarks/>
        quarter,

        /// <remarks/>
        year,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("fiscal-period")]
        fiscalperiod,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("fiscal-year")]
        fiscalyear,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class FetchFilter
    {

        private object[] itemsField;

        private FetchFilterType typeField;

        private bool isquickfindfieldsField;

        private bool isquickfindfieldsFieldSpecified;

        public FetchFilter()
        {
            this.typeField = FetchFilterType.and;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("condition", typeof(FetchCondition))]
        [System.Xml.Serialization.XmlElementAttribute("filter", typeof(FetchFilter))]
        public object[] Items
        {
            get
            {
                return this.itemsField;
            }
            set
            {
                this.itemsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValueAttribute(FetchFilterType.and)]
        public FetchFilterType type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool isquickfindfields
        {
            get
            {
                return this.isquickfindfieldsField;
            }
            set
            {
                this.isquickfindfieldsField = value;
                this.isquickfindfieldsFieldSpecified = true;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool isquickfindfieldsSpecified
        {
            get
            {
                return this.isquickfindfieldsFieldSpecified;
            }
            set
            {
                this.isquickfindfieldsFieldSpecified = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public enum FetchFilterType
    {

        /// <remarks/>
        and,

        /// <remarks/>
        or,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class FetchOrder
    {

        private object[] itemsField;

        private string attributeField;

        private string aliasField;

        private bool descendingField;

        public FetchOrder()
        {
            this.descendingField = false;
        }

        /// <remarks/>
        public object[] Items
        {
            get
            {
                return this.itemsField;
            }
            set
            {
                this.itemsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string attribute
        {
            get
            {
                return this.attributeField;
            }
            set
            {
                this.attributeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string alias
        {
            get
            {
                return this.aliasField;
            }
            set
            {
                this.aliasField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool descending
        {
            get
            {
                return this.descendingField;
            }
            set
            {
                this.descendingField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class FetchEntity
    {

        private object[] itemsField;

        private string nameField;

        private bool enableprefilteringField;

        private bool enableprefilteringFieldSpecified;

        private string prefilterparameternameField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("all-attributes", typeof(AllAttributes))]
        [System.Xml.Serialization.XmlElementAttribute("attribute", typeof(FetchAttribute))]
        [System.Xml.Serialization.XmlElementAttribute("filter", typeof(FetchFilter))]
        [System.Xml.Serialization.XmlElementAttribute("link-entity", typeof(FetchLinkEntity))]
        [System.Xml.Serialization.XmlElementAttribute("order", typeof(FetchOrder))]
        public object[] Items
        {
            get
            {
                return this.itemsField;
            }
            set
            {
                this.itemsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool enableprefiltering
        {
            get
            {
                return this.enableprefilteringField;
            }
            set
            {
                this.enableprefilteringField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool enableprefilteringSpecified
        {
            get
            {
                return this.enableprefilteringFieldSpecified;
            }
            set
            {
                this.enableprefilteringFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string prefilterparametername
        {
            get
            {
                return this.prefilterparameternameField;
            }
            set
            {
                this.prefilterparameternameField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    public enum FetchOperator
    {

        /// <remarks/>
        eq,

        /// <remarks/>
        neq,

        /// <remarks/>
        ne,

        /// <remarks/>
        gt,

        /// <remarks/>
        ge,

        /// <remarks/>
        le,

        /// <remarks/>
        lt,

        /// <remarks/>
        like,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("not-like")]
        notlike,

        /// <remarks/>
        @in,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("not-in")]
        notin,

        /// <remarks/>
        between,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("not-between")]
        notbetween,

        /// <remarks/>
        @null,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("not-null")]
        notnull,

        /// <remarks/>
        yesterday,

        /// <remarks/>
        today,

        /// <remarks/>
        tomorrow,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("last-seven-days")]
        lastsevendays,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("next-seven-days")]
        nextsevendays,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("last-week")]
        lastweek,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("this-week")]
        thisweek,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("next-week")]
        nextweek,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("last-month")]
        lastmonth,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("this-month")]
        thismonth,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("next-month")]
        nextmonth,

        /// <remarks/>
        on,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("on-or-before")]
        onorbefore,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("on-or-after")]
        onorafter,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("last-year")]
        lastyear,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("this-year")]
        thisyear,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("next-year")]
        nextyear,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("last-x-hours")]
        lastxhours,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("next-x-hours")]
        nextxhours,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("last-x-days")]
        lastxdays,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("next-x-days")]
        nextxdays,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("last-x-weeks")]
        lastxweeks,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("next-x-weeks")]
        nextxweeks,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("last-x-months")]
        lastxmonths,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("next-x-months")]
        nextxmonths,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("olderthan-x-months")]
        olderthanxmonths,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("olderthan-x-years")]
        olderthanxyears,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("olderthan-x-weeks")]
        olderthanxweeks,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("olderthan-x-days")]
        olderthanxdays,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("olderthan-x-hours")]
        olderthanxhours,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("olderthan-x-minutes")]
        olderthanxminutes,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("last-x-years")]
        lastxyears,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("next-x-years")]
        nextxyears,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("eq-userid")]
        equserid,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("ne-userid")]
        neuserid,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("eq-userteams")]
        equserteams,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("eq-useroruserteams")]
        equseroruserteams,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("eq-useroruserhierarchy")]
        equseroruserhierarchy,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("eq-useroruserhierarchyandteams")]
        equseroruserhierarchyandteams,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("eq-businessid")]
        eqbusinessid,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("ne-businessid")]
        nebusinessid,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("eq-userlanguage")]
        equserlanguage,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("this-fiscal-year")]
        thisfiscalyear,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("this-fiscal-period")]
        thisfiscalperiod,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("next-fiscal-year")]
        nextfiscalyear,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("next-fiscal-period")]
        nextfiscalperiod,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("last-fiscal-year")]
        lastfiscalyear,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("last-fiscal-period")]
        lastfiscalperiod,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("last-x-fiscal-years")]
        lastxfiscalyears,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("last-x-fiscal-periods")]
        lastxfiscalperiods,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("next-x-fiscal-years")]
        nextxfiscalyears,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("next-x-fiscal-periods")]
        nextxfiscalperiods,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("in-fiscal-year")]
        infiscalyear,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("in-fiscal-period")]
        infiscalperiod,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("in-fiscal-period-and-year")]
        infiscalperiodandyear,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("in-or-before-fiscal-period-and-year")]
        inorbeforefiscalperiodandyear,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("in-or-after-fiscal-period-and-year")]
        inorafterfiscalperiodandyear,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("begins-with")]
        beginswith,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("not-begin-with")]
        notbeginwith,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("ends-with")]
        endswith,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("not-end-with")]
        notendwith,

        /// <remarks/>
        under,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("eq-or-under")]
        eqorunder,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("not-under")]
        notunder,

        /// <remarks/>
        above,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("eq-or-above")]
        eqorabove,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("contain-values")]
        containvalues,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("not-contain-values")]
        notcontainvalues,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    public enum RowAggregateType
    {

        /// <remarks/>
        countchildren,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("fetch", Namespace = "", IsNullable = false)]
    [DataContract]
    public partial class FetchExpression
    {
        private object[] itemsField;

        private string versionField;

        private string countField;

        private string pageField;

        private string pagingcookieField;

        private string utcoffsetField;

        private bool aggregateField;

        private bool aggregateFieldSpecified;

        private bool distinctField;

        private bool distinctFieldSpecified;

        private string topField;

        private FetchMapping mappingField;

        private bool mappingFieldSpecified;

        private bool minactiverowversionField;

        private FetchOutputformat outputformatField;

        private bool outputformatFieldSpecified;

        private bool returntotalrecordcountField;

        private bool nolockField;

        public FetchExpression()
        {
            this.minactiverowversionField = false;
            this.returntotalrecordcountField = false;
            this.nolock = true;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("entity", typeof(FetchEntity))]
        [System.Xml.Serialization.XmlElementAttribute("order", typeof(FetchOrder))]
        public object[] Items
        {
            get
            {
                return this.itemsField;
            }
            set
            {
                this.itemsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string version
        {
            get
            {
                return this.versionField;
            }
            set
            {
                this.versionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "integer")]
        public string count
        {
            get
            {
                return this.countField;
            }
            set
            {
                this.countField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "integer")]
        public string page
        {
            get
            {
                return this.pageField;
            }
            set
            {
                this.pageField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("paging-cookie")]
        public string pagingcookie
        {
            get
            {
                return this.pagingcookieField;
            }
            set
            {
                this.pagingcookieField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("utc-offset")]
        public string utcoffset
        {
            get
            {
                return this.utcoffsetField;
            }
            set
            {
                this.utcoffsetField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool aggregate
        {
            get
            {
                return this.aggregateField;
            }
            set
            {
                this.aggregateField = value;
                this.aggregateSpecified = true;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool aggregateSpecified
        {
            get
            {
                return this.aggregateFieldSpecified;
            }
            set
            {
                this.aggregateFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool distinct
        {
            get
            {
                return this.distinctField;
            }
            set
            {
                this.distinctField = value;
                this.distinctFieldSpecified = true;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool distinctSpecified
        {
            get
            {
                return this.distinctFieldSpecified;
            }
            set
            {
                this.distinctFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "integer")]
        public string top
        {
            get
            {
                return this.topField;
            }
            set
            {
                this.topField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public FetchMapping mapping
        {
            get
            {
                return this.mappingField;
            }
            set
            {
                this.mappingField = value;
                this.mappingFieldSpecified = true;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool mappingSpecified
        {
            get
            {
                return this.mappingFieldSpecified;
            }
            set
            {
                this.mappingFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("min-active-row-version")]
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool minactiverowversion
        {
            get
            {
                return this.minactiverowversionField;
            }
            set
            {
                this.minactiverowversionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("output-format")]
        public FetchOutputformat outputformat
        {
            get
            {
                return this.outputformatField;
            }
            set
            {
                this.outputformatField = value;
                this.outputformatFieldSpecified = true;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool outputformatSpecified
        {
            get
            {
                return this.outputformatFieldSpecified;
            }
            set
            {
                this.outputformatFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool returntotalrecordcount
        {
            get
            {
                return this.returntotalrecordcountField;
            }
            set
            {
                this.returntotalrecordcountField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("no-lock")]
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool nolock
        {
            get
            {
                return this.nolockField;
            }
            set
            {
                this.nolockField = value;
            }
        }

        public static FetchExpression Deserialize(string xml)
        {
            // DeSerialize the XML into an Entity and return the Entity
            XmlSerializer serializer = new XmlSerializer(typeof(FetchExpression));
            // Declare a CRM Entity
            System.IO.StringReader reader = new System.IO.StringReader(xml);

            // Deserialize the Entity object
            return (FetchExpression)serializer.Deserialize(reader);
        }


        public string Serialize()
        {
            // Serialize the BusinessEntity into XML and return the XML as a string
            XmlSerializer serializer = new XmlSerializer(typeof(FetchExpression));
            StringWriter writer = new StringWriter();

            // Serialize
            serializer.Serialize(writer, this);
            string xmlString = writer.ToString();
            //
            //This is to remove the added formatting that the Serializer adds (i.e. tabs, newlines etc.)
            //
            System.Xml.XmlDocument document = new System.Xml.XmlDocument();
            document.LoadXml(xmlString);
            xmlString = document.OuterXml;
            return xmlString;
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public enum FetchMapping
    {

        /// <remarks/>
        @internal,

        /// <remarks/>
        logical,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public enum FetchOutputformat
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("xml-ado")]
        xmlado,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("xml-auto")]
        xmlauto,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("xml-elements")]
        xmlelements,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("xml-raw")]
        xmlraw,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("xml-platform")]
        xmlplatform,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class savedquery
    {

        private string nameField;

        private string savedqueryidField;

        private SerializedInteger returnedtypecodeField;

        private string descriptionField;

        private SerializedInteger querytypeField;

        private SerializedTrueFalse isCustomizableField;

        private SerializedTrueFalse canBeDeletedField;

        private string introducedVersionField;

        private SerializedTrueFalse isquickfindqueryField;

        private SerializedTrueFalse isuserdefinedField;

        private SerializedTrueFalse isdefaultField;

        private bool isprivateField;

        private bool isprivateFieldSpecified;

        private string queryapiField;

        private savedqueryFetchxml fetchxmlField;

        private savedqueryColumnsetxml columnsetxmlField;

        private savedqueryLayoutxml layoutxmlField;

        private string donotuseinLCIDField;

        private string useinLCIDField;

        /// <remarks/>
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        public string savedqueryid
        {
            get
            {
                return this.savedqueryidField;
            }
            set
            {
                this.savedqueryidField = value;
            }
        }

        /// <remarks/>
        public SerializedInteger returnedtypecode
        {
            get
            {
                return this.returnedtypecodeField;
            }
            set
            {
                this.returnedtypecodeField = value;
            }
        }

        /// <remarks/>
        public string description
        {
            get
            {
                return this.descriptionField;
            }
            set
            {
                this.descriptionField = value;
            }
        }

        /// <remarks/>
        public SerializedInteger querytype
        {
            get
            {
                return this.querytypeField;
            }
            set
            {
                this.querytypeField = value;
            }
        }

        /// <remarks/>
        public SerializedTrueFalse IsCustomizable
        {
            get
            {
                return this.isCustomizableField;
            }
            set
            {
                this.isCustomizableField = value;
            }
        }

        /// <remarks/>
        public SerializedTrueFalse CanBeDeleted
        {
            get
            {
                return this.canBeDeletedField;
            }
            set
            {
                this.canBeDeletedField = value;
            }
        }

        /// <remarks/>
        public string IntroducedVersion
        {
            get
            {
                return this.introducedVersionField;
            }
            set
            {
                this.introducedVersionField = value;
            }
        }

        /// <remarks/>
        public SerializedTrueFalse isquickfindquery
        {
            get
            {
                return this.isquickfindqueryField;
            }
            set
            {
                this.isquickfindqueryField = value;
            }
        }

        /// <remarks/>
        public SerializedTrueFalse isuserdefined
        {
            get
            {
                return this.isuserdefinedField;
            }
            set
            {
                this.isuserdefinedField = value;
            }
        }

        /// <remarks/>
        public SerializedTrueFalse isdefault
        {
            get
            {
                return this.isdefaultField;
            }
            set
            {
                this.isdefaultField = value;
            }
        }

        /// <remarks/>
        public bool isprivate
        {
            get
            {
                return this.isprivateField;
            }
            set
            {
                this.isprivateField = value;
                this.isprivateFieldSpecified = true;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool isprivateSpecified
        {
            get
            {
                return this.isprivateFieldSpecified;
            }
            set
            {
                this.isprivateFieldSpecified = value;
            }
        }

        /// <remarks/>
        public string queryapi
        {
            get
            {
                return this.queryapiField;
            }
            set
            {
                this.queryapiField = value;
            }
        }

        /// <remarks/>
        public savedqueryFetchxml fetchxml
        {
            get
            {
                return this.fetchxmlField;
            }
            set
            {
                this.fetchxmlField = value;
            }
        }

        /// <remarks/>
        public savedqueryColumnsetxml columnsetxml
        {
            get
            {
                return this.columnsetxmlField;
            }
            set
            {
                this.columnsetxmlField = value;
            }
        }

        /// <remarks/>
        public savedqueryLayoutxml layoutxml
        {
            get
            {
                return this.layoutxmlField;
            }
            set
            {
                this.layoutxmlField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string donotuseinLCID
        {
            get
            {
                return this.donotuseinLCIDField;
            }
            set
            {
                this.donotuseinLCIDField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string useinLCID
        {
            get
            {
                return this.useinLCIDField;
            }
            set
            {
                this.useinLCIDField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class savedqueryFetchxml
    {

        private FetchExpression fetchField;

        /// <remarks/>
        public FetchExpression fetch
        {
            get
            {
                return this.fetchField;
            }
            set
            {
                this.fetchField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class savedqueryColumnsetxml
    {

        private savedqueryColumnsetxmlColumnset columnsetField;

        /// <remarks/>
        public savedqueryColumnsetxmlColumnset columnset
        {
            get
            {
                return this.columnsetField;
            }
            set
            {
                this.columnsetField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class savedqueryColumnsetxmlColumnset
    {

        private object[] itemsField;

        private ItemsChoice[] itemsElementNameField;

        private string versionField;

        private bool distinctField;

        private bool distinctFieldSpecified;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("ascend", typeof(object))]
        [System.Xml.Serialization.XmlElementAttribute("column", typeof(savedqueryColumnsetxmlColumnsetColumn))]
        [System.Xml.Serialization.XmlElementAttribute("descend", typeof(object))]
        [System.Xml.Serialization.XmlElementAttribute("filter", typeof(savedqueryColumnsetxmlColumnsetFilter))]
        [System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemsElementName")]
        public object[] Items
        {
            get
            {
                return this.itemsField;
            }
            set
            {
                this.itemsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("ItemsElementName")]
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public ItemsChoice[] ItemsElementName
        {
            get
            {
                return this.itemsElementNameField;
            }
            set
            {
                this.itemsElementNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string version
        {
            get
            {
                return this.versionField;
            }
            set
            {
                this.versionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool distinct
        {
            get
            {
                return this.distinctField;
            }
            set
            {
                this.distinctField = value;
                this.distinctFieldSpecified = true;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool distinctSpecified
        {
            get
            {
                return this.distinctFieldSpecified;
            }
            set
            {
                this.distinctFieldSpecified = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class savedqueryColumnsetxmlColumnsetColumn
    {

        private build buildField;

        private bool buildFieldSpecified;

        private string addedbyField;

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public build build
        {
            get
            {
                return this.buildField;
            }
            set
            {
                this.buildField = value;
                this.buildFieldSpecified = true;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool buildSpecified
        {
            get
            {
                return this.buildFieldSpecified;
            }
            set
            {
                this.buildFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string addedby
        {
            get
            {
                return this.addedbyField;
            }
            set
            {
                this.addedbyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class savedqueryColumnsetxmlColumnsetFilter
    {

        private savedqueryColumnsetxmlColumnsetFilterCondition[] conditionField;

        private string columnField;

        private FetchOperator operatorField;

        private bool operatorFieldSpecified;

        private string valueField;

        private string typeField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("condition")]
        public savedqueryColumnsetxmlColumnsetFilterCondition[] condition
        {
            get
            {
                return this.conditionField;
            }
            set
            {
                this.conditionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string column
        {
            get
            {
                return this.columnField;
            }
            set
            {
                this.columnField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public FetchOperator @operator
        {
            get
            {
                return this.operatorField;
            }
            set
            {
                this.operatorField = value;
                this.operatorFieldSpecified = true;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool operatorSpecified
        {
            get
            {
                return this.operatorFieldSpecified;
            }
            set
            {
                this.operatorFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class savedqueryColumnsetxmlColumnsetFilterCondition
    {

        private string columnField;

        private FetchOperator operatorField;

        private bool operatorFieldSpecified;

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string column
        {
            get
            {
                return this.columnField;
            }
            set
            {
                this.columnField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public FetchOperator @operator
        {
            get
            {
                return this.operatorField;
            }
            set
            {
                this.operatorField = value;
                this.operatorFieldSpecified = true;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool operatorSpecified
        {
            get
            {
                return this.operatorFieldSpecified;
            }
            set
            {
                this.operatorFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(IncludeInSchema = false)]
    public enum ItemsChoice
    {

        /// <remarks/>
        ascend,

        /// <remarks/>
        column,

        /// <remarks/>
        descend,

        /// <remarks/>
        filter,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class savedqueryLayoutxml
    {

        private savedqueryLayoutxmlGrid gridField;

        /// <remarks/>
        public savedqueryLayoutxmlGrid grid
        {
            get
            {
                return this.gridField;
            }
            set
            {
                this.gridField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class savedqueryLayoutxmlGrid
    {

        private savedqueryLayoutxmlGridRow rowField;

        private string nameField;

        private bool selectField;

        private string previewField;

        private string iconField;

        private string jumpField;

        private string objectField;

        private string disableInlineEditingField;

        private string iconrendererField;

        private string multilinerowsField;

        /// <remarks/>
        public savedqueryLayoutxmlGridRow row
        {
            get
            {
                return this.rowField;
            }
            set
            {
                this.rowField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool select
        {
            get
            {
                return this.selectField;
            }
            set
            {
                this.selectField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string preview
        {
            get
            {
                return this.previewField;
            }
            set
            {
                this.previewField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string icon
        {
            get
            {
                return this.iconField;
            }
            set
            {
                this.iconField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string jump
        {
            get
            {
                return this.jumpField;
            }
            set
            {
                this.jumpField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "integer")]
        public string @object
        {
            get
            {
                return this.objectField;
            }
            set
            {
                this.objectField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "integer")]
        public string disableInlineEditing
        {
            get
            {
                return this.disableInlineEditingField;
            }
            set
            {
                this.disableInlineEditingField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string iconrenderer
        {
            get
            {
                return this.iconrendererField;
            }
            set
            {
                this.iconrendererField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string multilinerows
        {
            get
            {
                return this.multilinerowsField;
            }
            set
            {
                this.multilinerowsField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class savedqueryLayoutxmlGridRow
    {

        private savedqueryLayoutxmlGridRowCell[] cellField;

        private string nameField;

        private string idField;

        private string multiobjectidfieldField;

        private string layoutstyleField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("cell")]
        public savedqueryLayoutxmlGridRowCell[] cell
        {
            get
            {
                return this.cellField;
            }
            set
            {
                this.cellField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string multiobjectidfield
        {
            get
            {
                return this.multiobjectidfieldField;
            }
            set
            {
                this.multiobjectidfieldField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string layoutstyle
        {
            get
            {
                return this.layoutstyleField;
            }
            set
            {
                this.layoutstyleField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2558.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class savedqueryLayoutxmlGridRowCell
    {

        private string nameField;

        private string widthField;

        private string labelIdField;

        private string labelField;

        private string descField;

        private string ishiddenField;

        private string disableSortingField;

        private string disableMetaDataBindingField;

        private string cellTypeField;

        private string imageproviderwebresourceField;

        private string imageproviderfunctionnameField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "integer")]
        public string width
        {
            get
            {
                return this.widthField;
            }
            set
            {
                this.widthField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string LabelId
        {
            get
            {
                return this.labelIdField;
            }
            set
            {
                this.labelIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string label
        {
            get
            {
                return this.labelField;
            }
            set
            {
                this.labelField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string desc
        {
            get
            {
                return this.descField;
            }
            set
            {
                this.descField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "integer")]
        public string ishidden
        {
            get
            {
                return this.ishiddenField;
            }
            set
            {
                this.ishiddenField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "integer")]
        public string disableSorting
        {
            get
            {
                return this.disableSortingField;
            }
            set
            {
                this.disableSortingField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "integer")]
        public string disableMetaDataBinding
        {
            get
            {
                return this.disableMetaDataBindingField;
            }
            set
            {
                this.disableMetaDataBindingField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string cellType
        {
            get
            {
                return this.cellTypeField;
            }
            set
            {
                this.cellTypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string imageproviderwebresource
        {
            get
            {
                return this.imageproviderwebresourceField;
            }
            set
            {
                this.imageproviderwebresourceField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string imageproviderfunctionname
        {
            get
            {
                return this.imageproviderfunctionnameField;
            }
            set
            {
                this.imageproviderfunctionnameField = value;
            }
        }
    }
    public enum LinkType
    {
        inner,
        outer,
        Inner = inner,
        Outer = outer
    }
}