using CRM.OData.DataTypes;
using CRM.OData.Layout;
using CRM.OData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CRM.OData
{
    public class Condition<T> where T : CRMEntity
    {
        #region Fields
        private List<FetchCondition> conditions = new List<FetchCondition>();
        private List<FetchOrder> orders = new List<FetchOrder>();
        private FetchFilter currentFilter = null;
        public bool? isActive { get; private set; } = true;

        public int PageStart { get; private set; } = 1;
        public int PageItemCount { get; private set; } = 20;
        public bool isDistinct { get; private set; } = false;
        public bool isNoLock { get; private set; } = true;
        #endregion
        #region LINQ
        private static object Evaluate(Expression e)
        {
            object ret = null;
            if (e.NodeType == ExpressionType.Constant)
                ret = ((ConstantExpression)e).Value;
            else ret = Expression.Lambda(e).Compile().DynamicInvoke();
            if (ret == null) return null;

            if (ret.GetType() == typeof(Nullable))
            {
                ret = ((dynamic)ret).Value;
            }
            if (ret != null && ret.GetType().BaseType == typeof(NullBase))
            {
                ret = ((NullBase)ret).Value;
            }
            if (ret is DateTime)
            {
                ret = ((DateTime)ret).ToUniversalTime().ToString("s") + "Z";
            }
            if (ret == null) return null;
            if (ret.GetType().IsEnum)
            {
                return (int)(dynamic)ret;
            }
            return ret;
        }
        private void Compile(Expression expression, FetchFilter currentFilter)
        {
            if (CompareWhereCondition(expression))
            {
                CompileCondition(expression as BinaryExpression, currentFilter);
            }
            else
            {
                CompileExpression(expression, currentFilter);
            }
        }
        private void CompileCondition(BinaryExpression expression, FetchFilter currentFilter)
        {
            FetchFilter rightFilter = null, leftFilter = null;

            if (CompareWhereCondition(expression.Left))
            {
                leftFilter = new FetchFilter();
                var leftBin = expression.Left as BinaryExpression;

                leftFilter.type = leftBin.NodeType == ExpressionType.OrElse ? FetchFilterType.or : FetchFilterType.and;
                if (CompareWhereCondition(expression.Right))
                {
                    rightFilter = new FetchFilter();
                    var rightBin = expression.Right as BinaryExpression;
                    rightFilter.type = rightBin.NodeType == ExpressionType.OrElse ? FetchFilterType.or : FetchFilterType.and;
                    Compile(expression.Right, rightFilter);
                }
                else
                {
                    rightFilter = new FetchFilter();
                    CompileExpression(expression.Right, rightFilter);
                }

                Compile(expression.Left, leftFilter);

                currentFilter.type = expression.NodeType == ExpressionType.OrElse ? FetchFilterType.or : FetchFilterType.and;
                currentFilter.AddFilter(leftFilter);
                currentFilter.AddFilter(rightFilter);
            }
            else
            {
                leftFilter = new FetchFilter();
                if (CompareWhereCondition(expression.Right))
                {
                    rightFilter = new FetchFilter();
                    var rightBin = expression.Right as BinaryExpression;
                    rightFilter.type = rightBin.NodeType == ExpressionType.OrElse ? FetchFilterType.or : FetchFilterType.and;

                    CompileExpression(expression.Left, leftFilter);
                    Compile(expression.Right, rightFilter);

                    currentFilter.type = expression.NodeType == ExpressionType.OrElse ? FetchFilterType.or : FetchFilterType.and;
                    currentFilter.AddFilter(leftFilter);
                    currentFilter.AddFilter(rightFilter);
                }
                else
                {
                    currentFilter.type = expression.NodeType == ExpressionType.OrElse ? FetchFilterType.or : FetchFilterType.and;
                    CompileExpression(expression.Left, currentFilter);
                    CompileExpression(expression.Right, currentFilter);
                }
            }
        }
        private object CompileExpression(Expression exp, FetchFilter currentFilter)
        {
            if (exp.NodeType == ExpressionType.Convert)
            {
                return CompileExpression((exp as UnaryExpression).Operand, currentFilter);
            }
            else if (exp is MemberExpression)
            {
                var member = exp as MemberExpression;
                if (member.Expression is ParameterExpression)
                {
                    var target = member.Expression as ParameterExpression;
                    var prop = member.Member;
                    if (target.Type.BaseType == typeof(CRMEntity))
                    {
                        var tableDef = target.Type.GetCustomAttribute<CRMEntityAttribute>();
                        var colDef = prop.GetCustomAttribute<CRMFieldAttribute>();
                        if (colDef == null)
                        {
                            if (Localization.LowercaseEN(prop.Name) == "crmentityid")
                            {
                                return tableDef.Name + "id";
                            }
                            return prop.Name;
                        }
                        return colDef.Name;
                    }
                }
                else if (member.Expression is MemberExpression)
                {
                    var lside = member.Expression as MemberExpression;
                    if (lside.Expression is MemberExpression)
                    {
                        var lside2 = lside.Expression as MemberExpression;
                        if (lside2.Expression is ParameterExpression)
                        {
                            var target = lside2.Expression as ParameterExpression;
                            var prop = lside2.Member;
                            if (target.Type.BaseType == typeof(CRMEntity))
                            {
                                var tableDef = target.Type.GetCustomAttribute<CRMEntityAttribute>();
                                var colDef = prop.GetCustomAttribute<CRMFieldAttribute>();
                                return colDef.Name;
                            }
                        }
                    }
                }
                if (member.Expression != null && member.Expression.Type.BaseType == typeof(NullBase))
                {
                    return CompileExpression(member.Expression, currentFilter);
                }
                return Evaluate(exp);
            }
            else if (CompareWhereCondition(exp))
            {
                Compile(exp, currentFilter);
                return null;
            }
            else if (exp is BinaryExpression)
            {
                CompileBinaryExpression(exp as BinaryExpression, currentFilter);
                return null;
            }
            else
            {
                return Evaluate(exp);
            }
        }
        private void CompileBinaryExpression(BinaryExpression binaryExpression, FetchFilter currentFilter)
        {
            var A = CompileExpression(binaryExpression.Left, currentFilter).ToString();
            var B = CompileExpression(binaryExpression.Right, currentFilter);
            bool isStringOperation = binaryExpression.Left is MemberExpression ? (binaryExpression.Left as MemberExpression).Member.DeclaringType == typeof(string) : false;
            string stringOperationName = isStringOperation ? (binaryExpression.Left as MemberExpression).Member.Name : null;

            if (isStringOperation)
            {
                CompileStringOperation(A, B, stringOperationName, binaryExpression, currentFilter);
                return;
            }

            FetchOperator op = FetchOperator.eq;
            switch (binaryExpression.NodeType)
            {
                case ExpressionType.NotEqual: op = FetchOperator.ne; break;
                case ExpressionType.GreaterThan: op = FetchOperator.gt; break;
                case ExpressionType.GreaterThanOrEqual: op = FetchOperator.ge; break;
                case ExpressionType.LessThan: op = FetchOperator.lt; break;
                case ExpressionType.LessThanOrEqual: op = FetchOperator.le; break;
                case ExpressionType.MemberAccess:

                    break;
                case ExpressionType.Call:

                    break;
            }

            if (B == null && binaryExpression.NodeType == ExpressionType.Equal)
            {
                op = FetchOperator.@null;
            }
            if (B == null && binaryExpression.NodeType == ExpressionType.NotEqual)
            {
                op = FetchOperator.notnull;
            }

            Set(A, B, op, currentFilter);
        }

        private void CompileStringOperation(string a, object b, string stringOperationName, BinaryExpression binaryExpression, FetchFilter currentFilter)
        {
            switch (stringOperationName)
            {
                case "StartsWith":
                    Set(a, b, FetchOperator.beginswith, currentFilter);
                    break;
                case "EndsWith":
                    Set(a, b, FetchOperator.endswith, currentFilter);
                    break;
                case "Contains":
                    Set(a, b, FetchOperator.containvalues, currentFilter);
                    break;
            }

        }

        private static bool CompareWhereCondition(Expression body)
        {
            var b = body as BinaryExpression;
            return body is BinaryExpression && (b.NodeType == ExpressionType.AndAlso || b.NodeType == ExpressionType.OrElse);
        }

        public Condition<T> Set(System.Linq.Expressions.Expression<Func<T, bool>> body)
        {
            this.currentFilter = new FetchFilter();
            Compile(body.Body, this.currentFilter);
            return this;
        }
        #endregion

        #region Querying
        public Condition<T> Set(string Key, object Value, FetchOperator op = FetchOperator.eq, FetchFilter currentFilter = null)
        {
            if (Value == null && (op == FetchOperator.ne || op == FetchOperator.notnull)) op = FetchOperator.notnull;
            else if (Value == null) op = FetchOperator.@null;

            if (Value != null)
            {
                if (Value.GetType().BaseType == typeof(NullBase))
                {
                    Value = ((NullBase)(Value)).Value;
                }
            }
            if (Value is CRMReference)
            {
                Value = (EntityReference)((CRMReference)Value);
            }
            else if (Value is CRMMoney)
            {
                Value = ((Money)((CRMMoney)Value)).Value;
            }
            else if (Value is CRMId)
            {
                Value = (Guid)((CRMId)Value);
            }
            bool isEntityReference = false;
            string lookupEntityName = null;
            if (Value is EntityReference)
            {
                isEntityReference = true;
                lookupEntityName = ((EntityReference)Value).LogicalName;
                Value = ((EntityReference)Value).Id;
            }

            FetchCondition condition = null;

            if (op != FetchOperator.@null && op != FetchOperator.notnull)
            {
                condition = new FetchCondition() { attribute = Key, @operator = op, value = TypeConverter.ConvertToString(Value) };
            }
            else
            {
                condition = new FetchCondition() { attribute = Key, @operator = op };
            }

            if (isEntityReference)
            {
                condition.uitype = lookupEntityName;
            }

            if (currentFilter != null)
            {
                currentFilter.AddCondition(condition);
            }
            else
            {
                conditions.Add(condition);
            }
            return this;
        }
        public Condition<T> OrderBy(string Key, OrderType orderType = OrderType.Ascending)
        {
            orders.Add(new FetchOrder() { attribute = Key, descending = orderType == OrderType.Descending });
            return this;
        }
        public Condition<T> Page(int start, int itemcount = 10)
        {
            this.PageStart = start;
            this.PageItemCount = itemcount;
            return this;
        }
        public Condition<T> Distinct(bool value)
        {
            this.isDistinct = value;
            return this;
        }
        public Condition<T> NoLock(bool value)
        {
            this.isNoLock = value;
            return this;
        }
        public Condition<T> InactiveEntities()
        {
            this.isActive = false;
            return this;
        }
        public Condition<T> AllEntities()
        {
            this.isActive = null;
            return this;
        }
        #endregion

        #region Methods
        public FetchWrapper CreateQuery(string entityName, Dictionary<string, Tuple<CRMFieldAttribute, MemberInfo>> columns, string stateCodeFieldName = null)
        {
            var conditions = this.conditions.ToList();
            if (isActive.HasValue)
            {
                conditions.Add(new FetchCondition()
                {
                    attribute = stateCodeFieldName ?? "statecode",
                    @operator = FetchOperator.eq,
                    value = TypeConverter.ConvertToString((int)(isActive.Value ? EntityState.Active : EntityState.Inactive))
                });
            }

            foreach (var item in conditions)
            {
                var col = columns.Where(a => a.Key == item.attribute).FirstOrDefault();
                if (col.Value != null)
                {
                    item.attribute = col.Value.Item1.Name;
                }
                else
                {
                    if (Localization.LowercaseEN(item.attribute) == "crmentityid")
                    {
                        item.attribute = entityName + "id";
                    }
                }
            }
            var orders = this.orders.ToArray();
            foreach (var item in orders)
            {
                var col = columns.Where(a => a.Key == item.attribute).FirstOrDefault();
                if (col.Value != null)
                    item.attribute = col.Value.Item1.Name;
            }

            var exp = new FetchWrapper(entityName);
            if (this.currentFilter != null)
            {
                ((FetchEntity)exp.FetchExpression.Items[0]).AddFilter(currentFilter);
            }
            else
            {
                var filter = new FetchFilter() { type = FetchFilterType.and };
                foreach (var condition in conditions)
                    filter.AddCondition(condition);
                ((FetchEntity)exp.FetchExpression.Items[0]).AddFilter(filter);
            }
            exp.FetchExpression.page = Math.Max(1, this.PageStart).ToString();
            exp.FetchExpression.count = this.PageItemCount.ToString();
            foreach (var item in orders)
            {
                exp.AppendOrder(item.attribute, item.descending);
            }
            exp.FetchExpression.distinct = isDistinct;
            exp.NoLock = isNoLock;
            return exp;
        }

        public FetchWrapper CreateQuery(CRMEntityInformation entityInformation)
        {
            var conditions = this.conditions.ToList();
            if (isActive.HasValue)
            {
                conditions.Add(new FetchCondition()
                {
                    attribute = entityInformation.StateCodeFieldName ?? "statecode",
                    @operator = FetchOperator.eq,
                    value = TypeConverter.ConvertToString((int)(isActive.Value ? EntityState.Active : EntityState.Inactive))
                });
            }

            foreach (var item in conditions)
            {
                var col = entityInformation.Fields.Where(a => a.Key == item.attribute).FirstOrDefault();
                if (col.Value != null)
                {
                    item.attribute = col.Value.Item1.Name;
                }
                else
                {
                    if (Localization.LowercaseEN(item.attribute) == "crmentityid")
                    {
                        item.attribute = entityInformation.Entity.Name + "id";
                    }
                }
            }
            var orders = this.orders.ToArray();
            foreach (var item in orders)
            {
                var col = entityInformation.Fields.Where(a => a.Key == item.attribute).FirstOrDefault();
                if (col.Value != null)
                    item.attribute = col.Value.Item1.Name;
            }

            var exp = new FetchWrapper(entityInformation.Entity.Name);
            if (this.currentFilter != null)
            {
                ((FetchEntity)exp.FetchExpression.Items[0]).AddFilter(currentFilter);
            }
            else
            {
                var filter = new FetchFilter() { type = FetchFilterType.and };
                foreach (var condition in conditions)
                    filter.AddCondition(condition);
                ((FetchEntity)exp.FetchExpression.Items[0]).AddFilter(filter);
            }
            exp.FetchExpression.page = Math.Max(1, this.PageStart).ToString();
            exp.FetchExpression.count = this.PageItemCount.ToString();
            foreach (var item in orders)
            {
                exp.AppendOrder(item.attribute, item.descending);
            }
            exp.FetchExpression.distinct = isDistinct;
            exp.NoLock = isNoLock;

            foreach (var link in entityInformation.Links.Where(p => !p.FetchRuntime))
            {
                FetchLinkWrapper _link = new FetchLinkWrapper(link.SourceEntityName, link.ChildEntityName, link.SourceFieldName, LinkType.inner);
                exp.AppendLink(_link);
            }

            return exp;
        }
        #endregion
    }
}
