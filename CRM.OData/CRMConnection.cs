using CRM.OData;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CRM.OData
{
    public class CRMConnection : IDisposable
    {
        internal CRMManager Manager { get; set; }
        public bool UseHTTPs { get; set; } = false;
        public ODataCRMProxy Proxy { get; internal set; }
        public string ServiceUrl { get; private set; } = null;
        public string Username { get; private set; } = null;
        public string Password { get; private set; } = null;
        public string Domain { get; private set; } = null;
        public bool IsConnected { get; internal set; }

        internal bool Connect(CRMManager manager)
        {
            ConnectInternal(manager);
            try
            {
                return IsConnected = Proxy.Connect();
            }
            catch (Exception)
            {
                return false;
            }
        }
        internal async Task<bool> ConnectAsync(CRMManager manager)
        {
            ConnectInternal(manager);
            try
            {
                return IsConnected = await Proxy.ConnectAsync();
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void ConnectInternal(CRMManager manager)
        {
            this.Manager = manager;

            if (Proxy == null)
            {

                Proxy = new ODataCRMProxy(new ODataCRMConnection()
                {
                    ServiceUrl = this.ServiceUrl,
                    Username = this.Username,
                    Password = this.Password,
                    Domain = this.Domain
                });

                Proxy.ConnectionError += this.Manager.OnConnectionError;
                Proxy.Error += this.Manager.OnError;

            }
        }

        public void Dispose()
        {
            this.Proxy.Dispose();
        }
    }

}