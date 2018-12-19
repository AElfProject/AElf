using System;
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
}