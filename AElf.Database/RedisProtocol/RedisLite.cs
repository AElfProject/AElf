using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace AElf.Database.RedisProtocol
{
    /**
     * Simplified NServiceKit.Redis
     */
    public class RedisLite: IDisposable
    {
        public const long DefaultDb = 0;
        public const int DefaultPort = 6379;
        public const string DefaultHost = "localhost";
        public const int DefaultIdleTimeOutSecs = 240; //default on redis is 300

        internal const int Success = 1;
        internal const int OneGb = 1073741824;
        private readonly byte[] endData = new[] {(byte) '\r', (byte) '\n'};

        private int clientPort;
        private string lastCommand;
        private SocketException lastSocketException;
        public bool HadExceptions { get; protected set; }

        protected Socket socket;
        protected BufferedStream Bstream;

        /// <summary>
        /// Used to manage connection pooling
        /// </summary>
        internal bool Active { get; set; }

        internal long LastConnectedAtTimestamp;

        public long Id { get; set; }

        public string Host { get; private set; }
        public int Port { get; private set; }

        /// <summary>
        /// Gets or sets object key prefix.
        /// </summary>
        public string NamespacePrefix { get; set; }

        public int ConnectTimeout { get; set; }
        public int RetryTimeout { get; set; }
        public int RetryCount { get; set; }
        public int SendTimeout { get; set; }
        public int ReceiveTimeout { get; set; }
        public string Password { get; set; }
        public int IdleTimeOutSecs { get; set; }

        public RedisLite(string host, int port = 6379): this(host, port, null)
        {
        }

        public RedisLite(string host, int port, string password = null, long db = DefaultDb)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Port = port;
            SendTimeout = -1;
            ReceiveTimeout = -1;
            Password = password;
            Db = db;
            IdleTimeOutSecs = DefaultIdleTimeOutSecs;
        }

        private long _db;
        public long Db
        {
            get => _db;
            set => _db = value;
        }

        public bool Ping()
        {
            return SendExpectCode(Commands.Ping) == "PONG";
        }

        public void Quit()
        {
            SendCommand(Commands.Quit);
        }

        public long Exists(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return SendExpectLong(Commands.Exists, key.ToUtf8Bytes());
        }

        public void Set(string key, byte[] value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            value = value ?? new byte[0];

            if (value.Length > OneGb)
                throw new ArgumentException("value exceeds 1G", nameof(value));

            SendExpectSuccess(Commands.Set, key.ToUtf8Bytes(), value);
        }

        public void MSet(byte[][] keys, byte[][] values)
        {
            var keysAndValues = MergeCommandWithKeysAndValues(Commands.MSet, keys, values);

            SendExpectSuccess(keysAndValues);
        }

        public byte[] Get(string key)
        {
            return GetBytes(key);
        }

        public byte[] GetBytes(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return SendExpectData(Commands.Get, key.ToUtf8Bytes());
        }

        public byte[][] MGet(params byte[][] keys)
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys));
            if (keys.Length == 0)
                throw new ArgumentException("keys");

            var cmdWithArgs = MergeCommandWithArgs(Commands.MGet, keys);

            return SendExpectMultiData(cmdWithArgs);
        }

        public long Del(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return SendExpectLong(Commands.Del, key.ToUtf8Bytes());
        }

        public long Del(params string[] keys)
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys));

            var cmdWithArgs = MergeCommandWithArgs(Commands.Del, keys);
            return SendExpectLong(cmdWithArgs);
        }

        private void Connect()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                SendTimeout = SendTimeout,
                ReceiveTimeout = ReceiveTimeout
            };
            try
            {
                if (ConnectTimeout == 0)
                {
                    socket.Connect(Host, Port);
                }
                else
                {
                    var connectResult = socket.BeginConnect(Host, Port, null, null);
                    connectResult.AsyncWaitHandle.WaitOne(ConnectTimeout, true);
                }

                if (!socket.Connected)
                {
                    socket.Close();
                    socket = null;
                    HadExceptions = true;
                    return;
                }

                Bstream = new BufferedStream(new NetworkStream(socket), 16 * 1024);

                if (Password != null)
                    SendExpectSuccess(Commands.Auth, Password.ToUtf8Bytes());

                if (_db != 0)
                    SendExpectSuccess(Commands.Select, _db.ToUtf8Bytes());

                clientPort = socket.LocalEndPoint is IPEndPoint ipEndpoint ? ipEndpoint.Port : -1;
                lastCommand = null;
                lastSocketException = null;
                LastConnectedAtTimestamp = Stopwatch.GetTimestamp();
            }
            catch (SocketException ex)
            {
                socket?.Close();
                socket = null;

                HadExceptions = true;
                var throwEx = new Exception("could not connect to redis Instance at " + Host + ":" + Port, ex);
                Log(throwEx.Message, ex);
                throw throwEx;
            }
        }

        private bool IsDisposed { get; set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~RedisLite()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            Active = false;

            if (disposing)
            {
                //dispose un managed resources
                DisposeConnection();
            }
        }

        private void DisposeConnection()
        {
            if (IsDisposed) return;
            IsDisposed = true;

            if (socket == null) return;

            try
            {
                Quit();
            }
            finally
            {
                SafeConnectionClose();
            }
        }

        private bool Reconnect()
        {
            var previousDb = _db;

            SafeConnectionClose();
            Connect(); //sets db to 0

            if (previousDb != DefaultDb) Db = previousDb;

            return socket != null;
        }

        private void SafeConnectionClose()
        {
            try
            {
                // workaround for a .net bug: http://support.microsoft.com/kb/821625
                Bstream?.Close();
                socket?.Close();
            }
            catch
            {
            }

            Bstream = null;
            socket = null;
        }

        private string ReadLine()
        {
            var sb = new StringBuilder();

            int c;
            while ((c = Bstream.ReadByte()) != -1)
            {
                if (c == '\r')
                    continue;
                if (c == '\n')
                    break;
                sb.Append((char) c);
            }

            return sb.ToString();
        }

        private bool AssertConnectedSocket()
        {
            if (LastConnectedAtTimestamp > 0)
            {
                var now = Stopwatch.GetTimestamp();
                var elapsedSecs = (now - LastConnectedAtTimestamp) / Stopwatch.Frequency;

                if (socket == null || elapsedSecs > IdleTimeOutSecs && !socket.IsConnected())
                {
                    return Reconnect();
                }

                LastConnectedAtTimestamp = now;
            }

            if (socket == null)
            {
                Connect();
            }

            var isConnected = socket != null;

            return isConnected;
        }

        private bool HandleSocketException(SocketException ex)
        {
            HadExceptions = true;
            Console.WriteLine($"SocketException: {ex}");

            lastSocketException = ex;

            // timeout?
            socket.Close();
            socket = null;

            return false;
        }

        private RedisResponseException CreateResponseError(string error)
        {
            HadExceptions = true;
            var throwEx = new RedisResponseException($"{error}, sPort: {clientPort}, LastCommand: {lastCommand}");
            Log(throwEx.Message);
            throw throwEx;
        }

        private RedisException CreateConnectionError()
        {
            HadExceptions = true;
            var throwEx = new RedisException($"Unable to Connect: sPort: {clientPort}", lastSocketException);
            Log(throwEx.Message);
            throw throwEx;
        }

        private static byte[] GetCmdBytes(char cmdPrefix, int noOfLines)
        {
            var strLines = noOfLines.ToString();
            var strLinesLength = strLines.Length;

            var cmdBytes = new byte[1 + strLinesLength + 2];
            cmdBytes[0] = (byte) cmdPrefix;

            for (var i = 0; i < strLinesLength; i++)
                cmdBytes[i + 1] = (byte) strLines[i];

            cmdBytes[1 + strLinesLength] = 0x0D; // \r
            cmdBytes[2 + strLinesLength] = 0x0A; // \n

            return cmdBytes;
        }

        /// <summary>
        /// Command to set multuple binary safe arguments
        /// </summary>
        /// <param name="cmdWithBinaryArgs"></param>
        /// <returns></returns>
        protected bool SendCommand(params byte[][] cmdWithBinaryArgs)
        {
            if (!AssertConnectedSocket()) return false;

            try
            {
                CmdLog(cmdWithBinaryArgs);

                //Total command lines count
                WriteAllToSendBuffer(cmdWithBinaryArgs);

                FlushSendBuffer();
            }
            catch (SocketException ex)
            {
                _cmdBuffer.Clear();
                return HandleSocketException(ex);
            }

            return true;
        }

        public void WriteAllToSendBuffer(params byte[][] cmdWithBinaryArgs)
        {
            WriteToSendBuffer(GetCmdBytes('*', cmdWithBinaryArgs.Length));

            foreach (var safeBinaryValue in cmdWithBinaryArgs)
            {
                WriteToSendBuffer(GetCmdBytes('$', safeBinaryValue.Length));
                WriteToSendBuffer(safeBinaryValue);
                WriteToSendBuffer(endData);
            }
        }

        private readonly IList<ArraySegment<byte>> _cmdBuffer = new List<ArraySegment<byte>>();
        private byte[] _currentBuffer = BufferPool.GetBuffer();
        private int _currentBufferIndex;

        private void WriteToSendBuffer(byte[] cmdBytes)
        {
            if (CouldAddToCurrentBuffer(cmdBytes)) return;

            PushCurrentBuffer();

            if (CouldAddToCurrentBuffer(cmdBytes)) return;

            var bytesCopied = 0;
            while (bytesCopied < cmdBytes.Length)
            {
                var copyOfBytes = BufferPool.GetBuffer();
                var bytesToCopy = Math.Min(cmdBytes.Length - bytesCopied, copyOfBytes.Length);
                Buffer.BlockCopy(cmdBytes, bytesCopied, copyOfBytes, 0, bytesToCopy);
                _cmdBuffer.Add(new ArraySegment<byte>(copyOfBytes, 0, bytesToCopy));
                bytesCopied += bytesToCopy;
            }
        }

        private bool CouldAddToCurrentBuffer(byte[] cmdBytes)
        {
            if (cmdBytes.Length + _currentBufferIndex < BufferPool.BufferLength)
            {
                Buffer.BlockCopy(cmdBytes, 0, _currentBuffer, _currentBufferIndex, cmdBytes.Length);
                _currentBufferIndex += cmdBytes.Length;
                return true;
            }

            return false;
        }

        private void PushCurrentBuffer()
        {
            _cmdBuffer.Add(new ArraySegment<byte>(_currentBuffer, 0, _currentBufferIndex));
            _currentBuffer = BufferPool.GetBuffer();
            _currentBufferIndex = 0;
        }

        private void FlushSendBuffer()
        {
            if (_currentBufferIndex > 0)
                PushCurrentBuffer();

            // Sendling IList<ArraySegment> Throws 'Message to Large' SocketException in Mono
            foreach (var segment in _cmdBuffer)
            {
                var buffer = segment.Array;
                socket.Send(buffer, segment.Offset, segment.Count, SocketFlags.None);
            }

            ResetSendBuffer();
        }

        /// <summary>
        /// reset buffer index in send buffer
        /// </summary>
        public void ResetSendBuffer()
        {
            _currentBufferIndex = 0;
            for (int i = _cmdBuffer.Count - 1; i >= 0; i--)
            {
                var buffer = _cmdBuffer[i].Array;
                BufferPool.ReleaseBufferToPool(ref buffer);
                _cmdBuffer.RemoveAt(i);
            }
        }

        private int SafeReadByte()
        {
            return Bstream.ReadByte();
        }

        protected void SendExpectSuccess(params byte[][] cmdWithBinaryArgs)
        {
            if (!SendCommand(cmdWithBinaryArgs))
                throw CreateConnectionError();

            ExpectSuccess();
        }

        protected long SendExpectLong(params byte[][] cmdWithBinaryArgs)
        {
            if (!SendCommand(cmdWithBinaryArgs))
                throw CreateConnectionError();

            return ReadLong();
        }

        protected byte[] SendExpectData(params byte[][] cmdWithBinaryArgs)
        {
            if (!SendCommand(cmdWithBinaryArgs))
                throw CreateConnectionError();

            return ReadData();
        }

        protected string SendExpectString(params byte[][] cmdWithBinaryArgs)
        {
            var bytes = SendExpectData(cmdWithBinaryArgs);
            return bytes.FromUtf8Bytes();
        }

        protected double SendExpectDouble(params byte[][] cmdWithBinaryArgs)
        {
            if (!SendCommand(cmdWithBinaryArgs))
                throw CreateConnectionError();

            return ReadDouble();
        }

        public double ReadDouble()
        {
            var bytes = ReadData();
            return (bytes == null) ? double.NaN : ParseDouble(bytes);
        }

        public static double ParseDouble(byte[] doubleBytes)
        {
            var doubleString = Encoding.UTF8.GetString(doubleBytes);

            double d;
            double.TryParse(doubleString, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out d);

            return d;
        }

        protected string SendExpectCode(params byte[][] cmdWithBinaryArgs)
        {
            if (!SendCommand(cmdWithBinaryArgs))
                throw CreateConnectionError();

            return ExpectCode();
        }

        protected byte[][] SendExpectMultiData(params byte[][] cmdWithBinaryArgs)
        {
            if (!SendCommand(cmdWithBinaryArgs))
                throw CreateConnectionError();

            return ReadMultiData();
        }

        protected object[] SendExpectDeeplyNestedMultiData(params byte[][] cmdWithBinaryArgs)
        {
            if (!SendCommand(cmdWithBinaryArgs))
                throw CreateConnectionError();

            return ReadDeeplyNestedMultiData();
        }

        [Conditional("DEBUG_REDIS")]
        protected void Log(string fmt, params object[] args)
        {
            Console.WriteLine("{0}", string.Format(fmt, args).Trim());
        }

        [Conditional("DEBUG_REDIS")]
        protected void CmdLog(byte[][] args)
        {
            var sb = new StringBuilder();
            foreach (var arg in args)
            {
                if (sb.Length > 0)
                    sb.Append(" ");

                sb.Append(arg.FromUtf8Bytes());
            }

            lastCommand = sb.ToString();
            if (lastCommand.Length > 100)
            {
                lastCommand = lastCommand.Substring(0, 100) + "...";
            }

            Console.WriteLine("S: " + lastCommand);
        }

        protected void ExpectSuccess()
        {
            int c = SafeReadByte();
            if (c == -1)
                throw CreateResponseError("No more data");

            var s = ReadLine();

            Log((char) c + s);

            if (c == '-')
                throw CreateResponseError(s.StartsWith("ERR") && s.Length >= 4 ? s.Substring(4) : s);
        }

        private void ExpectWord(string word)
        {
            int c = SafeReadByte();
            if (c == -1)
                throw CreateResponseError("No more data");

            var s = ReadLine();

            Log((char) c + s);

            if (c == '-')
                throw CreateResponseError(s.StartsWith("ERR") ? s.Substring(4) : s);

            if (s != word)
                throw CreateResponseError(string.Format("Expected '{0}' got '{1}'", word, s));
        }

        private string ExpectCode()
        {
            int c = SafeReadByte();
            if (c == -1)
                throw CreateResponseError("No more data");

            var s = ReadLine();

            Log((char) c + s);

            if (c == '-')
                throw CreateResponseError(s.StartsWith("ERR") ? s.Substring(4) : s);

            return s;
        }

        internal void ExpectOk()
        {
            ExpectWord("OK");
        }

        internal void ExpectQueued()
        {
            ExpectWord("QUEUED");
        }

        public long ReadInt()
        {
            int c = SafeReadByte();
            if (c == -1)
                throw CreateResponseError("No more data");

            var s = ReadLine();

            Log("R: {0}", s);

            if (c == '-')
                throw CreateResponseError(s.StartsWith("ERR") ? s.Substring(4) : s);

            if (c == ':' || c == '$') //really strange why ZRANK needs the '$' here
            {
                int i;
                if (int.TryParse(s, out i))
                    return i;
            }

            throw CreateResponseError("Unknown reply on integer response: " + c + s);
        }

        public long ReadLong()
        {
            int c = SafeReadByte();
            if (c == -1)
                throw CreateResponseError("No more data");

            var s = ReadLine();

            Log("R: {0}", s);

            if (c == '-')
                throw CreateResponseError(s.StartsWith("ERR") ? s.Substring(4) : s);

            if (c == ':' || c == '$') //really strange why ZRANK needs the '$' here
            {
                long i;
                if (long.TryParse(s, out i))
                    return i;
            }

            throw CreateResponseError("Unknown reply on integer response: " + c + s);
        }

        private byte[] ReadData()
        {
            var r = ReadLine();
            return ParseSingleLine(r);
        }

        private byte[] ParseSingleLine(string r)
        {
            Log("R: {0}", r);
            if (r.Length == 0)
                throw CreateResponseError("Zero length respose");

            char c = r[0];
            if (c == '-')
                throw CreateResponseError(r.StartsWith("-ERR") ? r.Substring(5) : r.Substring(1));

            if (c == '$')
            {
                if (r == "$-1")
                    return null;
                int count;

                if (Int32.TryParse(r.Substring(1), out count))
                {
                    var retbuf = new byte[count];

                    var offset = 0;
                    while (count > 0)
                    {
                        var readCount = Bstream.Read(retbuf, offset, count);
                        if (readCount <= 0)
                            throw CreateResponseError("Unexpected end of Stream");

                        offset += readCount;
                        count -= readCount;
                    }

                    if (Bstream.ReadByte() != '\r' || Bstream.ReadByte() != '\n')
                        throw CreateResponseError("Invalid termination");

                    return retbuf;
                }

                throw CreateResponseError("Invalid length");
            }

            if (c == ':')
            {
                //match the return value
                return r.Substring(1).ToUtf8Bytes();
            }

            throw CreateResponseError("Unexpected reply: " + r);
        }

        private byte[][] ReadMultiData()
        {
            int c = SafeReadByte();
            if (c == -1)
                throw CreateResponseError("No more data");

            var s = ReadLine();
            Log("R: {0}", s);

            switch (c)
            {
                // Some commands like BRPOPLPUSH may return Bulk Reply instead of Multi-bulk
                case '$':
                    var t = new byte[2][];
                    t[1] = ParseSingleLine(string.Concat(char.ToString((char) c), s));
                    return t;

                case '-':
                    throw CreateResponseError(s.StartsWith("ERR") ? s.Substring(4) : s);

                case '*':
                    int count;
                    if (int.TryParse(s, out count))
                    {
                        if (count == -1)
                        {
                            //redis is in an invalid state
                            return new byte[0][];
                        }

                        var result = new byte[count][];

                        for (int i = 0; i < count; i++)
                            result[i] = ReadData();

                        return result;
                    }

                    break;
            }

            throw CreateResponseError("Unknown reply on multi-request: " + c + s);
        }

        private object[] ReadDeeplyNestedMultiData()
        {
            return (object[]) ReadDeeplyNestedMultiDataItem();
        }

        private object ReadDeeplyNestedMultiDataItem()
        {
            int c = SafeReadByte();
            if (c == -1)
                throw CreateResponseError("No more data");

            var s = ReadLine();
            Log("R: {0}", s);

            switch (c)
            {
                case '$':
                    return ParseSingleLine(string.Concat(char.ToString((char) c), s));

                case '-':
                    throw CreateResponseError(s.StartsWith("ERR") ? s.Substring(4) : s);

                case '*':
                    int count;
                    if (int.TryParse(s, out count))
                    {
                        var array = new object[count];
                        for (int i = 0; i < count; i++)
                        {
                            array[i] = ReadDeeplyNestedMultiDataItem();
                        }

                        return array;
                    }

                    break;

                default:
                    return s;
            }

            throw CreateResponseError("Unknown reply on multi-request: " + c + s);
        }

        internal int ReadMultiDataResultCount()
        {
            int c = SafeReadByte();
            if (c == -1)
                throw CreateResponseError("No more data");

            var s = ReadLine();
            Log("R: {0}", s);
            if (c == '-')
                throw CreateResponseError(s.StartsWith("ERR") ? s.Substring(4) : s);
            if (c == '*')
            {
                int count;
                if (int.TryParse(s, out count))
                {
                    return count;
                }
            }

            throw CreateResponseError("Unknown reply on multi-request: " + c + s);
        }

        private static void AssertListIdAndValue(string listId, byte[] value)
        {
            if (listId == null)
                throw new ArgumentNullException("listId");
            if (value == null)
                throw new ArgumentNullException("value");
        }

        private static byte[][] MergeCommandWithKeysAndValues(byte[] cmd, byte[][] keys, byte[][] values)
        {
            var firstParams = new[] {cmd};
            return MergeCommandWithKeysAndValues(firstParams, keys, values);
        }

        private static byte[][] MergeCommandWithKeysAndValues(byte[] cmd, byte[] firstArg, byte[][] keys, byte[][] values)
        {
            var firstParams = new[] {cmd, firstArg};
            return MergeCommandWithKeysAndValues(firstParams, keys, values);
        }

        private static byte[][] MergeCommandWithKeysAndValues(byte[][] firstParams,
            byte[][] keys, byte[][] values)
        {
            if (keys == null || keys.Length == 0)
                throw new ArgumentNullException(nameof(keys));
            if (values == null || values.Length == 0)
                throw new ArgumentNullException(nameof(values));
            if (keys.Length != values.Length)
                throw new ArgumentException("The number of values must be equal to the number of keys");

            var keyValueStartIndex = firstParams?.Length ?? 0;

            var keysAndValuesLength = keys.Length * 2 + keyValueStartIndex;
            var keysAndValues = new byte[keysAndValuesLength][];

            for (var i = 0; i < keyValueStartIndex; i++)
            {
                keysAndValues[i] = firstParams[i];
            }

            var j = 0;
            for (var i = keyValueStartIndex; i < keysAndValuesLength; i += 2)
            {
                keysAndValues[i] = keys[j];
                keysAndValues[i + 1] = values[j];
                j++;
            }

            return keysAndValues;
        }

        private static byte[][] MergeCommandWithArgs(byte[] cmd, params string[] args)
        {
            var byteArgs = args.ToMultiByteArray();
            return MergeCommandWithArgs(cmd, byteArgs);
        }

        private static byte[][] MergeCommandWithArgs(byte[] cmd, params byte[][] args)
        {
            var mergedBytes = new byte[1 + args.Length][];
            mergedBytes[0] = cmd;
            for (var i = 0; i < args.Length; i++)
            {
                mergedBytes[i + 1] = args[i];
            }

            return mergedBytes;
        }

        private static byte[][] MergeCommandWithArgs(byte[] cmd, byte[] firstArg, params byte[][] args)
        {
            var mergedBytes = new byte[2 + args.Length][];
            mergedBytes[0] = cmd;
            mergedBytes[1] = firstArg;
            for (var i = 0; i < args.Length; i++)
            {
                mergedBytes[i + 2] = args[i];
            }

            return mergedBytes;
        }

        protected byte[][] ConvertToBytes(string[] keys)
        {
            var keyBytes = new byte[keys.Length][];
            for (var i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                keyBytes[i] = key != null ? key.ToUtf8Bytes() : new byte[0];
            }

            return keyBytes;
        }

        protected byte[][] MergeAndConvertToBytes(string[] keys, string[] args)
        {
            if (keys == null)
                keys = new string[0];
            if (args == null)
                args = new string[0];

            var keysLength = keys.Length;
            var merged = new string[keysLength + args.Length];
            for (var i = 0; i < merged.Length; i++)
            {
                merged[i] = i < keysLength ? keys[i] : args[i - keysLength];
            }

            return ConvertToBytes(merged);
        }
    }

    internal class BufferPool
    {
        internal static void Flush()
        {
            for (int i = 0; i < pool.Length; i++)
            {
                Interlocked.Exchange(ref pool[i], null); // and drop the old value on the floor
            }
        }

        private BufferPool()
        {
        }

        const int PoolSize = 1000; //1.45MB
        internal const int BufferLength = 1450; //MTU size - some headers
        private static readonly object[] pool = new object[PoolSize];

        internal static byte[] GetBuffer()
        {
            object tmp;
            for (int i = 0; i < pool.Length; i++)
            {
                if ((tmp = Interlocked.Exchange(ref pool[i], null)) != null)
                    return (byte[]) tmp;
            }

            return new byte[BufferLength];
        }

        internal static void ResizeAndFlushLeft(ref byte[] buffer, int toFitAtLeastBytes, int copyFromIndex, int copyBytes)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(toFitAtLeastBytes > buffer.Length);
            Debug.Assert(copyFromIndex >= 0);
            Debug.Assert(copyBytes >= 0);

            // try doubling, else match
            int newLength = buffer.Length * 2;
            if (newLength < toFitAtLeastBytes) newLength = toFitAtLeastBytes;

            var newBuffer = new byte[newLength];
            if (copyBytes > 0)
            {
                Buffer.BlockCopy(buffer, copyFromIndex, newBuffer, 0, copyBytes);
            }

            if (buffer.Length == BufferLength)
            {
                ReleaseBufferToPool(ref buffer);
            }

            buffer = newBuffer;
        }

        internal static void ReleaseBufferToPool(ref byte[] buffer)
        {
            if (buffer == null) return;
            if (buffer.Length == BufferLength)
            {
                for (int i = 0; i < pool.Length; i++)
                {
                    if (Interlocked.CompareExchange(ref pool[i], buffer, null) == null)
                    {
                        break; // found a null; swapped it in
                    }
                }
            }

            // if no space, just drop it on the floor
            buffer = null;
        }
    }
}