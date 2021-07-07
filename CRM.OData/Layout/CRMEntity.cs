using CRM.OData.DataTypes;
using CRM.OData;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CRM.OData.Layout
{
    public class CRMEntity
    {
        private static ConcurrentDictionary<Type, CRMEntityInformation> entityInformations = new ConcurrentDictionary<Type, CRMEntityInformation>();

        #region Construction
        public CRMEntity()
        {
            Init();
        }
        #endregion
        #region Fields
        public CRMId CRMEntityId { get; set; }
        public string CRMEntityName { get; set; }
        public Entity EntityRecord { get; internal set; }
        private List<string> markedFields = new List<string>();

        public CRMReference EntityReference => new CRMReference(CRMEntityId, CRMEntityName);
        public EntityState EntityState { get; set; }
        public int? EntityStatus { get; set; }
        #endregion
        #region Type Conversations
        public static implicit operator bool(CRMEntity entity)
        {
            return entity != null && entity.EntityRecord != null && entity.CRMEntityId.Id != Guid.Empty;
        }
        public static implicit operator CRMReference(CRMEntity entity)
        {
            return entity.EntityReference;
        }
        public static implicit operator CRMId(CRMEntity entity)
        {
            return entity.CRMEntityId;
        }
        #endregion
        #region Methods
        public static CRMEntityInformation GetInformation<T>() where T : CRMEntity
        {
            return GetInformation(typeof(T));
        }
        public static CRMEntityInformation GetInformation(Type entityType)
        {
            if (entityInformations.TryGetValue(entityType, out var entityInfo)) return entityInfo;

            CRMEntityInformation info = new CRMEntityInformation();
            info.Entity = entityType.GetCustomAttribute<CRMEntityAttribute>() ?? null;
            info.Fields = new Dictionary<string, Tuple<CRMFieldAttribute, MemberInfo>>();
            info.Relationships = new Dictionary<string, Tuple<CRMRelationshipAttribute, MemberInfo>>();
            info.Links = new List<CRMEntityLink>();
            info.IsDisabledEntityNameConvertion = entityType.GetCustomAttribute<DisableEntityNameConvertion>() != null;
            if (info.Entity == null) return null;
            foreach (var field in entityType.GetFields())
            {
                var fieldAttribute = field.GetCustomAttribute<CRMFieldAttribute>() ?? null;
                if (fieldAttribute != null)
                {
                    info.Fields[field.Name] = new Tuple<CRMFieldAttribute, MemberInfo>(fieldAttribute, field);
                   // bool isLinkedField = false;
                    Type linkedFieldType = null;
                    if (field.FieldType.BaseType == typeof(NullBase))
                    {
                        var t = field.FieldType.GenericTypeArguments[0];
                        if (t.GetCustomAttribute<CRMEntityAttribute>() != null)
                        {
                      //      isLinkedField = true;
                            linkedFieldType = t;
                        }
                    }
                    var linkedEntity = linkedFieldType.GetCustomAttribute<CRMEntityAttribute>();
                    var linkedEntityDefinition = field.GetCustomAttribute<CRMEntityAttribute>();
                    if (linkedEntity != null || linkedEntityDefinition != null)
                    {
                        if (linkedEntity == null && linkedEntityDefinition != null)
                        {
                            foreach (var linkedEntityName in linkedEntityDefinition.Names)
                            {
                                AddEntityLink(info, field, fieldAttribute, linkedEntityName);
                            }
                        }
                        else
                        {
                            AddEntityLink(info, field, fieldAttribute, linkedEntity?.Name ?? linkedEntityDefinition?.Name);
                        }
                    }
                }
                var relationShipAttribute = field.GetCustomAttribute<CRMRelationshipAttribute>() ?? null;
                if (relationShipAttribute != null)
                {
                    info.Relationships[relationShipAttribute.Name] = new Tuple<CRMRelationshipAttribute, MemberInfo>(relationShipAttribute, field);
                }
            }
            foreach (var property in entityType.GetProperties())
            {
                var fieldAttribute = property.GetCustomAttribute<CRMFieldAttribute>() ?? null;
                if (fieldAttribute != null)
                {
                    info.Fields[property.Name] = new Tuple<CRMFieldAttribute, MemberInfo>(fieldAttribute, property);
                    //bool isLinkedField = false;
                    Type linkedFieldType = property.PropertyType;
                    if (property.PropertyType.BaseType == typeof(NullBase) || property.PropertyType.BaseType == typeof(Nullable))
                    {
                        linkedFieldType = property.PropertyType.GenericTypeArguments[0];
                    }
                    var linkedEntity = linkedFieldType.GetCustomAttribute<CRMEntityAttribute>();
                    var linkedEntityDefinition = property.GetCustomAttribute<CRMEntityAttribute>();
                    if (linkedEntity != null || linkedEntityDefinition != null)
                    {
                        if (linkedEntity == null && linkedEntityDefinition != null)
                        {
                            foreach (var linkedEntityName in linkedEntityDefinition.Names)
                            {
                                AddEntityLink(info, property, fieldAttribute, linkedEntityName);
                            }
                        }
                        else
                        {
                            AddEntityLink(info, property, fieldAttribute, linkedEntity?.Name ?? linkedEntityDefinition?.Name);
                        }
                    }
                }
                var relationShipAttribute = property.GetCustomAttribute<CRMRelationshipAttribute>() ?? null;
                if (relationShipAttribute != null)
                {
                    info.Relationships[relationShipAttribute.Name] = new Tuple<CRMRelationshipAttribute, MemberInfo>(relationShipAttribute, property);
                }
            }

            var stateCodeField = (from field in info.Fields
                                  let stateCodeAttribute = field.Value.Item2.GetCustomAttribute<CRMStateCodeAttribute>()
                                  where stateCodeAttribute != null
                                  select new KeyValuePair<string, string>(field.Value.Item1.Name, stateCodeAttribute.Name)).FirstOrDefault();

            var statusCodeField = (from field in info.Fields
                                   let statusCodeAttribute = field.Value.Item2.GetCustomAttribute<CRMStatusCodeAttribute>()
                                   where statusCodeAttribute != null
                                   select new KeyValuePair<string, string>(field.Value.Item1.Name, statusCodeAttribute.Name)).FirstOrDefault();
            info.StateCodeFieldName = (stateCodeField.Value ?? stateCodeField.Key) ?? "statecode";
            info.StatusCodeFieldName = (statusCodeField.Value ?? statusCodeField.Key) ?? "statuscode";

            entityInformations[entityType] = info;

            return info;
        }

        private static void AddEntityLink(CRMEntityInformation info, MemberInfo field, CRMFieldAttribute fieldAttribute, string linkedEntityName)
        {
            CRMEntityLink link = new CRMEntityLink();
            link.SourceEntityName = info.Entity.Name;
            link.ChildEntityName = linkedEntityName;
            link.SourceFieldName = fieldAttribute.Name;
            link.ChildFieldName = $"{link.ChildEntityName}id";
            link.FetchRuntime = fieldAttribute.FetchRuntime;
            link.FieldDefinition = field;
            link.Field = fieldAttribute;
            info.Links.Add(link);
        }

        public static T Box<T>(Entity rawEntity, CRMEntityInformation entityInformation, Dictionary<Type, Func<object, object>> registeredTypeConverters, CRMManager cRMManager) where T : CRMEntity
        {
            return (T)Box(typeof(T), rawEntity, entityInformation, registeredTypeConverters, cRMManager);
        }
        public static CRMEntity Box(Type type, Entity rawEntity, CRMEntityInformation entityInformation, Dictionary<Type, Func<object, object>> registeredTypeConverters, CRMManager cRMManager)
        {
            CRMEntity result = (CRMEntity)Activator.CreateInstance(type);
            if (rawEntity == null)
            {
                return result;
            }
            result.CRMEntityId = rawEntity.Id;
            result.CRMEntityName = rawEntity.LogicalName;
            result.EntityRecord = rawEntity;
            if (rawEntity.TryGetAttributeValue<OptionSetValue>("statecode", out OptionSetValue statecode))
            {
                result.EntityState = (EntityState)statecode.Value;
            }
            else
            {
                result.EntityState = EntityState.Unknown;
            }

            if (rawEntity.TryGetAttributeValue<OptionSetValue>("statuscode", out OptionSetValue statuscode))
            {
                result.EntityStatus = statuscode.Value;
            }
            else
            {
                result.EntityStatus = null;
            }

            int linkedIndex = 1;
            if (type != typeof(CRMEntity))
            {
                foreach (var field in entityInformation.Fields)
                {
                    MemberInfo fieldMember = field.Value.Item2;
                    object fieldValue = null;
                    if (!rawEntity.TryGetAttributeValue(field.Value.Item1.Name, out fieldValue))
                    {
                        continue;
                    }

                    Type fieldValueType = fieldValue?.GetType();

                    if (registeredTypeConverters.TryGetValue(fieldValueType, out var typeconverter))
                    {
                        fieldValue = typeconverter(fieldValue);
                    }


                    if (fieldMember.MemberType == MemberTypes.Field)
                    {
                        fieldValueType = ((FieldInfo)fieldMember).FieldType;
                    }
                    else if (fieldMember.MemberType == MemberTypes.Property)
                    {
                        fieldValueType = ((PropertyInfo)fieldMember).PropertyType;
                    }

                    bool isnullablefield = false;

                    if (fieldValueType.BaseType == typeof(NullBase))
                    {
                        isnullablefield = true;
                    }

                    Type rawfieldValueType = fieldValueType.BaseType == typeof(NullBase) ? fieldValueType.GenericTypeArguments[0] : fieldValueType;

                    if (field.Value.Item1.IsEnum)
                    {
                        fieldValue = (TypeConverter.Convert<OptionSetValue>(fieldValue)).Value;
                        if (rawfieldValueType.IsEnum)
                        {
                            fieldValue = Enum.ToObject(rawfieldValueType, fieldValue);
                        }

                    }

                    if (fieldValue is EntityReference)
                    {
                        var _ref = (fieldValue as EntityReference);
                        fieldValue = new CRMReference(_ref.Id, _ref.LogicalName);
                    }
                    else if (fieldValue is Guid)
                    {
                        fieldValue = new CRMId((Guid)fieldValue);
                    }
                    else if (fieldValue is Money)
                    {
                        fieldValue = new CRMMoney((Money)fieldValue);
                    }

                    bool islinkedField = false;
                    Type linkedFieldType = fieldValueType;

                    if (fieldValueType.BaseType == typeof(NullBase))
                    {
                        var t = fieldValueType.GenericTypeArguments[0];
                        var tEntity = t.GetCustomAttribute<CRMEntityAttribute>();
                        if (tEntity != null || t == typeof(CRMEntity))
                        {
                            islinkedField = true;
                            linkedFieldType = t;
                        }

                        if (!islinkedField)
                            fieldValue = Activator.CreateInstance(fieldValueType, Convert.ChangeType(fieldValue, rawfieldValueType));
                    }
                    else if (fieldValueType.BaseType == typeof(Nullable))
                    {
                        var t = fieldValueType.GenericTypeArguments[0];
                        var tEntity = t.GetCustomAttribute<CRMEntityAttribute>();
                        if (tEntity != null)
                        {
                            islinkedField = true;
                            linkedFieldType = t;
                        }
                    }
                    else
                    {
                        var t = fieldValueType;
                        var tEntity = t.GetCustomAttribute<CRMEntityAttribute>();
                        if (tEntity != null)
                        {
                            islinkedField = true;
                            linkedFieldType = t;
                        }
                    }

                    if (islinkedField)
                    {
                        string targetEntityName = null;
                        if (fieldValue is CRMReference)
                        {
                            targetEntityName = ((CRMReference)fieldValue).LogicalName;
                        }
                        var link = entityInformation.Links.Where(a => a.SourceFieldName == field.Value.Item1.Name && a.ChildEntityName == targetEntityName).FirstOrDefault();
                        Entity linkedEntity = new Entity(link.ChildEntityName);


                        var linkedEntityRawFields = rawEntity.Attributes.Where(a => a.Key.StartsWith($"{link.ChildEntityName}{linkedIndex}.")).Select(a => new KeyValuePair<string, object>(a.Key.Split('.').Last(), a.Value)).ToArray();

                        linkedEntity.SetAttributes(linkedEntityRawFields);

                        var linkedEntityInfo = CRMEntity.GetInformation(linkedFieldType);
                        fieldValue = Box(linkedFieldType, linkedEntity, linkedEntityInfo, registeredTypeConverters, cRMManager);
                        ((CRMEntity)fieldValue).CRMEntityId = rawEntity.Id;

                        if (isnullablefield)
                        {
                            fieldValue = Activator.CreateInstance(typeof(Null<>).MakeGenericType(linkedFieldType), fieldValue);
                        }

                        linkedIndex++;
                    }

                    if (fieldMember.MemberType == MemberTypes.Field)
                    {
                        ((FieldInfo)fieldMember).SetValue(result, fieldValue);
                    }
                    else if (fieldMember.MemberType == MemberTypes.Property)
                    {
                        ((PropertyInfo)fieldMember).SetValue(result, fieldValue);
                    }
                }
                /*  foreach (var rship in entityInformation.Relationships)
                  {
                      MemberInfo fieldMember = rship.Value.Item2;
                      CRMRelationshipAttribute crmRelationship = rship.Value.Item1;

                      CRMRelationship fieldValue = new CRMRelationship();
                      fieldValue.EntityId = rawEntity.Id;
                      fieldValue.EntityName = rawEntity.LogicalName;
                      fieldValue.RelationshipName = crmRelationship.Name;
                      fieldValue.RootEntity = crmRelationship.RootEntity;
                      fieldValue.RootEntityFieldName = crmRelationship.RootEntityFieldName;
                      fieldValue.ChildEntity = crmRelationship.ChildEntity;
                      fieldValue.ChildEntityFieldName = crmRelationship.ChildEntityFieldName;
                      fieldValue.Columns = entityInformation.Fields;
                      fieldValue.Manager = cRMManager;

                      if (fieldMember.MemberType == MemberTypes.Field)
                      {
                          ((FieldInfo)fieldMember).SetValue(result, fieldValue);
                      }
                      else if (fieldMember.MemberType == MemberTypes.Property)
                      {
                          ((PropertyInfo)fieldMember).SetValue(result, fieldValue);
                      }
                  }*/
            }
            return result;
        }
        public Entity UnBox(CRMEntityInformation entityInformation, Dictionary<Type, Func<object, object>> registeredTypeConverters)
        {
            if (this.GetType() == typeof(DynamicCRMEntity))
            {
                return this.EntityRecord;
            }

            Entity result = new Entity(entityInformation.Entity.Name);
            result.Id = this.CRMEntityId;
            if (this.EntityState != EntityState.Unknown)
            {
                result[entityInformation.StateCodeFieldName] = new OptionSetValue((int)this.EntityState);
            }
            if (this.EntityStatus.HasValue)
            {
                result[entityInformation.StatusCodeFieldName] = new OptionSetValue(this.EntityStatus.Value);
            }
            foreach (var field in entityInformation.Fields)
            {
                object fieldValue = null;
                Type fieldType = typeof(object);
                if (markedFields.Count > 0)
                {
                    if (!markedFields.Contains(field.Value.Item1.Name)) continue;
                }
                if (field.Value.Item2.GetCustomAttribute<System.ComponentModel.ReadOnlyAttribute>()?.IsReadOnly == true)
                {
                    continue;
                }
                if (field.Value.Item2.MemberType == MemberTypes.Field)
                {
                    fieldValue = ((FieldInfo)field.Value.Item2).GetValue(this);
                    fieldType = ((FieldInfo)field.Value.Item2).FieldType;
                }
                else if (field.Value.Item2.MemberType == MemberTypes.Property)
                {
                    fieldValue = ((PropertyInfo)field.Value.Item2).GetValue(this);
                    fieldType = ((PropertyInfo)field.Value.Item2).PropertyType;
                }

                if (fieldType.BaseType == typeof(NullBase) && fieldValue != null)
                {
                    fieldValue = ((NullBase)fieldValue).Value;
                    fieldType = fieldType.GetGenericArguments().FirstOrDefault();
                }
                if (fieldValue == null) continue;

                bool isConverted = false;
                if (fieldValue != null)
                {
                    if (registeredTypeConverters.TryGetValue(fieldValue.GetType(), out var converter))
                    {
                        fieldValue = converter(fieldValue);
                        isConverted = true;
                    }
                }

                if (!isConverted)
                {
                    //Convert OptionSetValue
                    if (field.Value.Item1.IsEnum)
                    {
                        fieldValue = new OptionSetValue(Convert.ToInt32(fieldValue));
                    }

                    if (fieldValue is CRMReference) fieldValue = (EntityReference)((CRMReference)fieldValue);
                    else if (fieldValue is CRMMoney) fieldValue = (Money)((CRMMoney)fieldValue);
                    else if (fieldValue is CRMOptionSet) fieldValue = new OptionSetValue(((CRMOptionSet)fieldValue).Value);
                }

                if (fieldValue is CRMReference)
                {
                    fieldValue = (EntityReference)((CRMReference)fieldValue);
                }
                else if (fieldValue is CRMMoney)
                {
                    fieldValue = new Money(((CRMMoney)fieldValue).Value);
                }

                result[field.Value.Item1.Name] = fieldValue;
            }
            return result;
        }
        protected virtual void Init()
        {
            var info = GetInformation(this.GetType());
            if (info != null)
            {
                foreach (var field in info.Fields)
                {
                    var member = field.Value.Item2;
                    bool isNullable = false;
                    if (member.MemberType == MemberTypes.Field)
                    {
                        isNullable = (member as FieldInfo).FieldType.BaseType == typeof(NullBase);
                    }
                    else if (member.MemberType == MemberTypes.Property)
                    {
                        isNullable = (member as PropertyInfo).PropertyType.BaseType == typeof(NullBase);
                    }

                    if (isNullable)
                    {
                        if (member.MemberType == MemberTypes.Field) (member as FieldInfo).SetValue(this, Activator.CreateInstance((member as FieldInfo).FieldType));
                        else if (member.MemberType == MemberTypes.Property) (member as PropertyInfo).SetValue(this, Activator.CreateInstance((member as PropertyInfo).PropertyType));
                    }
                }
            }
        }
        public bool Exist()
        {
            return EntityRecord != null && CRMEntityId.Id != Guid.Empty;
        }
        public void MarkField(string fieldName)
        {
            if (!this.markedFields.Contains(fieldName))
                this.markedFields.Add(fieldName);
        }
        #endregion
    }
}
