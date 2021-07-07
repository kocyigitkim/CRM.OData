using CRM.OData;
using CRM.OData.Layout;
using CRM.OData.Metadata;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace CRM.OData
{
    public class ODataCRMProxy : IDisposable
    {
        public event Action<Exception> Error;
        public event Action<Exception> ConnectionError;

        public JObject MetaData { get; internal set; }

        private ODataCRMConnection connection;
        private RestClient client;
        private NetworkCredential credentials;
        private EntityMetadata[] entities = new EntityMetadata[0];
        public ODataCRMConnection Connection => connection;

        public ODataCRMProxy(ODataCRMConnection connection)
        {
            this.connection = connection;
        }

        public IEnumerable<EntityMetadata> Entities => entities;

        public bool Connect()
        {
            var t = this.ConnectAsync();
            t.Wait();
            return t.Result;
        }

        private void LoadMetadata(XmlDocument doc)
        {
            List<EntityMetadata> entities = new List<EntityMetadata>();
            foreach (XmlNode childnode in doc.DocumentElement.ChildNodes)
            {
                if (childnode.LocalName.ToLower() == "dataservices")
                {
                    foreach (XmlNode entity in childnode.ChildNodes[0].ChildNodes)
                    {
                        if (entity.LocalName.ToLower() != "entitytype") continue;
                        EntityMetadata metadata = new EntityMetadata();
                        metadata.Name = entity.Attributes["Name"].Value;
                        List<FieldMetadata> fields = new List<FieldMetadata>();
                        foreach (XmlNode field in entity.ChildNodes)
                        {
                            switch (field.LocalName.ToLower())
                            {
                                case "navigationproperty":
                                    {
                                        var f = new FieldMetadata();
                                        f.SchemaName = field.Attributes["Name"].Value;
                                        f.Type = field.Attributes["Type"].Value;

                                        if (f.SchemaName.StartsWith("_") && f.SchemaName.EndsWith("_value"))
                                        {
                                            f.SchemaName = f.SchemaName.Substring(1, f.SchemaName.Length - 9);
                                        }

                                        if (field.ChildNodes.Count == 1 && field.ChildNodes[0].LocalName.ToLower() == "referentialconstraint")
                                        {
                                            var field_lookup = field.ChildNodes[0];
                                            var _fieldname = field_lookup.Attributes["Property"].Value;
                                            if (_fieldname.StartsWith("_") && _fieldname.EndsWith("_value"))
                                            {
                                                f.Name = _fieldname.Substring(1, _fieldname.Length - 9);
                                            }
                                            else
                                            {
                                                f.Name = _fieldname;
                                            }
                                            f.LookupEntityName = field.Attributes["Type"].Value.Replace("mscrm.", "");
                                            f.LookupEntityPrimaryKey = field_lookup.Attributes["ReferencedProperty"].Value;
                                            fields.Add(f);
                                        }
                                        else
                                        {
                                            f.Name = f.SchemaName;
                                        }

                                    }
                                    break;
                            }

                        }
                        if (fields.Count > 0)
                        {
                            metadata.Fields = fields.ToArray();
                            fields.Clear();
                            entities.Add(metadata);
                        }
                    }
                }
            }
            this.entities = entities.ToArray();
            entities.Clear();
        }

        public async Task<bool> ConnectAsync()
        {
            this.client = new RestClient(connection.ServiceUrl);
            this.credentials = new NetworkCredential(connection.Username, connection.Password, connection.Domain);

            try
            {
                var response = await client.ExecuteAsync(new RestRequest("/$metadata#EntityDefinitions/Attributes") { Credentials = this.credentials, Method = Method.GET });
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(response.Content);
                    LoadMetadata(doc);
                    return true;
                }
                else
                {
                    throw new Exception(((int)response.StatusCode).ToString() + " " + response.StatusDescription);
                }
            }
            catch (Exception exception)
            {
                ConnectionError?.Invoke(exception);
                Error?.Invoke(exception);
            }

            return false;
        }
        private async Task<T> ExecuteRequestAsync<T>(string address, Action<RestRequest> request = null, Action<IRestResponse> response = null)
        {
            try
            {
                var req = new RestRequest(address)
                {
                    Method = Method.GET,
                    Credentials = this.credentials,
                    RequestFormat = DataFormat.Json
                };

                if (request != null) request(req);

                int retryCount = 0;
            retry_execute_request:
                var res = await client.ExecuteAsync(req);
                if (response != null) response(res);
                if ((int)res.StatusCode == 0 && retryCount < 10)
                {
                    retryCount++;
                    Thread.Sleep(500);
                    goto retry_execute_request;
                }

                if ((int)res.StatusCode >= 200 && (int)res.StatusCode < 300)
                {
                    return JsonConvert.DeserializeObject<T>(res.Content);
                }
                else
                {
                    Error?.Invoke(new Exception($"Status Code: {res.StatusCode}, {res.StatusDescription}", new Exception(res.Content)));
                    return default(T);
                }
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex);
                return default(T);
            }
        }
        private T ExecuteRequest<T>(string address, Action<RestRequest> request = null, Action<IRestResponse> response = null)
        {
            var t = ExecuteRequestAsync<T>(address, request, response);
            t.Wait();
            return t.Result;
        }
        public async Task<JArray> FetchAsync(string xmlQuery, bool isDisabledConvertion = false)
        {
            try
            {
                string mainEntity = GetMainEntityNameFromFetchXML(xmlQuery);
                mainEntity = GetCollectionName(mainEntity, isDisabledConvertion);

                var result = await ExecuteRequestAsync<JObject>("/" + mainEntity, (q) =>
                {
                    q.AddHeader("Prefer", "odata.include-annotations=\"*\"");
                    q.AddParameter("fetchXml", xmlQuery);
                });
                if (result != null)
                {
                    return result.Value<JArray>("value");
                }
                return new JArray();
            }
            catch (Exception exception)
            {
                Error?.Invoke(exception);
                return new JArray();
            }
        }

        private static string GetCollectionName(string logicalName, bool isDisabledConvertion = false)
        {
            if (isDisabledConvertion) return logicalName;
            return logicalName.GetPlural();
        }

        public JArray Fetch(string xmlQuery, bool isDisabledConvertion = false)
        {
            var t = FetchAsync(xmlQuery, isDisabledConvertion);
            t.Wait();
            return t.Result;
        }
        public async Task<JToken> FetchDirectAsync(string xmlQuery, bool isDisabledConvertion = false)
        {
            try
            {
                string mainEntity = GetMainEntityNameFromFetchXML(xmlQuery);
                mainEntity = GetCollectionName(mainEntity, isDisabledConvertion);

                var result = await ExecuteRequestAsync<JToken>("/" + mainEntity, (q) =>
                {
                    q.AddHeader("Prefer", "odata.include-annotations=\"*\"");
                    q.AddParameter("fetchXml", xmlQuery);
                });
                if (result != null)
                {
                    return result;
                }
                return new JObject();
            }
            catch (Exception exception)
            {
                Error?.Invoke(exception);
                return new JObject();
            }
        }
        public JToken FetchDirect(string xmlQuery, bool isDisabledConvertion = false)
        {
            var t = FetchDirectAsync(xmlQuery, isDisabledConvertion);
            t.Wait();
            return t.Result;
        }
        private static string GetMainEntityNameFromFetchXML(string xmlQuery)
        {
            var lcaseQuery = xmlQuery.ToLower();
            var entityIndex = lcaseQuery.IndexOf("<entity");
            var entityNameIndex = lcaseQuery.IndexOf("name=", entityIndex);
            var entityNameStartChar = lcaseQuery[entityNameIndex + 5];
            var entityNameEndIndex = lcaseQuery.IndexOf(entityNameStartChar, entityNameIndex + 6);
            string mainEntity = xmlQuery.Substring(entityNameIndex + 6, entityNameEndIndex - entityNameIndex - 6);
            return mainEntity;
        }

        public Guid Create(Entity entity, bool isDisabledConvertion = false)
        {
            var t = CreateAsync(entity, isDisabledConvertion);
            t.Wait();
            return t.Result;
        }
        public async Task<Guid> CreateAsync(Entity entity, bool isDisabledConvertion = false)
        {
            string mainEntity = entity.LogicalName;
            mainEntity = GetCollectionName(mainEntity, isDisabledConvertion);
            bool isSuccess = false;
            await ExecuteRequestAsync<JObject>("/" + mainEntity, (req) =>
            {
                req.Method = Method.POST;
                var jObj = new JObject();
                foreach (var attr in entity.Attributes)
                {
                    UnBoxEntityToJson(jObj, attr, entity.LogicalName, isDisabledConvertion);
                }
                req.AddParameter("application/json", JsonConvert.SerializeObject(jObj), ParameterType.RequestBody);
            }, (res) =>
            {
                if ((int)res.StatusCode >= 200 && (int)res.StatusCode < 300)
                {
                    string v = res.Headers.Where(p => Localization.LowercaseEN(p.Name) == "odata-entityid").FirstOrDefault().Value?.ToString();
                    v = "{" + v.Split('(').LastOrDefault().Split(')').FirstOrDefault().ToUpper() + "}";
                    entity.Id = Guid.Parse(v);
                    isSuccess = true;
                }
            });

            return isSuccess ? entity.Id.Id : Guid.Empty;
        }

        public async Task<bool> AssociateAsync(EntityReference source, EntityReference dest, string relationShipName, bool isDisabledConvertion = false)
        {
            bool isSuccess = false;
            await ExecuteRequestAsync<JObject>("/" + (GetCollectionName(source.LogicalName, isDisabledConvertion)) + $"({source.Id.ToString().Replace("{", "").Replace("}", "").ToLower()})/{relationShipName}/$ref", (req) =>
               {
                   req.Method = Method.POST;
                   var jObj = new JObject();
                   jObj.Add("@odata.id", JToken.FromObject($"{client.BaseUrl.ToString()}/{GetCollectionName(dest.LogicalName, isDisabledConvertion)}({dest.Id.ToString().Replace("{", "").Replace("}", "").ToLower()})"));
                   req.AddParameter("application/json", JsonConvert.SerializeObject(jObj), ParameterType.RequestBody);
               }, (res) =>
               {
                   if ((int)res.StatusCode >= 200 && (int)res.StatusCode < 300)
                   {
                       isSuccess = true;
                   }
               });

            return isSuccess;
        }

        public async Task<bool> UpdateAsync(Entity entity, bool isDisabledConvertion = false)
        {
            string mainEntity = entity.LogicalName;
            mainEntity = GetCollectionName(mainEntity, isDisabledConvertion);
            bool isSuccess = false;
            await ExecuteRequestAsync<JObject>("/" + mainEntity + $"({entity.Id.Id.ToString().Replace("{", "").Replace("}", "").ToLower()})", (req) =>
              {
                  req.Method = Method.PATCH;
                  var jObj = new JObject();
                  foreach (var attr in entity.Attributes)
                  {
                      UnBoxEntityToJson(jObj, attr, entity.LogicalName, isDisabledConvertion);
                  }
                  req.AddParameter("application/json", JsonConvert.SerializeObject(jObj), ParameterType.RequestBody);
              }, (res) =>
              {
                  if ((int)res.StatusCode >= 200 && (int)res.StatusCode < 300)
                  {
                      isSuccess = true;
                  }
              });

            return isSuccess;
        }
        public bool Update(Entity entity, bool isDisabledConvertion = false)
        {
            var t = UpdateAsync(entity, isDisabledConvertion);
            t.Wait();
            return t.Result;
        }

        private void UnBoxEntityToJson(JObject jObj, KeyValuePair<string, object> attr, string logicalName, bool isDisabledConvertion = false)
        {
            if (attr.Value is CRMEntity)
            {
                attr = new KeyValuePair<string, object>(attr.Key, (EntityReference)(attr.Value as CRMEntity).EntityReference);
            }

            if (attr.Value is EntityReference)
            {
                EntityReference entityRef = ((EntityReference)attr.Value);
                string lookupEntityName = entityRef.LogicalName;
                logicalName = Localization.LowercaseEN(logicalName);
                lookupEntityName = Localization.LowercaseEN(lookupEntityName);
                string lookupEntityPrimaryKey = lookupEntityName + "id";
                var fields = this.entities.Where(p => Localization.LowercaseEN(p.Name) == logicalName).FirstOrDefault()?.Fields?.Where(p => Localization.LowercaseEN(p.LookupEntityName) == lookupEntityName && Localization.LowercaseEN(p.LookupEntityPrimaryKey) == lookupEntityPrimaryKey).ToArray();
                var _targetField = fields.FirstOrDefault();
                if (fields.Length > 1)
                {
                    var attrNameWithoutId = attr.Key.EndsWith("id") ? attr.Key.Substring(0, attr.Key.Length - 2) : attr.Key;
                    _targetField = fields.Where(p => p.Name == attrNameWithoutId).FirstOrDefault();
                }
                string _fieldname = attr.Key;
                if (_targetField != null)
                {
                    _fieldname = _targetField.SchemaName;
                }
                if (string.IsNullOrWhiteSpace(entityRef.LogicalName) || entityRef.Id == Guid.Empty)
                {
                    jObj.Add($"{_fieldname}@odata.bind", null);
                }
                else
                {
                    jObj.Add($"{_fieldname}@odata.bind", JValue.FromObject($"/{GetCollectionName(lookupEntityName, isDisabledConvertion)}({ (((EntityReference)attr.Value).Id).ToString().Replace("{", "").Replace("}", "") })"));
                }
            }
            else
            {
                jObj.Add(attr.Key, TypeConverter.ConvertToJson(attr.Value));
            }
        }

        public EntityCollection RetrieveMultiple(string fetchXML, bool isDisabledConvertion = false)
        {
            var _result = this.FetchDirect(fetchXML, isDisabledConvertion);
            return RetrieveMultipleInternal(fetchXML, _result);
        }
        public async Task<EntityCollection> RetrieveMultipleAsync(string fetchXML, bool isDisabledConvertion = false)
        {
            var _result = await this.FetchDirectAsync(fetchXML, isDisabledConvertion);
            return RetrieveMultipleInternal(fetchXML, _result);
        }
        private EntityCollection RetrieveMultipleInternal(string fetchXML, JToken _result)
        {
            var items = _result is JArray ? (JArray)_result : (JArray)((JObject)_result)["value"];
            var entityName = GetMainEntityNameFromFetchXML(fetchXML);
            try
            {
                EntityCollection collection = new EntityCollection();
                if (!(_result is JArray))
                {
                    var resultObj = (JObject)_result;
                    if (int.TryParse(((JValue)resultObj["@Microsoft.Dynamics.CRM.totalrecordcount"])?.Value?.ToString() ?? "0", out int v_totalrecord))
                    {
                        collection.TotalRecordCount = v_totalrecord;
                    }
                    if ((((JValue)resultObj["@Microsoft.Dynamics.CRM.totalrecordcountlimitexceeded"])?.Value ?? false).Equals(true))
                    {
                        collection.TotalRecordCountLimitExceeded = true;
                        collection.MoreRecords = true;
                    }
                    else
                    {
                        collection.MoreRecords = false;
                        collection.TotalRecordCountLimitExceeded = false;
                    }
                    try
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(fetchXML);
                        var _fetch = doc.SelectSingleNode("fetch");
                        if (int.TryParse(_fetch.Attributes["page"]?.Value, out int _page))
                        {
                            collection.Page = _page;
                        }
                    }
                    catch { }
                }
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        Entity entity = new Entity();
                        entity.Id = Guid.Parse("{" + ((JValue)item[entityName + "id"]).Value.ToString() + "}");
                        entity.LogicalName = entityName;
                        GetEntityFields(item, entity);
                        collection.Add(entity);
                    }
                }
                return collection;
            }
            catch (Exception exception)
            {
                Error?.Invoke(exception);
                return new EntityCollection();
            }
        }

        private static void GetEntityFields(JToken item, Entity entity)
        {
            foreach (var prop in (JObject)item)
            {
                if (prop.Key.Contains("@")) continue;
                var propvalue = ((JValue)prop.Value).Value;
                if (prop.Key.StartsWith("_") && prop.Key.EndsWith("_value"))
                {
                    var propname = prop.Key.Substring(1, prop.Key.Length - 7);
                    var entityname = ((JObject)item)[prop.Key + "@Microsoft.Dynamics.CRM.lookuplogicalname"].ToString();

                    entity.SetAttribute(propname, new EntityReference(entityname, Guid.Parse("{" + propvalue.ToString().ToUpper() + "}")));
                }
                else
                {
                    if (((JObject)item).ContainsKey(prop.Key + "_base") && ((JObject)item).ContainsKey(prop.Key + "@OData.Community.Display.V1.FormattedValue") && ((JObject)item).ContainsKey(prop.Key + "_base@OData.Community.Display.V1.FormattedValue"))
                    {
                        propvalue = new Money(Convert.ToDecimal(propvalue));
                    }


                    entity.SetAttribute(prop.Key, propvalue);
                }
            }
        }

        public EntityCollection RetrieveMultiple(FetchWrapper wrapper, bool isDisabledConvertion = false)
        {
            string xml = wrapper.ConvertToFetchXml();
            return RetrieveMultiple(xml, isDisabledConvertion);
        }
        public async Task<EntityCollection> RetrieveMultipleAsync(FetchWrapper wrapper, bool isDisabledConvertion = false)
        {
            string xml = wrapper.ConvertToFetchXml();
            return await RetrieveMultipleAsync(xml, isDisabledConvertion);
        }
        public bool SetState(EntityReference entityReference, int statecode, int statuscode, bool isDisabledConvertion = false)
        {
            try
            {
                string mainEntity = entityReference.LogicalName;
                mainEntity = GetCollectionName(mainEntity, isDisabledConvertion);

                var result = ExecuteRequest<JToken>("/" + mainEntity + $"({entityReference.Id.ToString().Replace("{", "").Replace("}", "").ToLower()})");
                return true;
            }
            catch (Exception exception)
            {
                Error?.Invoke(exception);
                return false;
            }
        }
        public async Task<bool> SetStateAsync(EntityReference entityReference, int statecode, int statuscode, bool isDisabledConvertion = false)
        {
            try
            {
                string mainEntity = entityReference.LogicalName;
                mainEntity = GetCollectionName(mainEntity, isDisabledConvertion);

                var result = await ExecuteRequestAsync<JToken>("/" + mainEntity + $"({entityReference.Id.ToString().Replace("{", "").Replace("}", "").ToLower()})");
                return true;
            }
            catch (Exception exception)
            {
                Error?.Invoke(exception);
                return false;
            }
        }

        public void Dispose()
        {
            this.entities = null;
        }
    }
}
