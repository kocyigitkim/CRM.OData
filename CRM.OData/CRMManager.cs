using Dynamics365.OData.DataTypes;
using Dynamics365.OData.Layout;
using Dynamics365.OData;
using Dynamics365.OData;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dynamics365.OData
{
    public class CRMManager : IDisposable
    {
        internal static Dictionary<Type, Func<object, object>> RegisteredTypeConverters = new Dictionary<Type, Func<object, object>>();
        private CRMConnection _connection;
        public CRMConnection Connection => _connection;

        public event Action<Exception> Error;
        public event Action<Exception> ConnectionError;
        public bool ReadOnly { get; set; } = false;
        internal void OnError(Exception exception)
        {
            Error?.Invoke(exception);
        }
        internal void OnConnectionError(Exception exception)
        {
            ConnectionError?.Invoke(exception);
        }
        public CRMManager(CRMConnection connection)
        {
            RegisterTypeConverter(typeof(CRMId), (arg) =>
            {
                if (arg is CRMId)
                {
                    return ((CRMId)arg).Id;
                }

                if (arg is string)
                {
                    if (Guid.TryParse(arg.ToString(), out var varg))
                    {
                        arg = varg;
                    }
                }

                Guid gArg = (Guid)arg;
                return new CRMId(gArg);
            });
            this._connection = connection;
        }
        public static void RegisterTypeConverter(Type type, Func<object, object> converter)
        {
            RegisteredTypeConverters[type] = converter;
        }

        public static Dictionary<Type, Func<object, object>> GetRegisteredTypeConverters()
        {
            return RegisteredTypeConverters;
        }
        public static void UnregisterTypeConverter(Type type)
        {
            if (RegisteredTypeConverters.ContainsKey(type))
            {
                RegisteredTypeConverters.Remove(type);
            }
        }

        public bool Connect()
        {
            return this._connection.Connect(this);
        }
        public async Task<bool> ConnectAsync()
        {
            return await this._connection.ConnectAsync(this);
        }
        internal ODataCRMProxy Service
        {
            get
            {
                if (!_connection.IsConnected)
                {
                    _connection.Connect(this);
                }
                return _connection.Proxy;
            }
        }

        #region Methods
        #region Find
        public T FindById<T>(string EntityName, CRMId id) where T : CRMEntity
        {
            var info = CRMEntity.GetInformation<T>();
            Entity rawEntity;
            try
            {
                rawEntity = FindByIdInternal(EntityName, id, info);
            }
            catch (Exception exception)
            {
                Error?.Invoke(exception);
                return null;
            }
            try
            {
                var record = CRMEntity.Box<T>(rawEntity, info, RegisteredTypeConverters, this);
                EntityLinksFetchRuntime(info, record);
                return record;
            }
            catch (Exception exception)
            {
                Error?.Invoke(exception);
                return null;
            }
        }
        public async Task<T> FindByIdAsync<T>(string EntityName, CRMId id) where T : CRMEntity
        {
            var info = CRMEntity.GetInformation<T>();
            Entity rawEntity;
            try
            {
                rawEntity = await FindByIdInternalAsync(EntityName, id, info);
            }
            catch (Exception exception)
            {
                Error?.Invoke(exception);
                return null;
            }
            try
            {
                var record = CRMEntity.Box<T>(rawEntity, info, RegisteredTypeConverters, this);
                await EntityLinksFetchRuntimeAsync(info, record);
                return record;
            }
            catch (Exception exception)
            {
                Error?.Invoke(exception);
                return null;
            }
        }
        private Entity FindByIdInternal(string logicalname, CRMId id, CRMEntityInformation information)
        {
            FetchWrapper wrapper = CreateFindByIdInternalQuery(logicalname, id, information);
            var response = Service.RetrieveMultiple(wrapper, information?.IsDisabledEntityNameConvertion ?? false);
            return response.entities.FirstOrDefault();
        }

        private static FetchWrapper CreateFindByIdInternalQuery(string logicalname, CRMId id, CRMEntityInformation information)
        {
            var wrapper = new FetchWrapper(logicalname);
            var exp = wrapper.FetchExpression;
            wrapper.AppendFilter($"{logicalname}id", id.RawId.Replace("{", "").Replace("}", "").ToLower());

            if (information != null)
            {
                foreach (var link in information.Links)
                {
                    var _link = new FetchLinkWrapper(link.SourceEntityName, link.ChildEntityName, link.SourceFieldName, LinkType.inner);
                    wrapper.AppendLink(_link);
                }
            }

            return wrapper;
        }

        private async Task<Entity> FindByIdInternalAsync(string logicalname, CRMId id, CRMEntityInformation information)
        {
            var wrapper = CreateFindByIdInternalQuery(logicalname, id, information);
            var response = await Service.RetrieveMultipleAsync(wrapper, information?.IsDisabledEntityNameConvertion ?? false);
            return response.entities.FirstOrDefault();
        }
        public async Task<CRMEntity> FindByIdAsync(CRMReference _ref)
        {
            return await FindByIdAsync<CRMEntity>(_ref.LogicalName, _ref.Id);
        }
        public CRMEntity FindById(CRMReference _ref)
        {
            return FindById<CRMEntity>(_ref.LogicalName, _ref.Id);
        }
        public T FindById<T>(CRMId id) where T : CRMEntity
        {
            var entityInfo = CRMEntity.GetInformation<T>();
            Entity rawEntity = FindByIdInternal(entityInfo.Entity.Name, id.Id, entityInfo);
            try
            {
                T record = CRMEntity.Box<T>(rawEntity, entityInfo, RegisteredTypeConverters, this);
                EntityLinksFetchRuntime(entityInfo, record);
                return record;
            }
            catch (Exception exception)
            {
                Error?.Invoke(exception);
                return null;
            }
        }
        public async Task<T> FindByIdAsync<T>(CRMId id) where T : CRMEntity
        {
            var entityInfo = CRMEntity.GetInformation<T>();
            Entity rawEntity = await FindByIdInternalAsync(entityInfo.Entity.Name, id.Id, entityInfo);
            try
            {
                T record = CRMEntity.Box<T>(rawEntity, entityInfo, RegisteredTypeConverters, this);
                await EntityLinksFetchRuntimeAsync(entityInfo, record);
                return record;
            }
            catch (Exception exception)
            {
                Error?.Invoke(exception);
                return null;
            }
        }

        public CRMEntityCollection<T> FindByCondition<T>(Condition<T> condition) where T : CRMEntity
        {
            var entityInfo = CRMEntity.GetInformation<T>();
            FetchWrapper _query;
            try
            {
                _query = condition.CreateQuery(entityInfo);
                _query.FetchExpression.returntotalrecordcount = true;
            }
            catch (Exception exception)
            {
                Error?.Invoke(exception);
                return null;
            }
            EntityCollection rawEntities = null;
            try
            {
                rawEntities = Service.RetrieveMultiple(_query, entityInfo?.IsDisabledEntityNameConvertion ?? false);
            }
            catch (Exception exception)
            {
                Error?.Invoke(exception);
                return new CRMEntityCollection<T>() { Entities = new T[0] };
            }
            try
            {
                var entities = (from item in rawEntities.entities select CRMEntity.Box<T>(item, entityInfo, RegisteredTypeConverters, this)).ToArray();
                try
                {
                    foreach (var entity in entities)
                    {
                        EntityLinksFetchRuntime(entityInfo, entity);
                    }
                }
                catch (Exception error)
                {
                    this.Error?.Invoke(error);
                }

                return new CRMEntityCollection<T>()
                {
                    Entities = entities,
                    MoreRecords = rawEntities.MoreRecords,
                    TotalRecordCount = rawEntities.TotalRecordCount,
                    TotalRecordCountLimitExceeded = rawEntities.TotalRecordCountLimitExceeded,
                    _query = _query,
                    _manager = this
                };
            }
            catch (Exception error)
            {
                this.Error?.Invoke(error);
                return new CRMEntityCollection<T>();
            }
        }
        public CRMEntityCollection<T> FindByCondition<T>(string EntityName, Condition<T> condition, bool isDisabledConvertion = false) where T : CRMEntity
        {
            FetchWrapper _query;
            try
            {
                _query = condition.CreateQuery(EntityName, new Dictionary<string, Tuple<CRMFieldAttribute, System.Reflection.MemberInfo>>());
                _query.FetchExpression.returntotalrecordcount = true;
            }
            catch (Exception exception)
            {
                Error?.Invoke(exception);
                return null;
            }
            EntityCollection rawEntities = null;
            try
            {
                rawEntities = Service.RetrieveMultiple(_query, isDisabledConvertion);
            }
            catch (Exception exception)
            {
                Error?.Invoke(exception);
                return new CRMEntityCollection<T>() { Entities = new T[0] };
            }
            try
            {
                var entities = (from item in rawEntities.entities select CRMEntity.Box<T>(item, null, RegisteredTypeConverters, this)).ToArray();

                return new CRMEntityCollection<T>()
                {
                    Entities = entities,
                    MoreRecords = rawEntities.MoreRecords,
                    TotalRecordCount = rawEntities.TotalRecordCount,
                    TotalRecordCountLimitExceeded = rawEntities.TotalRecordCountLimitExceeded,
                    _query = _query,
                    _manager = this
                };
            }
            catch (Exception error)
            {
                this.Error?.Invoke(error);
                return new CRMEntityCollection<T>();
            }
        }

        public async Task<CRMEntityCollection<T>> FindByConditionAsync<T>(Condition<T> condition) where T : CRMEntity
        {
            var entityInfo = CRMEntity.GetInformation<T>();
            FetchWrapper _query;
            try
            {
                _query = condition.CreateQuery(entityInfo);
                _query.FetchExpression.returntotalrecordcount = true;
            }
            catch (Exception exception)
            {
                Error?.Invoke(exception);
                return null;
            }
            EntityCollection rawEntities = null;
            try
            {
                rawEntities = await Service.RetrieveMultipleAsync(_query, entityInfo.IsDisabledEntityNameConvertion);
            }
            catch (Exception exception)
            {
                Error?.Invoke(exception);
                return new CRMEntityCollection<T>() { Entities = new T[0] };
            }
            try
            {
                var entities = (from item in rawEntities.entities select CRMEntity.Box<T>(item, entityInfo, RegisteredTypeConverters, this)).ToArray();
                try
                {
                    foreach (var entity in entities)
                    {
                        await EntityLinksFetchRuntimeAsync(entityInfo, entity);
                    }
                }
                catch (Exception error)
                {
                    this.Error?.Invoke(error);
                }

                return new CRMEntityCollection<T>()
                {
                    Entities = entities,
                    MoreRecords = rawEntities.MoreRecords,
                    TotalRecordCount = rawEntities.TotalRecordCount,
                    TotalRecordCountLimitExceeded = rawEntities.TotalRecordCountLimitExceeded,
                    _query = _query,
                    _manager = this
                };
            }
            catch (Exception error)
            {
                this.Error?.Invoke(error);
                return new CRMEntityCollection<T>();
            }
        }
        public async Task<CRMEntityCollection<T>> FindByConditionAsync<T>(string EntityName, Condition<T> condition, bool isDisabledConvertion = false) where T : CRMEntity
        {
            FetchWrapper _query;
            try
            {
                _query = condition.CreateQuery(EntityName, new Dictionary<string, Tuple<CRMFieldAttribute, System.Reflection.MemberInfo>>());
                _query.FetchExpression.returntotalrecordcount = true;
            }
            catch (Exception exception)
            {
                Error?.Invoke(exception);
                return null;
            }
            EntityCollection rawEntities = null;
            try
            {
                rawEntities = await Service.RetrieveMultipleAsync(_query, isDisabledConvertion);
            }
            catch (Exception exception)
            {
                Error?.Invoke(exception);
                return new CRMEntityCollection<T>() { Entities = new T[0] };
            }
            try
            {
                var entities = (from item in rawEntities.entities select CRMEntity.Box<T>(item, null, RegisteredTypeConverters, this)).ToArray();

                return new CRMEntityCollection<T>()
                {
                    Entities = entities,
                    MoreRecords = rawEntities.MoreRecords,
                    TotalRecordCount = rawEntities.TotalRecordCount,
                    TotalRecordCountLimitExceeded = rawEntities.TotalRecordCountLimitExceeded,
                    _query = _query,
                    _manager = this
                };
            }
            catch (Exception error)
            {
                this.Error?.Invoke(error);
                return new CRMEntityCollection<T>();
            }
        }
        #endregion
        #region Associate
        public async Task<bool> AssociateAsync(CRMReference source, CRMReference dest, string RelationShipName, bool IsDisabledConvertion = false)
        {
            return await _connection.Proxy.AssociateAsync(source, dest, RelationShipName, IsDisabledConvertion);
        }
        #endregion
        #region Insert / Update / Delete
        public bool Insert(CRMEntity entity)
        {
            try
            {
                var entityInfo = CRMEntity.GetInformation(entity.GetType());
                Entity unboxed = null;
                if (entityInfo != null)
                {
                    unboxed = entity.UnBox(entityInfo, RegisteredTypeConverters);
                }
                else
                {
                    unboxed = entity.EntityRecord;
                }
                if (ReadOnly) throw new Exception("Can't insert an entity because of CRM Manager is readonly");
                entity.CRMEntityId = Service.Create(unboxed, entityInfo.IsDisabledEntityNameConvertion);
                entity.EntityRecord = unboxed;
                entity.CRMEntityName = entityInfo.Entity.Name;
                return true;
            }
            catch (Exception exception)
            {
                Error?.Invoke(exception);
                return false;
            }
        }
        public async Task<bool> InsertAsync(CRMEntity entity)
        {
            try
            {
                var entityInfo = CRMEntity.GetInformation(entity.GetType());
                Entity unboxed = null;
                if (entityInfo != null)
                {
                    unboxed = entity.UnBox(entityInfo, RegisteredTypeConverters);
                }
                else
                {
                    unboxed = entity.EntityRecord;
                }
                if (ReadOnly) throw new Exception("Can't insert an entity because of CRM Manager is readonly");
                entity.CRMEntityId = await Service.CreateAsync(unboxed, entityInfo?.IsDisabledEntityNameConvertion ?? false);
                if (entityInfo != null)
                {
                    entity.EntityRecord = unboxed;
                    entity.CRMEntityName = entityInfo.Entity.Name;
                }
                return true;
            }
            catch (Exception exception)
            {
                Error?.Invoke(exception);
                return false;
            }
        }
        public async Task<bool> UpdateAsync(CRMEntity entity)
        {
            try
            {
                var entityInfo = CRMEntity.GetInformation(entity.GetType());
                Entity unboxed = null;
                if (entityInfo != null)
                {
                    unboxed = entity.UnBox(entityInfo, RegisteredTypeConverters);
                }
                else
                {
                    unboxed = entity.EntityRecord;
                }
                if (!CheckEntityUpdate(entity.EntityRecord, unboxed))
                {
                    return true;
                }
                if (ReadOnly) throw new Exception("Can't update an entity because of CRM Manager is readonly");
                return await Service.UpdateAsync(unboxed, entityInfo?.IsDisabledEntityNameConvertion ?? false);
            }
            catch (Exception exception)
            {
                Error?.Invoke(exception);
                return false;
            }
        }
        public bool Update(CRMEntity entity)
        {
            try
            {
                var entityInfo = CRMEntity.GetInformation(entity.GetType());
                Entity unboxed = null;
                if (entityInfo != null)
                {
                    unboxed = entity.UnBox(entityInfo, RegisteredTypeConverters);
                }
                else
                {
                    unboxed = entity.EntityRecord;
                }
                if (!CheckEntityUpdate(entity.EntityRecord, unboxed))
                {
                    return true;
                }
                if (ReadOnly) throw new Exception("Can't update an entity because of CRM Manager is readonly");
                return Service.Update(unboxed, entityInfo?.IsDisabledEntityNameConvertion ?? false);
            }
            catch (Exception exception)
            {
                Error?.Invoke(exception);
                return false;
            }
        }
        private bool CheckEntityUpdate(Entity entityRecord, Entity unboxed)
        {
            return true;
            /* bool CheckEquality(object a, object b)
             {
                 Type tA = null, tB = null;
                 if (a != null)
                 {
                     tA = a.GetType();
                 }
                 if (b != null)
                 {
                     tB = b.GetType();
                 }
                 if (a == null && b == null) return false;
                 else if ((a == null && b == null) || (a != null && b != null)) return true;
                 if (tA == typeof(EntityReference))
                 {
                     if (tB == typeof(EntityReference))
                     {
                         var refA = ((EntityReference)a);
                         var refB = ((EntityReference)b);
                         var vA = refA.Id.ToString() + refA.LogicalName;
                         var vB = refB.Id.ToString() + refB.LogicalName;
                         return vA == vB;
                     }
                 }
                 else if (tA == typeof(Money))
                 {
                     if (tB == typeof(Money))
                     {
                         var refA = ((Money)a);
                         var refB = ((Money)b);
                         return refA.Value == refB.Value;
                     }
                 }
                 else if (tA == typeof(OptionSetValue))
                 {
                     if (tB == typeof(OptionSetValue))
                     {
                         var refA = ((OptionSetValue)a);
                         var refB = ((OptionSetValue)b);
                         return refA.Value == refB.Value;
                     }
                 }
                 return a == b;
             }
             var sourceAttributes = entityRecord.Attributes.ToArray();
             var destAttributes = unboxed.Attributes.ToArray();
             var newAttributes = destAttributes.Where(p => sourceAttributes.Where(s => s.Key == p.Key).Count() == 0).ToArray();
             var updatedAttributes = destAttributes.Where(p => sourceAttributes.Where(s => s.Key == p.Key && !CheckEquality(s.Value, p.Value)).Count() > 0).ToArray();
             return newAttributes.Length > 0 || updatedAttributes.Length > 0;*/
        }
        public bool InsertOrUpdate(CRMEntity entity)
        {
            if (entity != null)
            {
                if (entity.CRMEntityId.Id != Guid.Empty) return Update(entity);
                else return Insert(entity);
            }
            return false;
        }
        public async Task<bool> InsertOrUpdateAsync(CRMEntity entity)
        {
            if (entity != null)
            {
                if (entity.CRMEntityId.Id != Guid.Empty) return await UpdateAsync(entity);
                else return await InsertAsync(entity);
            }
            return false;
        }

        #endregion
        #region Type Conversation
        public CRMEntity Cast(Type type, CRMEntity entity)
        {
            var entityInformation = CRMEntity.GetInformation(type);
            CRMEntity record = CRMEntity.Box(type, entity.EntityRecord, entityInformation, RegisteredTypeConverters, this);
            EntityLinksFetchRuntime(entityInformation, record);
            return record;
        }
        public T Cast<T>(CRMEntity entity) where T : CRMEntity
        {
            var entityInformation = CRMEntity.GetInformation(typeof(T));
            T record = CRMEntity.Box<T>(entity.EntityRecord, entityInformation, RegisteredTypeConverters, this);
            EntityLinksFetchRuntime(entityInformation, record);
            return record;
        }
        public CRMEntityCollection<T> Cast<T, Y>(CRMEntityCollection<Y> collection) where T : CRMEntity where Y : CRMEntity
        {
            CRMEntityCollection<T> result = new CRMEntityCollection<T>();
            result.MoreRecords = collection.MoreRecords;
            result.TotalRecordCount = collection.TotalRecordCount;
            result.TotalRecordCountLimitExceeded = collection.TotalRecordCountLimitExceeded;
            result.Page = collection.Page;
            result.Entities = (from item in collection.Entities select Cast<T>(item)).ToArray();
            return result;
        }
        #endregion
        #region Query
        private void EntityLinksFetchRuntime<T>(CRMEntityInformation entityInfo, T entity) where T : CRMEntity
        {
            if (entityInfo == null) return;
            foreach (var link in entityInfo.Links.Where(p => p.FetchRuntime))
            {
                var entityRef = entity.EntityRecord.GetAttributeValue<EntityReference>(link.SourceFieldName);
                if (entityRef == null) continue;
                var linkedEntity = FindById(entityRef);
                EntityLinksFetchRuntimeInternal(entity, link, linkedEntity);
            }
        }
        private async Task EntityLinksFetchRuntimeAsync<T>(CRMEntityInformation entityInfo, T entity) where T : CRMEntity
        {
            if (entityInfo == null) return;
            foreach (var link in entityInfo.Links.Where(p => p.FetchRuntime))
            {
                var entityRef = entity.EntityRecord.GetAttributeValue<EntityReference>(link.SourceFieldName);
                if (entityRef == null) continue;
                var linkedEntity = await FindByIdAsync(entityRef);
                EntityLinksFetchRuntimeInternal(entity, link, linkedEntity);
            }
        }
        private void EntityLinksFetchRuntimeInternal<T>(T entity, CRMEntityLink link, CRMEntity linkedEntity) where T : CRMEntity
        {
            Type linkedEntityType = null;
            if (link.FieldDefinition is FieldInfo)
            {
                linkedEntityType = (link.FieldDefinition as FieldInfo).FieldType;
            }
            else if (link.FieldDefinition is PropertyInfo)
            {
                linkedEntityType = (link.FieldDefinition as PropertyInfo).PropertyType;
            }
            Type rawLinkedEntityType = linkedEntityType;
            bool isNullable = false;
            if (rawLinkedEntityType.BaseType == typeof(NullBase))
            {
                rawLinkedEntityType = rawLinkedEntityType.GenericTypeArguments[0];
                isNullable = true;
            }

            object castedEntity = rawLinkedEntityType != typeof(CRMEntity) ? Cast(rawLinkedEntityType, linkedEntity) : linkedEntity;

            if (isNullable)
            {
                castedEntity = Activator.CreateInstance(linkedEntityType, castedEntity);
            }

            if (link.FieldDefinition is FieldInfo)
            {
                (link.FieldDefinition as FieldInfo).SetValue(entity, castedEntity);
            }
            else if (link.FieldDefinition is PropertyInfo)
            {
                (link.FieldDefinition as PropertyInfo).SetValue(entity, castedEntity);
            }
        }

        public JToken Fetch(string xmlQuery, bool isDisabledConvertion = false)
        {
            return Service.FetchDirect(xmlQuery, isDisabledConvertion);
        }
        public async Task<JToken> FetchAsync(string xmlQuery, bool isDisabledConvertion = false)
        {
            return await Service.FetchDirectAsync(xmlQuery, isDisabledConvertion);
        }
        public CRMEntityCollection<T> RetrieveMultiple<T>(string fetchXML, bool isDisabledConvertion = false) where T : CRMEntity
        {
            var entityCollection = Service.RetrieveMultiple(fetchXML, isDisabledConvertion);
            return RetrieveMultipleInternal<T>(entityCollection);
        }
        public async Task<CRMEntityCollection<T>> RetrieveMultipleAsync<T>(string fetchXML, bool isDisabledConvertion = false) where T : CRMEntity
        {
            var entityCollection = await Service.RetrieveMultipleAsync(fetchXML, isDisabledConvertion);
            return RetrieveMultipleInternal<T>(entityCollection);
        }
        private CRMEntityCollection<T> RetrieveMultipleInternal<T>(EntityCollection entityCollection) where T : CRMEntity
        {
            CRMEntityCollection<T> result = new CRMEntityCollection<T>();
            result.MoreRecords = entityCollection.MoreRecords;
            result.Page = entityCollection.Page;
            result.TotalRecordCount = entityCollection.TotalRecordCount;
            result.TotalRecordCountLimitExceeded = entityCollection.TotalRecordCountLimitExceeded;
            var _entities = new List<T>();
            var entityInfo = CRMEntity.GetInformation<T>();
            foreach (var item in entityCollection.entities)
            {
                _entities.Add(CRMEntity.Box<T>(item, entityInfo, RegisteredTypeConverters, this));
            }

            try
            {
                foreach (var entity in _entities)
                {
                    EntityLinksFetchRuntime(entityInfo, entity);
                }
            }
            catch (Exception error)
            {
                this.Error?.Invoke(error);
            }

            result.Entities = _entities;
            return result;
        }

        public CRMEntityCollection<T> RetrieveMultiple<T>(FetchWrapper exp) where T : CRMEntity
        {
            return RetrieveMultiple<T>(exp.ConvertToFetchXml());
        }
        public async Task<CRMEntityCollection<T>> RetrieveMultipleAsync<T>(FetchWrapper exp) where T : CRMEntity
        {
            return await RetrieveMultipleAsync<T>(exp.ConvertToFetchXml());
        }
        #endregion
        #region State Management
        public bool SetState(CRMReference reference, int State, int Status, bool isDisabledConvertion = false)
        {
            try
            {
                if (ReadOnly) throw new Exception("Can't set entity's state because of CRM Manager is readonly");
                return Service.SetState(reference, State, Status, isDisabledConvertion);
            }
            catch (Exception exception)
            {
                Error?.Invoke(exception);
                return false;
            }
        }

        public bool SetState(CRMReference reference, bool State, int? Status = null, bool isDisabledConvertion = false)
        {
            if (!Status.HasValue)
            {
                Status = State ? 1 : 2;
            }
            return SetState(reference, (int)(State ? EntityState.Active : EntityState.Inactive), Status.Value, isDisabledConvertion);
        }
        public bool SetState(CRMReference reference, EntityState State, int? Status = null, bool isDisabledConvertion = false)
        {
            if (!Status.HasValue)
            {
                Status = State == EntityState.Active ? 1 : 2;
            }
            return SetState(reference, (int)State, Status.Value, isDisabledConvertion);
        }
        public bool SetState<TStateCode, TStatusCode>(CRMReference reference, TStateCode stateCode, TStatusCode statusCode, bool isDisabledConvertion = false) where TStateCode : struct where TStatusCode : struct
        {
            return SetState(reference, Convert.ToInt32(stateCode), Convert.ToInt32(statusCode), isDisabledConvertion);
        }

        public async Task<bool> SetStateAsync(CRMReference reference, int State, int Status, bool isDisabledConvertion = false)
        {
            try
            {
                if (ReadOnly) throw new Exception("Can't set entity's state because of CRM Manager is readonly");
                return await Service.SetStateAsync(reference, State, Status, isDisabledConvertion);
            }
            catch (Exception exception)
            {
                Error?.Invoke(exception);
                return false;
            }
        }
        public async Task<bool> SetStateAsync(CRMReference reference, bool State, int? Status = null, bool isDisabledConvertion = false)
        {
            if (!Status.HasValue)
            {
                Status = State ? 1 : 2;
            }
            return await SetStateAsync(reference, (int)(State ? EntityState.Active : EntityState.Inactive), Status.Value, isDisabledConvertion);
        }
        public async Task<bool> SetStateAsync(CRMReference reference, EntityState State, int? Status = null, bool isDisabledConvertion = false)
        {
            if (!Status.HasValue)
            {
                Status = State == EntityState.Active ? 1 : 2;
            }
            return await SetStateAsync(reference, (int)State, Status.Value, isDisabledConvertion);
        }
        public async Task<bool> SetStateAsync<TStateCode, TStatusCode>(CRMReference reference, TStateCode stateCode, TStatusCode statusCode, bool isDisabledConvertion = false) where TStateCode : struct where TStatusCode : struct
        {
            return await SetStateAsync(reference, Convert.ToInt32(stateCode), Convert.ToInt32(statusCode), isDisabledConvertion);
        }

        #endregion
        #endregion

        public void Dispose()
        {
            this.Connection.Dispose();
        }

    }
}
