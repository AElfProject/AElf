using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AElf.Types.CSharp;

namespace AElf.Sdk.CSharp.Types
{
    public class Map
    {
        protected string _name;
        protected IDataProvider _dataProvider;

        protected IDataProvider DataProvider
        {
            get
            {
                if (_dataProvider == null)
                {
                    _dataProvider = Api.GetDataProvider(_name);
                }

                return _dataProvider;
            }
        }

        public Map(string name)
        {
            _name = name;
            //_dataProvider = Api.GetDataProvider(name);
        }

        internal Map(IDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }

        public void SetValue(Hash keyHash, byte[] value)
        {
            var task = SetValueAsync(keyHash, value);
            task.Wait();
        }

        public byte[] GetValue(Hash keyHash)
        {
            var task = GetValueAsync(keyHash);
            task.Wait();
            return task.Result;
        }

        public async Task SetValueAsync(Hash keyHash, byte[] value)
        {
            await Api.GetDataProvider(_name).SetAsync(keyHash, value);
        }

        public async Task<byte[]> GetValueAsync(Hash keyHash)
        {
            return await Api.GetDataProvider(_name).GetAsync(keyHash);
        }

        public IDataProvider GetSubDataProvider(string dataProviderKey)
        {
            return Api.GetDataProvider(_name).GetDataProvider(dataProviderKey);
        }
    }

    public class Map<TKey, TValue> : Map where TKey : IMessage where TValue : IMessage
    {
        public Map(string name) : base(name)
        {
        }

        internal Map(IDataProvider dataProvider) : base(dataProvider)
        {
        }

        public TValue this[TKey key]
        {
            get => GetValue(key);
            set => SetValue(key, value);
        }

        public void SetValue(TKey keyHash, TValue value)
        {
            var task = SetValueAsync(keyHash, value);
            task.Wait();
        }

        public TValue GetValue(TKey keyHash)
        {
            var task = GetValueAsync(keyHash);
            task.Wait();
            return task.Result;
        }

        public async Task SetValueAsync(TKey key, TValue value)
        {
            await DataProvider.SetAsync(key.CalculateHash(), value.ToByteArray());
        }

        public async Task<TValue> GetValueAsync(TKey key)
        {
            var bytes = await DataProvider.GetAsync(key.CalculateHash());
            return Api.Serializer.Deserialize<TValue>(bytes);
        }
    }

    public class MapToBool<TKey> : Map<TKey, BoolValue> where TKey : IMessage
    {
        public MapToBool(string name) : base(name)
        {
        }

        internal MapToBool(IDataProvider dataProvider) : base(dataProvider)
        {
        }

        public void SetValue(TKey keyHash, bool value)
        {
            var task = SetValueAsync(keyHash, value);
            task.Wait();
        }

        public bool GetValue(TKey keyHash)
        {
            var task = GetValueAsync(keyHash);
            task.Wait();
            return task.Result;
        }

        public async Task SetValueAsync(TKey key, bool value)
        {
            await base.SetValueAsync(key, new BoolValue()
            {
                Value = value
            });
        }

        public new async Task<bool> GetValueAsync(TKey key)
        {
            var retval = await base.GetValueAsync(key);
            return retval.Value;
        }
    }

    public class MapToUInt32<TKey> : Map<TKey, UInt32Value> where TKey : IMessage
    {
        public MapToUInt32(string name) : base(name)
        {
        }

        internal MapToUInt32(IDataProvider dataProvider) : base(dataProvider)
        {
        }

        public void SetValue(TKey keyHash, uint value)
        {
            var task = SetValueAsync(keyHash, value);
            task.Wait();
        }

        public uint GetValue(TKey keyHash)
        {
            var task = GetValueAsync(keyHash);
            task.Wait();
            return task.Result;
        }

        public async Task SetValueAsync(TKey key, uint value)
        {
            await base.SetValueAsync(key, new UInt32Value()
            {
                Value = value
            });
        }

        public new async Task<uint> GetValueAsync(TKey key)
        {
            var retval = await base.GetValueAsync(key);
            return retval.Value;
        }
    }

    public class MapToInt32<TKey> : Map<TKey, SInt32Value> where TKey : IMessage
    {
        public MapToInt32(string name) : base(name)
        {
        }

        internal MapToInt32(IDataProvider dataProvider) : base(dataProvider)
        {
        }

        public void SetValue(TKey keyHash, int value)
        {
            var task = SetValueAsync(keyHash, value);
            task.Wait();
        }

        public int GetValue(TKey keyHash)
        {
            var task = GetValueAsync(keyHash);
            task.Wait();
            return task.Result;
        }

        public async Task SetValueAsync(TKey key, int value)
        {
            await base.SetValueAsync(key, new SInt32Value()
            {
                Value = value
            });
        }

        public new async Task<int> GetValueAsync(TKey key)
        {
            var retval = await base.GetValueAsync(key);
            return retval.Value;
        }
    }

    public class MapToUInt64<TKey> : Map<TKey, UInt64Value> where TKey : IMessage
    {
        public MapToUInt64(string name) : base(name)
        {
        }

        internal MapToUInt64(IDataProvider dataProvider) : base(dataProvider)
        {
        }

        public void SetValue(TKey keyHash, ulong value)
        {
            var task = SetValueAsync(keyHash, value);
            task.Wait();
        }

        public ulong GetValue(TKey keyHash)
        {
            var task = GetValueAsync(keyHash);
            task.Wait();
            return task.Result;
        }

        public async Task SetValueAsync(TKey key, ulong value)
        {
            await base.SetValueAsync(key, new UInt64Value()
            {
                Value = value
            });
        }

        public new async Task<ulong> GetValueAsync(TKey key)
        {
            var retval = await base.GetValueAsync(key);
            return retval.Value;
        }
    }

    public class MapToInt64<TKey> : Map<TKey, Int64Value> where TKey : IMessage
    {
        public MapToInt64(string name) : base(name)
        {
        }

        internal MapToInt64(IDataProvider dataProvider) : base(dataProvider)
        {
        }

        public void SetValue(TKey keyHash, long value)
        {
            var task = SetValueAsync(keyHash, value);
            task.Wait();
        }

        public long GetValue(TKey keyHash)
        {
            var task = GetValueAsync(keyHash);
            task.Wait();
            return task.Result;
        }

        public async Task SetValueAsync(TKey key, long value)
        {
            await base.SetValueAsync(key, new Int64Value()
            {
                Value = value
            });
        }

        public new async Task<long> GetValueAsync(TKey key)
        {
            var retval = await base.GetValueAsync(key);
            return retval.Value;
        }
    }

    public class MapToBytes<TKey> : Map<TKey, BytesValue> where TKey : IMessage
    {
        public MapToBytes(string name) : base(name)
        {
        }

        internal MapToBytes(IDataProvider dataProvider) : base(dataProvider)
        {
        }

        public void SetValue(TKey keyHash, byte[] value)
        {
            var task = SetValueAsync(keyHash, value);
            task.Wait();
        }

        public byte[] GetValue(TKey keyHash)
        {
            var task = GetValueAsync(keyHash);
            task.Wait();
            return task.Result;
        }

        public async Task SetValueAsync(TKey key, byte[] value)
        {
            await base.SetValueAsync(key, new BytesValue()
            {
                Value = ByteString.CopyFrom(value)
            });
        }

        public new async Task<byte[]> GetValueAsync(TKey key)
        {
            var retval = await base.GetValueAsync(key);
            return retval.Value.ToByteArray();
        }
    }

    public class MapToString<TKey> : Map<TKey, StringValue> where TKey : IMessage
    {
        public MapToString(string name) : base(name)
        {
        }

        internal MapToString(IDataProvider dataProvider) : base(dataProvider)
        {
        }

        public void SetValue(TKey keyHash, string value)
        {
            var task = SetValueAsync(keyHash, value);
            task.Wait();
        }

        public string GetValue(TKey keyHash)
        {
            var task = GetValueAsync(keyHash);
            task.Wait();
            return task.Result;
        }

        public async Task SetValueAsync(TKey key, string value)
        {
            await base.SetValueAsync(key, new StringValue()
            {
                Value = value
            });
        }

        public new async Task<string> GetValueAsync(TKey key)
        {
            var retval = await base.GetValueAsync(key);
            return retval.Value;
        }
    }

    public class MapToUserType<TKey, TValue> : Map where TKey : IMessage where TValue : UserType
    {
        public MapToUserType(string name) : base(name)
        {
        }

        internal MapToUserType(IDataProvider dataProvider) : base(dataProvider)
        {
        }

        public void SetValue(TKey keyHash, TValue value)
        {
            var task = SetValueAsync(keyHash, value);
            task.Wait();
        }

        public TValue GetValue(TKey keyHash)
        {
            var task = GetValueAsync(keyHash);
            task.Wait();
            return task.Result;
        }

        public async Task SetValueAsync(TKey key, TValue value)
        {
            await DataProvider.SetAsync(key.CalculateHash(), value.ToPbMessage().ToByteArray());
        }

        public async Task<TValue> GetValueAsync(TKey key)
        {
            var obj = (TValue) Activator.CreateInstance(typeof(TValue));
            var bytes = await DataProvider.GetAsync(key.CalculateHash());
            var userTypeHolder = Api.Serializer.Deserialize<UserTypeHolder>(bytes);
            obj.Unpack(userTypeHolder);
            return obj;
        }

        public IDataProvider GetSubDataProvider(string dataProviderKey)
        {
            return Api.GetDataProvider(_name).GetDataProvider(dataProviderKey);
        }
    }
}