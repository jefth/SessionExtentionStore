using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Redis;
using System.Web;

namespace SessionExtentionStore
{
    class UpdateTTL : IHttpModule
    {
        private readonly IRedisClientsManager _clientManager = RedisConn.ClientManager;
        public void Dispose() { }

        public void Init(HttpApplication application)
        {
            application.BeginRequest += ResetItemTimeout;
        }

        private void ResetItemTimeout(Object source, EventArgs e)
        {
            var application = (HttpApplication)source;
            var name = application.Context.Session.SessionID;
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }
            var span = new TimeSpan(0, 0, 20, 0);

            using (var client = this.GetClientAndWatch(name))
            {
                using (var transaction = client.CreateTransaction())
                {
                    transaction.QueueCommand(c => c.ExpireEntryIn(name, span));
                    transaction.Commit();
                }
            }
        }

        private IRedisClient GetClientAndWatch(string key)
        {
            var client = _clientManager.GetClient();

            client.Watch(key);
            return client;
        }
    }
}
