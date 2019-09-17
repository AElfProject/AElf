using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AElf.Database.RedisProtocol
{
    /**
    * Simplified NServiceKit.Redis
    */
    public static class Commands
    {
        public static readonly byte[] Quit = "QUIT".ToUtf8Bytes();
        public static readonly byte[] Auth = "AUTH".ToUtf8Bytes();
        public static readonly byte[] Exists = "EXISTS".ToUtf8Bytes();
        public static readonly byte[] Del = "DEL".ToUtf8Bytes();
        public static readonly byte[] Select = "SELECT".ToUtf8Bytes();
        public static readonly byte[] Ping = "PING".ToUtf8Bytes();

        public static readonly byte[] Set = "SET".ToUtf8Bytes();
        public static readonly byte[] Get = "GET".ToUtf8Bytes();
        public static readonly byte[] MGet = "MGET".ToUtf8Bytes();
        public static readonly byte[] MSet = "MSET".ToUtf8Bytes();
    }

    public static class RedisExtensions
    {
        public static byte[][] ToMultiByteArray(this string[] args)
        {
            var byteArgs = new byte[args.Length][];
            for (var i = 0; i < args.Length; ++i)
                byteArgs[i] = args[i].ToUtf8Bytes();
            return byteArgs;
        }

        public static bool IsConnected(this Socket socket)
        {
            try
            {
                return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (SocketException)
            {
                return false;
            }
        }

        public static string FromUtf8Bytes(this byte[] bytes)
        {
            return bytes == null ? null : Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        public static byte[] ToUtf8Bytes(this string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }

        public static byte[] ToUtf8Bytes(this int intVal)
        {
            return FastToUtf8Bytes(intVal.ToString());
        }

        public static byte[] ToUtf8Bytes(this long longVal)
        {
            return FastToUtf8Bytes(longVal.ToString());
        }

        public static byte[] ToUtf8Bytes(this ulong ulongVal)
        {
            return FastToUtf8Bytes(ulongVal.ToString());
        }

        /// <summary>
        /// Skip the encoding process for 'safe strings' 
        /// </summary>
        /// <param name="strVal"></param>
        /// <returns></returns>
        private static byte[] FastToUtf8Bytes(string strVal)
        {
            var bytes = new byte[strVal.Length];
            for (var i = 0; i < strVal.Length; i++)
                bytes[i] = (byte) strVal[i];

            return bytes;
        }
    
        public static List<RedisEndpoint> ToRedisEndPoints(this IEnumerable<string> hosts)
        {
            return hosts == null
                ? new List<RedisEndpoint>()
                : hosts.Select(x => ToRedisEndpoint(x)).ToList();
        }

        
        public static string [] SplitOnFirst (this string strVal, char needle)
        {
            if (strVal == null) return EmptyStringArray;
            var pos = strVal.IndexOf (needle);
            return pos == -1
                ? new [] { strVal }
                : new [] { strVal.Substring (0, pos), strVal.Substring (pos + 1) };
        }


        public static string [] SplitOnFirst (this string strVal, string needle)
        {
            if (strVal == null) return EmptyStringArray;
            var pos = strVal.IndexOf (needle, StringComparison.OrdinalIgnoreCase);
            return pos == -1
                ? new [] { strVal }
                : new [] { strVal.Substring (0, pos), strVal.Substring (pos + needle.Length) };
        }
        public static string [] SplitOnLast (this string strVal, char needle)
        {
            if (strVal == null) return EmptyStringArray;
            var pos = strVal.LastIndexOf (needle);
            return pos == -1
                ? new [] { strVal }
                : new [] { strVal.Substring (0, pos), strVal.Substring (pos + 1) };
        }

        public static readonly string [] EmptyStringArray = new string [0];

        public static string [] SplitOnLast (this string strVal, string needle)
        {
            if (strVal == null) return EmptyStringArray;
            var pos = strVal.LastIndexOf (needle, StringComparison.OrdinalIgnoreCase);
            return pos == -1
                ? new [] { strVal }
                : new [] { strVal.Substring (0, pos), strVal.Substring (pos + needle.Length) };
        }
        
        public static RedisEndpoint ToRedisEndpoint(this string connectionString, int? defaultPort = null)
        {
            HandleConnectionString(ref connectionString);

            var domainParts = connectionString.SplitOnLast('@');
            var qsParts = domainParts.Last().SplitOnFirst('?');
            var hostParts = qsParts[0].SplitOnLast(':');
            var useDefaultPort = true;
            var port = defaultPort.GetValueOrDefault(RedisConfig.DefaultPort);
            if (hostParts.Length > 1)
            {
                port = int.Parse(hostParts[1]);
                useDefaultPort = false;
            }

            var endpoint = new RedisEndpoint(hostParts[0], port);
            if (domainParts.Length > 1)
            {
                var authParts = domainParts[0].SplitOnFirst(':');
                if (authParts.Length > 1)
                    endpoint.Client = authParts[0];

                endpoint.Password = authParts.Last();
            }

            if (qsParts.Length > 1)
            {
                var qsParams = qsParts[1].Split('&');
                foreach (var param in qsParams)
                {
                    var entry = param.Split('=');
                    var value = entry.Length > 1 ? WebUtility.UrlDecode( entry[1] ) : null;
                    if (value == null) continue;

                    var name = entry[0].ToLower();
                    HandleEndpointByName(endpoint, useDefaultPort, name, value);
                }
            }

            return endpoint;
        }

        private static void HandleConnectionString(ref string connectionString)
        {
            if (connectionString == null)
                throw new ArgumentNullException("connectionString");
            if (connectionString.StartsWith("redis://"))
                connectionString = connectionString.Substring("redis://".Length);
        }
        
        private static void HandleEndpointByName(RedisEndpoint endpoint, bool useDefaultPort, string name, string value)
        {
            switch (name)
            {
                case "db":
                    endpoint.Db = int.Parse(value);
                    break;
                case "ssl":
                    endpoint.Ssl = bool.Parse(value);
                    if (useDefaultPort)
                        endpoint.Port = RedisConfig.DefaultPortSsl;
                    break;
                case "client":
                    endpoint.Client = value;
                    break;
                case "password":
                    endpoint.Password = value;
                    break;
                case "namespaceprefix":
                    endpoint.NamespacePrefix = value;
                    break;
                case "connecttimeout":
                    endpoint.ConnectTimeout = int.Parse(value);
                    break;
                case "sendtimeout":
                    endpoint.SendTimeout = int.Parse(value);
                    break;
                case "receivetimeout":
                    endpoint.ReceiveTimeout = int.Parse(value);
                    break;
                case "retrytimeout":
                    endpoint.RetryTimeout = int.Parse(value);
                    break;
                case "idletimeout":
                case "idletimeoutsecs":
                    endpoint.IdleTimeOutSecs = int.Parse(value);
                    break;
            }
        }
    }

    internal static class RedisExtensionsInternal
    {
//        public static bool IsConnected(this Socket socket)
//        {
//            try
//            {
//                return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
//            }
//            catch (SocketException)
//            {
//                return false;
//            }
//        }

//
//        public static string[] GetIds(this IHasStringId[] itemsWithId)
//        {
//            var ids = new string[itemsWithId.Length];
//            for (var i = 0; i < itemsWithId.Length; i++)
//            {
//                ids[i] = itemsWithId[i].Id;
//            }
//
//            return ids;
//        }

        public static List<string> ToStringList(this byte[][] multiDataList)
        {
            if (multiDataList == null)
                return new List<string>();

            var results = new List<string>();
            foreach (var multiData in multiDataList)
            {
                results.Add(multiData.FromUtf8Bytes());
            }

            return results;
        }
//
//        public static string[] ToStringArray(this byte[][] multiDataList)
//        {
//            if (multiDataList == null)
//                return TypeConstants.EmptyStringArray;
//
//            var to = new string[multiDataList.Length];
//            for (int i = 0; i < multiDataList.Length; i++)
//            {
//                to[i] = multiDataList[i].FromUtf8Bytes();
//            }
//
//            return to;
//        }
//
//        public static Dictionary<string, string> ToStringDictionary(this byte[][] multiDataList)
//        {
//            if (multiDataList == null)
//                return TypeConstants.EmptyStringDictionary;
//
//            var map = new Dictionary<string, string>();
//
//            for (var i = 0; i < multiDataList.Length; i += 2)
//            {
//                var key = multiDataList[i].FromUtf8Bytes();
//                map[key] = multiDataList[i + 1].FromUtf8Bytes();
//            }
//
//            return map;
//        }

        private static readonly NumberFormatInfo DoubleFormatProvider = new NumberFormatInfo
        {
            PositiveInfinitySymbol = "+inf",
            NegativeInfinitySymbol = "-inf"
        };

        public static byte[] ToFastUtf8Bytes(this double value)
        {
            return FastToUtf8Bytes(value.ToString("R", DoubleFormatProvider));
        }

        private static byte[] FastToUtf8Bytes(string strVal)
        {
            var bytes = new byte[strVal.Length];
            for (var i = 0; i < strVal.Length; i++)
                bytes[i] = (byte) strVal[i];

            return bytes;
        }
//
//        public static byte[][] ToMultiByteArray(this string[] args)
//        {
//            var byteArgs = new byte[args.Length][];
//            for (var i = 0; i < args.Length; ++i)
//                byteArgs[i] = args[i].ToUtf8Bytes();
//            return byteArgs;
//        }

        public static byte[][] PrependByteArray(this byte[][] args, byte[] valueToPrepend)
        {
            var newArgs = new byte[args.Length + 1][];
            newArgs[0] = valueToPrepend;
            var i = 1;
            foreach (var arg in args)
                newArgs[i++] = arg;

            return newArgs;
        }

        public static byte[][] PrependInt(this byte[][] args, int valueToPrepend)
        {
            return args.PrependByteArray(valueToPrepend.ToUtf8Bytes());
        }
    }

    public class RedisException : Exception
    {
        public RedisException(string message) : base(message)
        {
        }

        public RedisException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class RedisResponseException : RedisException
    {
        public RedisResponseException(string message) : base(message)
        {
        }

        public RedisResponseException(string message, string code) : base(message)
        {
            Code = code;
        }

        public string Code { get; private set; }
    }

    public class RedisEndpoint 
    {
        public RedisEndpoint()
        {
            Host = RedisConfig.DefaultHost;
            Port = RedisConfig.DefaultPort;
            Db = RedisConfig.DefaultDb;

            ConnectTimeout = RedisConfig.DefaultConnectTimeout;
            SendTimeout = RedisConfig.DefaultSendTimeout;
            ReceiveTimeout = RedisConfig.DefaultReceiveTimeout;
            RetryTimeout = RedisConfig.DefaultRetryTimeout;
            IdleTimeOutSecs = RedisConfig.DefaultIdleTimeOutSecs;
        }

        public RedisEndpoint(string host, int port, string password = null, long db = RedisConfig.DefaultDb)
            : this()
        {
            this.Host = host;
            this.Port = port;
            this.Password = password;
            this.Db = db;
        }

        public string Host { get; set; }
        public int Port { get; set; }
        public bool Ssl { get; set; }
        public int ConnectTimeout { get; set; }
        public int SendTimeout { get; set; }
        public int ReceiveTimeout { get; set; }
        public int RetryTimeout { get; set; }
        public int IdleTimeOutSecs { get; set; }
        public long Db { get; set; }
        public string Client { get; set; }
        public string Password { get; set; }

        public bool RequiresAuth
        {
            get { return !string.IsNullOrEmpty(Password); }
        }

        public string NamespacePrefix { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0}:{1}", Host, Port);

            var args = new List<string>();
            
            if (Client != null)
                args.Add("Client=" + Client);
            if (Password != null)
                args.Add("Password=" + System.Net.WebUtility.UrlEncode(Password));
            if (Db != RedisConfig.DefaultDb)
                args.Add("Db=" + Db);
            if (Ssl)
                args.Add("Ssl=true");
            if (ConnectTimeout != RedisConfig.DefaultConnectTimeout)
                args.Add("ConnectTimeout=" + ConnectTimeout);
            if (SendTimeout != RedisConfig.DefaultSendTimeout)
                args.Add("SendTimeout=" + SendTimeout);
            if (ReceiveTimeout != RedisConfig.DefaultReceiveTimeout)
                args.Add("ReceiveTimeout=" + ReceiveTimeout);
            if (RetryTimeout != RedisConfig.DefaultRetryTimeout)
                args.Add("RetryTimeout=" + RetryTimeout);
            if (IdleTimeOutSecs != RedisConfig.DefaultIdleTimeOutSecs)
                args.Add("IdleTimeOutSecs=" + IdleTimeOutSecs);
            if (NamespacePrefix != null)
                args.Add("NamespacePrefix=" + System.Net.WebUtility.UrlEncode(NamespacePrefix));

            if (args.Count > 0)
                sb.Append("?").Append(string.Join("&", args));

            return sb.ToString();
        }

        protected bool Equals(RedisEndpoint other)
        {
            return string.Equals(Host, other.Host)
                   && Port == other.Port
                   && Ssl.Equals(other.Ssl)
                   && ConnectTimeout == other.ConnectTimeout
                   && SendTimeout == other.SendTimeout
                   && ReceiveTimeout == other.ReceiveTimeout
                   && RetryTimeout == other.RetryTimeout
                   && IdleTimeOutSecs == other.IdleTimeOutSecs
                   && Db == other.Db
                   && string.Equals(Client, other.Client)
                   && string.Equals(Password, other.Password)
                   && string.Equals(NamespacePrefix, other.NamespacePrefix);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RedisEndpoint) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Host != null ? Host.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Port;
                hashCode = (hashCode * 397) ^ Ssl.GetHashCode();
                hashCode = (hashCode * 397) ^ ConnectTimeout;
                hashCode = (hashCode * 397) ^ SendTimeout;
                hashCode = (hashCode * 397) ^ ReceiveTimeout;
                hashCode = (hashCode * 397) ^ RetryTimeout;
                hashCode = (hashCode * 397) ^ IdleTimeOutSecs;
                hashCode = (hashCode * 397) ^ Db.GetHashCode();
                hashCode = (hashCode * 397) ^ (Client != null ? Client.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Password != null ? Password.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (NamespacePrefix != null ? NamespacePrefix.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
    
    public class RedisConfig
    {
        //redis-server defaults:
        public const long DefaultDb = 0;
        public const int DefaultPort = 6379;
        public const int DefaultPortSsl = 6380;
        public const int DefaultPortSentinel = 26379;
        public const string DefaultHost = "localhost";

        /// <summary>
        /// The default RedisClient Socket ConnectTimeout (default -1, None)
        /// </summary>
        public static int DefaultConnectTimeout = -1;

        /// <summary>
        /// The default RedisClient Socket SendTimeout (default -1, None)
        /// </summary>
        public static int DefaultSendTimeout = -1;

        /// <summary>
        /// The default RedisClient Socket ReceiveTimeout (default -1, None)
        /// </summary>
        public static int DefaultReceiveTimeout = -1;

        /// <summary>
        /// Default Idle TimeOut before a connection is considered to be stale (default 240 secs)
        /// </summary>
        public static int DefaultIdleTimeOutSecs = 240;

        /// <summary>
        /// The default RetryTimeout for auto retry of failed operations (default 10,000ms)
        /// </summary>
        public static int DefaultRetryTimeout = 10 * 1000;

        /// <summary>
        /// Default Max Pool Size for Pooled Redis Client Managers (default none)
        /// </summary>
        public static int? DefaultMaxPoolSize;

        /// <summary>
        /// The BackOff multiplier failed Auto Retries starts from (default 10ms)
        /// </summary>
        public static int BackOffMultiplier = 10;

        /// <summary>
        /// The Byte Buffer Size to combine Redis Operations within (1450 bytes)
        /// </summary>
        public static int BufferLength => 1450;

        /// <summary>
        /// The Byte Buffer Size for Operations to use a byte buffer pool (default 500kb)
        /// </summary>
        public static int BufferPoolMaxSize = 500000;

        /// <summary>
        /// Whether Connections to Master hosts should be verified they're still master instances (default true)
        /// </summary>
        public static bool VerifyMasterConnections = true;

        /// <summary>
        /// The ConnectTimeout on clients used to find the next available host (default 200ms)
        /// </summary>
        public static int HostLookupTimeoutMs = 200;

        /// <summary>
        /// Skip ServerVersion Checks by specifying Min Version number, e.g: 2.8.12 => 2812, 2.9.1 => 2910
        /// </summary>
        public static int? AssumeServerVersion;

        /// <summary>
        /// How long to hold deactivated clients for before disposing their connection (default 1 min)
        /// Dispose of deactivated Clients immediately with TimeSpan.Zero
        /// </summary>
        public static TimeSpan DeactivatedClientsExpiry = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Whether Debug Logging should log detailed Redis operations (default false)
        /// </summary>
        public static bool DisableVerboseLogging = false;


        /// <summary>
        /// Assert all access using pooled RedisClient instance should be limited to same thread.
        /// Captures StackTrace so is very slow, use only for debugging connection issues.
        /// </summary>
        public static bool AssertAccessOnlyOnSameThread = false;

        /// <summary>
        /// Resets Redis Config and Redis Stats back to default values
        /// </summary>
        public static void Reset()
        {

            DefaultConnectTimeout = -1;
            DefaultSendTimeout = -1;
            DefaultReceiveTimeout = -1;
            DefaultRetryTimeout = 10 * 1000;
            DefaultIdleTimeOutSecs = 240;
            DefaultMaxPoolSize = null;
            BackOffMultiplier = 10;
            BufferPoolMaxSize = 500000;
            VerifyMasterConnections = true;
            HostLookupTimeoutMs = 200;
            AssumeServerVersion = null;
            DeactivatedClientsExpiry = TimeSpan.FromMinutes(1);
            DisableVerboseLogging = false;
            AssertAccessOnlyOnSameThread = false;
        }
    }
}