using Dynamics365.OData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamics365.OData.Layout
{
    public class CRMEntityCollection<T> where T : CRMEntity
    {
        #region Construction
        internal CRMEntityCollection() { }
        #endregion
        #region Fields
        public IEnumerable<T> Entities { get; internal set; }
        public bool MoreRecords { get; internal set; }
        public int TotalRecordCount { get; internal set; }
        public bool TotalRecordCountLimitExceeded { get; internal set; }
        public bool HasRecords => Entities != null && Entities.Count() > 0;
        public T First => Entities.FirstOrDefault();
        public int EntityCount => Entities != null ? (Entities.Count()) : 0;
        public int? Page { get; set; }
        #endregion
        #region Query
        internal FetchWrapper _query;
        internal CRMManager _manager;
        #endregion
        #region Methods
        public bool PreviousPage()
        {
            return SwitchPage((this.Page ?? 1) - 1);
        }
        public bool NextPage()
        {
            return SwitchPage((this.Page ?? 1) + 1);
        }
        private bool SwitchPage(int PageIndex)
        {
            if (_query != null && _manager != null)
            {
                if (PageIndex > 0)
                {
                    this.Page = PageIndex;
                    var exp = _query;
                    exp.FetchExpression.page = PageIndex.ToString();
                    var result = _manager.RetrieveMultiple<T>(exp);
                    this.Entities = result.Entities;
                    this.MoreRecords = result.MoreRecords;
                    this.TotalRecordCount = result.TotalRecordCount;
                    this.TotalRecordCountLimitExceeded = result.TotalRecordCountLimitExceeded;
                    if (this.EntityCount == 0) return false;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        public IEnumerable<T> GetAll()
        {
            List<T> result = new List<T>();
            do
            {
                result.AddRange(Entities);
                if (EntityCount == 0) break;
            } while (NextPage());
            return result;
        }

        public async Task<bool> PreviousPageAsync()
        {
            return await SwitchPageAsync((this.Page ?? 1) - 1);
        }
        public async Task<bool> NextPageAsync()
        {
            return await SwitchPageAsync((this.Page ?? 1) + 1);
        }
        private async Task<bool> SwitchPageAsync(int PageIndex)
        {
            if (_query != null && _manager != null)
            {
                if (PageIndex > 0)
                {
                    this.Page = PageIndex;
                    var exp = _query;
                    exp.FetchExpression.page = PageIndex.ToString();
                    var result = await _manager.RetrieveMultipleAsync<T>(exp);
                    this.Entities = result.Entities;
                    this.MoreRecords = result.MoreRecords;
                    this.TotalRecordCount = result.TotalRecordCount;
                    this.TotalRecordCountLimitExceeded = result.TotalRecordCountLimitExceeded;
                    if (this.EntityCount == 0) return false;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            List<T> result = new List<T>();
            do
            {
                result.AddRange(Entities);
                if (EntityCount == 0) break;
            } while (await NextPageAsync());
            return result;
        }
        #endregion
    }
}
