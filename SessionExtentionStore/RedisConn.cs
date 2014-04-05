using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ServiceStack.Redis;

namespace SessionExtentionStore
{
    internal static class RedisConn
    {
        private static readonly string RedisIp = System.Configuration.ConfigurationManager.AppSettings["SessionExtention"];
        internal static readonly IRedisClientsManager ClientManager = new BasicRedisClientManager(RedisIp);
    }
}
