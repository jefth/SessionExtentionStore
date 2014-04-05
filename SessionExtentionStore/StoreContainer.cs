using System.Web.Configuration;
using ServiceStack.Redis;
using ServiceStack.Text;
using System;
using System.Collections;

namespace SessionExtentionStore
{

    public class StoreContainer
    {
        private readonly IRedisClientsManager _clientsManager = RedisConn.ClientManager;

        static readonly Hashtable SImmutableTypes;

        static StoreContainer()
        {
            Type t;
            SImmutableTypes = new Hashtable(19);

            t = typeof(String);
            SImmutableTypes.Add(t, t);
            t = typeof(Int32);
            SImmutableTypes.Add(t, t);
            t = typeof(Boolean);
            SImmutableTypes.Add(t, t);
            t = typeof(DateTime);
            SImmutableTypes.Add(t, t);
            t = typeof(Decimal);
            SImmutableTypes.Add(t, t);
            t = typeof(Byte);
            SImmutableTypes.Add(t, t);
            t = typeof(Char);
            SImmutableTypes.Add(t, t);
            t = typeof(Single);
            SImmutableTypes.Add(t, t);
            t = typeof(Double);
            SImmutableTypes.Add(t, t);
            t = typeof(SByte);
            SImmutableTypes.Add(t, t);
            t = typeof(Int16);
            SImmutableTypes.Add(t, t);
            t = typeof(Int64);
            SImmutableTypes.Add(t, t);
            t = typeof(UInt16);
            SImmutableTypes.Add(t, t);
            t = typeof(UInt32);
            SImmutableTypes.Add(t, t);
            t = typeof(UInt64);
            SImmutableTypes.Add(t, t);
            t = typeof(TimeSpan);
            SImmutableTypes.Add(t, t);
            t = typeof(Guid);
            SImmutableTypes.Add(t, t);
            t = typeof(IntPtr);
            SImmutableTypes.Add(t, t);
            t = typeof(UIntPtr);
            SImmutableTypes.Add(t, t);
        }

        private static bool IsImmutable(Type t)
        {
            return SImmutableTypes[t] != null;
        }
        /// <summary>
        /// 用SessionID来作为用户唯一性表示
        /// </summary>
        private readonly string _name;
        public StoreContainer(string sessionId)
        {
            _name = "ExtentionStore/" + sessionId;
        }

        /// <summary>
        /// 绝对过期时间会转换为相对过期时间
        /// </summary>
        public DateTime ExpiresAt
        {
            set { _expiresIn = value - DateTime.Now; }
        }

        /// <summary>
        /// 相对过期时间
        /// </summary>
        private static readonly SessionStateSection SessionConfit =(SessionStateSection)WebConfigurationManager.GetSection("system.web/sessionState");
        private TimeSpan _expiresIn = SessionConfit.Timeout;
        public TimeSpan ExpiresIn
        {
            set { _expiresIn = value; }
        }

        private IRedisClient GetClientAndWatch(string key)
        {
            var client = _clientsManager.GetClient();

            client.Watch(key);
            return client;
        }

        /// <summary>
        /// 向Redis内写入值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">hashMap的key</param>
        /// <param name="o">存入对象</param>
        private void Set<T>(string key, T o)
        {
            using (var client = this.GetClientAndWatch(key))
            {
                using (var t = client.CreateTransaction())
                {
                    var typeName = o.GetType().AssemblyQualifiedName;
                    t.QueueCommand(c => c.SetEntryInHash(_name, key + "Type", typeName));
                    if (!IsImmutable(o.GetType()))
                    {
                        t.QueueCommand(c => c.SetEntryInHash(_name, key + "Data", o.ToJson()));
                    }
                    else
                    {
                        t.QueueCommand(c => c.SetEntryInHash(_name, key + "Data", o.ToString()));
                    }
                    t.QueueCommand(c => c.ExpireEntryIn(_name, _expiresIn));
                    t.Commit();
                }
            }
        }

        /// <summary>
        /// 从Redis内获取值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private object Get(string key)
        {
            using (IRedisClient client = _clientsManager.GetClient())
            {
                object o = null;
                var typeName = client.GetValueFromHash(_name, key + "Type");
                var valueJson = client.GetValueFromHash(_name, key + "Data");

                if (typeName == null || valueJson == null)
                {
                    throw new NullReferenceException();
                }

                var type = Type.GetType(typeName, true);

                if (!IsImmutable(type))
                {
                    o = ServiceStack.Text.JsonSerializer.DeserializeFromString(valueJson, type);
                }
                else
                {
                    o = Convert.ChangeType(valueJson, type);
                }
                client.ExpireEntryIn(_name, _expiresIn);
                return o;
            }
        }

        public object this[string key]
        {
            get { return Get(key); }
            set { Set(key, value); }
        }

        /// <summary>
        /// 移除某项
        /// </summary>
        /// <param name="key"></param>
        public void Remove(string key)
        {
            using (var client = this.GetClientAndWatch(key))
            {
                using (var t = client.CreateTransaction())
                {
                    t.QueueCommand(c => c.RemoveEntryFromHash(_name, key + "Data"));
                    t.QueueCommand(c => c.RemoveEntryFromHash(_name, key + "Type"));
                    t.Commit();
                }
            }
        }

        /// <summary>
        /// 清空Store
        /// </summary>
        public void Clear()
        {
            using (var client = this.GetClientAndWatch(_name))
            {
                using (var t = client.CreateTransaction())
                {

                    t.QueueCommand(c => c.Remove(_name));
                    t.Commit();
                }
            }
        }

        /// <summary>
        /// 获取数据Json
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetJson(string key)
        {
            using (IRedisClient client = _clientsManager.GetClient())
            {
                return client.GetValueFromHash(_name, key + "Data");
            }
        }

        /// <summary>
        /// 获取存储的值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T GetValue<T>(string key)
        {
            using (IRedisClient client = _clientsManager.GetClient())
            {
                T o = default(T);
                var valueJson = client.GetValueFromHash(_name, key + "Data");

                if (valueJson == null)
                {
                    throw new NullReferenceException();
                }

                if (!IsImmutable(typeof (T)))
                {
                    o = JsonSerializer.DeserializeFromString<T>(valueJson);
                }
                else
                {
                    o = (T) Convert.ChangeType(valueJson, typeof (T));
                    try
                    {
                        o = (T)Convert.ChangeType(valueJson, typeof(T));
                    }
                    catch (Exception)
                    {
                        o = default(T);
                    }
                }

                client.ExpireEntryIn(_name, _expiresIn);
                return o;
            }
        }
    }
}