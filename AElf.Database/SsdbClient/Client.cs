using System;
using System.Collections.Generic;
using System.Linq;

namespace AElf.Database.SsdbClient
{
    public class Client : IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private Link _link;

        public Client(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public bool Connect()
        {
            _link = new Link(_host, _port);
            return _link.Connect();
        }

        public void Close()
        {
            _link.Close();
        }

        public void Dispose()
        {
            Close();
        }

        private void CheckRequestKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("key can't be empty");
            }
        }
        
        private void CheckRequestKey(byte[] key)
        {
            if (key == null || key.Length == 0)
            {
                throw new ArgumentException("key can't be empty");
            }
        }

        private void CheckResponse(byte[] code)
        {
            if (!Helper.BytesEqual(ResponseCode.OkByte, code))
            {
                throw new Exception(Helper.BytesToString(code));
            }
        }

        private KeyValuePair<string, byte[]>[] ParseRespToKeyValuePair(List<byte[]> resp)
        {
            var size = (resp.Count - 1) / 2;
            var kvs = new KeyValuePair<string, byte[]>[size];
            for (var i = 0; i < size; i ++)
            {
                var key = Helper.BytesToString(resp[i * 2 + 1]);
                var val = resp[i * 2 + 2];
                kvs[i] = new KeyValuePair<string, byte[]>(key, val);
            }

            return kvs;
        }

        private KeyValuePair<string, long>[] ParseRespToKeyValuePairLong(List<byte[]> resp)
        {
            var size = (resp.Count - 1) / 2;
            var kvs = new KeyValuePair<string, long>[size];
            for (var i = 0; i < size; i ++)
            {
                var key = Helper.BytesToString(resp[i * 2 + 1]);
                var val = long.Parse(Helper.BytesToString(resp[i * 2 + 2]));
                kvs[i] = new KeyValuePair<string, long>(key, val);
            }

            return kvs;
        }
        
        private IList<string> ParseRespToList(List<byte[]> resp)
        {
            var size = resp.Count - 1;
            var list = new List<string>(size);
            for (var i = 1; i < size; i ++)
            {
                list.Add(Helper.BytesToString(resp[i]));
            }
            return list;
        }
        
        /***** flush *****/

        internal void FlushDB(SsdbType type)
        {
            switch (type)
            {
                case SsdbType.None:
                    FlushDBKeyValue();
                    FlushDBHash();
                    FlushDBZSet();
                    break;
                case SsdbType.KeyValue:
                    FlushDBKeyValue();
                    break;
                case SsdbType.Hash:
                    FlushDBHash();
                    break;
                case SsdbType.ZSet:
                    FlushDBZSet();
                    break;
            }
        }

        internal void FlushDBKeyValue()
        {
            while (true) {
                var keys = Keys("", "", 1000);
                if (keys.Any())
                {
                    MultiHDel(keys.ToList());
                }
                else
                {
                    return;
                }
            }
        }
        
        internal void FlushDBHash()
        {
            while (true) {
                var names = HList("", "", 1000);
                if (names.Count == 0)
                {
                    return;
                }
                foreach (var name in names)
                {
                    HClear(name);
                }
            }
        }
        
        internal void FlushDBZSet()
        {
            while (true) {
                var names = ZList("", "", 1000);
                if (names.Count == 0)
                {
                    return;
                }
                foreach (var name in names)
                {
                    ZClear(name);
                }
            }
            
            throw new NotImplementedException();
        }

        /***** kv *****/

        public bool Exists(byte[] key)
        {
            var resp = _link.Request(Command.Exists, key);
            var respCode = Helper.BytesToString(resp[0]);
            if (respCode == ResponseCode.NotFound)
            {
                return false;
            }

            CheckResponse(resp[0]);
            if (resp.Count != 2)
            {
                throw new Exception("Bad response!");
            }

            return (Helper.BytesToString(resp[1]) == "1" ? true : false);
        }

        public bool Exists(string key)
        {
            CheckRequestKey(key);
            return Exists(Helper.StringToBytes(key));
        }

        public void Set(byte[] key, byte[] val)
        {
            CheckRequestKey(key);
            var resp = _link.Request(Command.Set, key, val);
            CheckResponse(resp[0]);
        }

        public void Set(string key, string val)
        {
            CheckRequestKey(key);
            Set(Helper.StringToBytes(key), Helper.StringToBytes(val));
        }

        public void Set(string key, byte[] val)
        {
            CheckRequestKey(key);
            Set(Helper.StringToBytes(key), val);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns>returns true if name.key is found, otherwise returns false.</returns>
        public bool Get(byte[] key, out byte[] val)
        {
            CheckRequestKey(key);
            val = null;
            var resp = _link.Request(Command.Get, key);
            var respCode = Helper.BytesToString(resp[0]);
            if (respCode == ResponseCode.NotFound)
            {
                return false;
            }

            CheckResponse(resp[0]);
            if (resp.Count != 2)
            {
                throw new Exception("Bad response!");
            }

            val = resp[1];
            return true;
        }

        public bool Get(string key, out byte[] val)
        {
            CheckRequestKey(key);
            return Get(Helper.StringToBytes(key), out val);
        }

        public bool Get(string key, out string val)
        {
            CheckRequestKey(key);
            val = null;
            byte[] bs;
            if (!Get(key, out bs))
            {
                return false;
            }

            val = Helper.BytesToString(bs);
            return true;
        }

        public void Del(byte[] key)
        {
            CheckRequestKey(key);
            var resp = _link.Request(Command.Del, key);
            CheckResponse(resp[0]);
        }

        public void Del(string key)
        {
            CheckRequestKey(key);
            Del(Helper.StringToBytes(key));
        }

        public KeyValuePair<string, byte[]>[] Scan(string keyStart, string keyEnd, long limit)
        {
            var resp = _link.Request(Command.Scan, keyStart, keyEnd, limit.ToString());
            CheckResponse(resp[0]);
            return ParseRespToKeyValuePair(resp);
        }

        public KeyValuePair<string, byte[]>[] RScan(string keyStart, string keyEnd, long limit)
        {
            var resp = _link.Request(Command.RScan, keyStart, keyEnd, limit.ToString());
            CheckResponse(resp[0]);
            return ParseRespToKeyValuePair(resp);
        }

        public IList<string> Keys(string keyStart, string keyEnd, long limit)
        {
            var resp = _link.Request(Command.Keys, keyStart, keyEnd, limit.ToString());
            CheckResponse(resp[0]);
            return ParseRespToList(resp);
        }
        
        public void MultiHDel(byte[][] keys)
        {
            var resp = _link.Request(Command.MultiDel, keys);
            CheckResponse(resp[0]);
        }

        public void MultiHDel(List<string> keys)
        {
            var req = new byte[keys.Count][];
            for (var i = 0; i < keys.Count; i++)
            {
                req[i] = Helper.StringToBytes(keys[i]);
            }
            MultiHDel(req);
        }

        /***** hash *****/

        public void HSet(byte[] name, byte[] key, byte[] val)
        {
            var resp = _link.Request(Command.HSet, name, key, val);
            CheckResponse(resp[0]);
        }

        public void HSet(string name, string key, byte[] val)
        {
            HSet(Helper.StringToBytes(name), Helper.StringToBytes(key), val);
        }

        public void HSet(string name, string key, string val)
        {
            HSet(Helper.StringToBytes(name), Helper.StringToBytes(key), Helper.StringToBytes(val));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="name"></param>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns>returns true if name.key is found, otherwise returns false.</returns>
        public bool HGet(byte[] name, byte[] key, out byte[] val)
        {
            val = null;
            var resp = _link.Request(Command.HGet, name, key);
            var respCode = Helper.BytesToString(resp[0]);
            if (respCode == ResponseCode.NotFound)
            {
                return false;
            }

            CheckResponse(resp[0]);
            if (resp.Count != 2)
            {
                throw new Exception("Bad response!");
            }

            val = resp[1];
            return true;
        }

        public bool HGet(string name, string key, out byte[] val)
        {
            return HGet(Helper.StringToBytes(name), Helper.StringToBytes(key), out val);
        }

        public bool HGet(string name, string key, out string val)
        {
            val = null;
            byte[] bs;
            if (!HGet(name, key, out bs))
            {
                return false;
            }

            val = Helper.BytesToString(bs);
            return true;
        }

        public void HDel(byte[] name, byte[] key)
        {
            var resp = _link.Request(Command.HDel, name, key);
            CheckResponse(resp[0]);
        }

        public void HDel(string name, string key)
        {
            HDel(Helper.StringToBytes(name), Helper.StringToBytes(key));
        }

        public bool HExists(byte[] name, byte[] key)
        {
            var resp = _link.Request(Command.HExists, name, key);
            var respCode = Helper.BytesToString(resp[0]);
            if (respCode == ResponseCode.NotFound)
            {
                return false;
            }

            CheckResponse(resp[0]);
            if (resp.Count != 2)
            {
                throw new Exception("Bad response!");
            }

            return (Helper.BytesToString(resp[1]) == "1" ? true : false);
        }

        public bool HExists(string name, string key)
        {
            return HExists(Helper.StringToBytes(name), Helper.StringToBytes(key));
        }

        public long HSize(byte[] name)
        {
            var resp = _link.Request(Command.HSize, name);
            CheckResponse(resp[0]);
            if (resp.Count != 2)
            {
                throw new Exception("Bad response!");
            }

            return long.Parse(Helper.BytesToString(resp[1]));
        }

        public long HSize(string name)
        {
            return HSize(Helper.StringToBytes(name));
        }

        public KeyValuePair<string, byte[]>[] HScan(string name, string keyStart, string keyEnd, long limit)
        {
            var resp = _link.Request(Command.HScan, name, keyStart, keyEnd, limit.ToString());
            CheckResponse(resp[0]);
            return ParseRespToKeyValuePair(resp);
        }

        public KeyValuePair<string, byte[]>[] HRScan(string name, string keyStart, string keyEnd, long limit)
        {
            var resp = _link.Request(Command.HRScan, name, keyStart, keyEnd, limit.ToString());
            CheckResponse(resp[0]);
            return ParseRespToKeyValuePair(resp);
        }

        public void MultiHSet(byte[] name, KeyValuePair<byte[], byte[]>[] kvs)
        {
            var req = new byte[(kvs.Length * 2) + 1][];
            req[0] = name;
            for (var i = 0; i < kvs.Length; i++)
            {
                req[(2 * i) + 1] = kvs[i].Key;
                req[(2 * i) + 2] = kvs[i].Value;
            }

            var resp = _link.Request(Command.MultiHSet, req);
            CheckResponse(resp[0]);
        }

        public void MultiHSet(string name, KeyValuePair<string, string>[] kvs)
        {
            var req = new KeyValuePair<byte[], byte[]>[kvs.Length];
            for (var i = 0; i < kvs.Length; i++)
            {
                req[i] = new KeyValuePair<byte[], byte[]>(Helper.StringToBytes(kvs[i].Key),
                    Helper.StringToBytes(kvs[i].Value));
            }

            MultiHSet(Helper.StringToBytes(name), req);
        }

        public void MultiHDel(byte[] name, byte[][] keys)
        {
            var req = new byte[keys.Length + 1][];
            req[0] = name;
            for (var i = 0; i < keys.Length; i++)
            {
                req[i + 1] = keys[i];
            }

            var resp = _link.Request(Command.MultiHDel, req);
            CheckResponse(resp[0]);
        }

        public void MultiHDel(string name, string[] keys)
        {
            var req = new byte[keys.Length][];
            for (var i = 0; i < keys.Length; i++)
            {
                req[i] = Helper.StringToBytes(keys[i]);
            }

            MultiHDel(Helper.StringToBytes(name), req);
        }

        public KeyValuePair<string, byte[]>[] MultiHGet(byte[] name, byte[][] keys)
        {
            var req = new byte[keys.Length + 1][];
            req[0] = name;
            for (var i = 0; i < keys.Length; i++)
            {
                req[i + 1] = keys[i];
            }

            var resp = _link.Request(Command.MultiHGet, req);
            CheckResponse(resp[0]);
            var ret = ParseRespToKeyValuePair(resp);

            return ret;
        }

        public KeyValuePair<string, byte[]>[] MultiHGet(string name, string[] keys)
        {
            var req = new byte[keys.Length][];
            for (var i = 0; i < keys.Length; i++)
            {
                req[i] = Helper.StringToBytes(keys[i]);
            }

            return MultiHGet(Helper.StringToBytes(name), req);
        }
        
        public IList<string> HList(string nameStart, string nameEnd, long limit)
        {
            var resp = _link.Request(Command.HList, nameStart, nameEnd, limit.ToString());
            CheckResponse(resp[0]);
            return ParseRespToList(resp);
        }

        public void HClear(string name)
        {
            var resp = _link.Request(Command.HClear, name);
            CheckResponse(resp[0]);
        }

        /***** zset *****/

        public void ZSet(byte[] name, byte[] key, long score)
        {
            var resp = _link.Request(Command.ZSet, name, key, Helper.StringToBytes(score.ToString()));
            CheckResponse(resp[0]);
        }

        public void ZSet(string name, string key, long score)
        {
            ZSet(Helper.StringToBytes(name), Helper.StringToBytes(key), score);
        }

        public long ZIncr(byte[] name, byte[] key, long increment)
        {
            var resp = _link.Request(Command.ZIncr, name, key, Helper.StringToBytes(increment.ToString()));
            CheckResponse(resp[0]);
            if (resp.Count != 2)
            {
                throw new Exception("Bad response!");
            }

            return long.Parse(Helper.BytesToString(resp[1]));
        }

        public long ZIncr(string name, string key, long increment)
        {
            return ZIncr(Helper.StringToBytes(name), Helper.StringToBytes(key), increment);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="name"></param>
        /// <param name="key"></param>
        /// <param name="score"></param>
        /// <returns>returns true if name.key is found, otherwise returns false.</returns>
        public bool ZGet(byte[] name, byte[] key, out long score)
        {
            score = -1;
            var resp = _link.Request(Command.ZGet, name, key);
            var respCode = Helper.BytesToString(resp[0]);
            if (respCode == ResponseCode.NotFound)
            {
                return false;
            }

            CheckResponse(resp[0]);
            if (resp.Count != 2)
            {
                throw new Exception("Bad response!");
            }

            score = long.Parse(Helper.BytesToString(resp[1]));
            return true;
        }

        public bool ZGet(string name, string key, out long score)
        {
            return ZGet(Helper.StringToBytes(name), Helper.StringToBytes(key), out score);
        }

        public void ZDel(byte[] name, byte[] key)
        {
            var resp = _link.Request(Command.ZDel, name, key);
            CheckResponse(resp[0]);
        }

        public void ZDel(string name, string key)
        {
            ZDel(Helper.StringToBytes(name), Helper.StringToBytes(key));
        }

        public long ZSize(byte[] name)
        {
            var resp = _link.Request(Command.ZSize, name);
            CheckResponse(resp[0]);
            if (resp.Count != 2)
            {
                throw new Exception("Bad response!");
            }

            return long.Parse(Helper.BytesToString(resp[1]));
        }

        public long ZSize(string name)
        {
            return ZSize(Helper.StringToBytes(name));
        }

        public bool ZExists(byte[] name, byte[] key)
        {
            var resp = _link.Request(Command.ZExists, name, key);
            var respCode = Helper.BytesToString(resp[0]);
            if (respCode == ResponseCode.NotFound)
            {
                return false;
            }

            CheckResponse(resp[0]);
            if (resp.Count != 2)
            {
                throw new Exception("Bad response!");
            }

            return (Helper.BytesToString(resp[1]) == "1" ? true : false);
        }

        public bool ZExists(string name, string key)
        {
            return ZExists(Helper.StringToBytes(name), Helper.StringToBytes(key));
        }

        public KeyValuePair<string, long>[] ZRange(string name, int offset, int limit)
        {
            var resp = _link.Request(Command.ZRange, name, offset.ToString(), limit.ToString());
            CheckResponse(resp[0]);
            return ParseRespToKeyValuePairLong(resp);
        }

        public KeyValuePair<string, long>[] ZRRange(string name, int offset, int limit)
        {
            var resp = _link.Request(Command.ZRRange, name, offset.ToString(), limit.ToString());
            CheckResponse(resp[0]);
            return ParseRespToKeyValuePairLong(resp);
        }

        public KeyValuePair<string, long>[] ZScan(string name, string keyStart, long scoreStart, long scoreEnd,
            long limit)
        {
            var scoreS = "";
            var scoreE = "";
            if (scoreStart != long.MinValue)
            {
                scoreS = scoreStart.ToString();
            }

            if (scoreEnd != long.MaxValue)
            {
                scoreE = scoreEnd.ToString();
            }

            var resp = _link.Request(Command.ZScan, name, keyStart, scoreS, scoreE, limit.ToString());
            CheckResponse(resp[0]);
            return ParseRespToKeyValuePairLong(resp);
        }

        public KeyValuePair<string, long>[] ZRScan(string name, string keyStart, long scoreStart, long scoreEnd,
            long limit)
        {
            var scoreS = "";
            var scoreE = "";
            if (scoreStart != long.MaxValue)
            {
                scoreS = scoreStart.ToString();
            }

            if (scoreEnd != long.MinValue)
            {
                scoreE = scoreEnd.ToString();
            }

            var resp = _link.Request(Command.ZRScan, name, keyStart, scoreS, scoreE, limit.ToString());
            CheckResponse(resp[0]);
            return ParseRespToKeyValuePairLong(resp);
        }

        public void MultiZSet(byte[] name, KeyValuePair<byte[], long>[] kvs)
        {
            var req = new byte[(kvs.Length * 2) + 1][];
            req[0] = name;
            for (var i = 0; i < kvs.Length; i++)
            {
                req[(2 * i) + 1] = kvs[i].Key;
                req[(2 * i) + 2] = Helper.StringToBytes(kvs[i].Value.ToString());
            }

            var resp = _link.Request(Command.MultiZSet, req);
            CheckResponse(resp[0]);
        }

        public void MultiZSet(string name, KeyValuePair<string, long>[] kvs)
        {
            var req = new KeyValuePair<byte[], long>[kvs.Length];
            for (var i = 0; i < kvs.Length; i++)
            {
                req[i] = new KeyValuePair<byte[], long>(Helper.StringToBytes(kvs[i].Key), kvs[i].Value);
            }

            MultiZSet(Helper.StringToBytes(name), req);
        }

        public void MultiZDel(byte[] name, byte[][] keys)
        {
            var req = new byte[keys.Length + 1][];
            req[0] = name;
            for (var i = 0; i < keys.Length; i++)
            {
                req[i + 1] = keys[i];
            }

            var resp = _link.Request(Command.MultiZDel, req);
            CheckResponse(resp[0]);
        }

        public void MultiZDel(string name, string[] keys)
        {
            var req = new byte[keys.Length][];
            for (var i = 0; i < keys.Length; i++)
            {
                req[i] = Helper.StringToBytes(keys[i]);
            }

            MultiZDel(Helper.StringToBytes(name), req);
        }

        public KeyValuePair<string, long>[] MultiZGet(byte[] name, byte[][] keys)
        {
            var req = new byte[keys.Length + 1][];
            req[0] = name;
            for (var i = 0; i < keys.Length; i++)
            {
                req[i + 1] = keys[i];
            }

            var resp = _link.Request(Command.MultiZGet, req);
            CheckResponse(resp[0]);
            return ParseRespToKeyValuePairLong(resp);
        }

        public KeyValuePair<string, long>[] MultiZGet(string name, string[] keys)
        {
            var req = new byte[keys.Length][];
            for (var i = 0; i < keys.Length; i++)
            {
                req[i] = Helper.StringToBytes(keys[i]);
            }

            return MultiZGet(Helper.StringToBytes(name), req);
        }
        
        public IList<string> ZList(string nameStart, string nameEnd, long limit)
        {
            var resp = _link.Request(Command.ZList, nameStart, nameEnd, limit.ToString());
            CheckResponse(resp[0]);
            return ParseRespToList(resp);
        }

        public void ZClear(string name)
        {
            var resp = _link.Request(Command.ZClear, name);
            CheckResponse(resp[0]);
        }
    }
}