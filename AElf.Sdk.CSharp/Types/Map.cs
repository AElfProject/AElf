using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.SmartContract;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Sdk.CSharp.Types
{
    public class Map
    {
        protected readonly string Name;
        private IDataProvider _dataProvider;

        protected IDataProvider DataProvider
        {
            get
            {
                if (_dataProvider == null)
                {
                    _dataProvider = Api.GetDataProvider(Name);
                }

                return _dataProvider;
            }
        }

        protected Map(string name)
        {
            Name = name;
        }

        internal Map(IDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }
    }

    public class Map<TKey, TValue> : Map where TKey : IMessage where TValue : IMessage, new()
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
            await DataProvider.SetAsync<TValue>(Hash.FromMessage(key), value.ToByteArray());
        }

        public async Task<TValue> GetValueAsync(TKey key)
        {
            var bytes = await DataProvider.GetAsync<TValue>(Hash.FromMessage(key)) ?? new byte[0];
            return Api.Serializer.Deserialize<TValue>(bytes);
        }

//        public async Task SetValueAsync(TKey key, TValue value)
//        {
//            await DataProvider.SetDataAsync(Hash.FromMessage(key), value);
//        }
    }

    public class MapToBool<TKey> : Map<TKey, BoolValue> where TKey : IMessage
    {
        public MapToBool(string name) : base(name)
        {
        }

        internal MapToBool(IDataProvider dataProvider) : base(dataProvider)
        {
        }

        public new bool this[TKey key]
        {
            get => GetValue(key);
            set => SetValue(key, value);
        }

        public void SetValue(TKey keyHash, bool value)
        {
            var task = SetValueAsync(keyHash, value);
            task.Wait();
        }

        public new bool GetValue(TKey keyHash)
        {
            var task = GetValueAsync(keyHash);
            task.Wait();
            return task.Result;
        }

        public async Task SetValueAsync(TKey key, bool value)
        {
            await base.SetValueAsync(key, new BoolValue
            {
                Value = value
            });
        }

        public new async Task<bool> GetValueAsync(TKey key)
        {
            var retval = await base.GetValueAsync(key);
            return retval?.Value ?? false;
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

        public new uint this[TKey key]
        {
            get => GetValue(key);
            set => SetValue(key, value);
        }

        public void SetValue(TKey keyHash, uint value)
        {
            var task = SetValueAsync(keyHash, value);
            task.Wait();
        }

        public new uint GetValue(TKey keyHash)
        {
            var task = GetValueAsync(keyHash);
            task.Wait();
            return task.Result;
        }

        public async Task SetValueAsync(TKey key, uint value)
        {
            await base.SetValueAsync(key, new UInt32Value
            {
                Value = value
            });
        }

        public new async Task<uint> GetValueAsync(TKey key)
        {
            var retval = await base.GetValueAsync(key);
            return retval?.Value ?? 0;
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

        public new int this[TKey key]
        {
            get => GetValue(key);
            set => SetValue(key, value);
        }

        public void SetValue(TKey keyHash, int value)
        {
            var task = SetValueAsync(keyHash, value);
            task.Wait();
        }

        public new int GetValue(TKey keyHash)
        {
            var task = GetValueAsync(keyHash);
            task.Wait();
            return task.Result;
        }

        public async Task SetValueAsync(TKey key, int value)
        {
            await base.SetValueAsync(key, new SInt32Value
            {
                Value = value
            });
        }

        public new async Task<int> GetValueAsync(TKey key)
        {
            var retval = await base.GetValueAsync(key);
            return retval?.Value ?? 0;
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

        public new ulong this[TKey key]
        {
            get => GetValue(key);
            set => SetValue(key, value);
        }

        public void SetValue(TKey keyHash, ulong value)
        {
            var task = SetValueAsync(keyHash, value);
            task.Wait();
        }

        public new ulong GetValue(TKey keyHash)
        {
            var task = GetValueAsync(keyHash);
            task.Wait();
            return task.Result;
        }

        public async Task SetValueAsync(TKey key, ulong value)
        {
            await base.SetValueAsync(key, new UInt64Value
            {
                Value = value
            });
        }

        public new async Task<ulong> GetValueAsync(TKey key)
        {
            var retval = await base.GetValueAsync(key);
            return retval?.Value ?? 0;
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

        public new long this[TKey key]
        {
            get => GetValue(key);
            set => SetValue(key, value);
        }

        public void SetValue(TKey keyHash, long value)
        {
            var task = SetValueAsync(keyHash, value);
            task.Wait();
        }

        public new long GetValue(TKey keyHash)
        {
            var task = GetValueAsync(keyHash);
            task.Wait();
            return task.Result;
        }

        public async Task SetValueAsync(TKey key, long value)
        {
            await base.SetValueAsync(key, new Int64Value
            {
                Value = value
            });
        }

        public new async Task<long> GetValueAsync(TKey key)
        {
            var retval = await base.GetValueAsync(key);
            return retval?.Value ?? 0;
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

        public new byte[] this[TKey key]
        {
            get => GetValue(key);
            set => SetValue(key, value);
        }

        public void SetValue(TKey keyHash, byte[] value)
        {
            var task = SetValueAsync(keyHash, value);
            task.Wait();
        }

        public new byte[] GetValue(TKey keyHash)
        {
            var task = GetValueAsync(keyHash);
            task.Wait();
            return task.Result;
        }

        public async Task SetValueAsync(TKey key, byte[] value)
        {
            await base.SetValueAsync(key, new BytesValue
            {
                Value = ByteString.CopyFrom(value)
            });
        }

        public new async Task<byte[]> GetValueAsync(TKey key)
        {
            var retval = await base.GetValueAsync(key);
            return retval?.Value.ToByteArray() ?? new byte[0];
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

        public new string this[TKey key]
        {
            get => GetValue(key);
            set => SetValue(key, value);
        }

        public void SetValue(TKey keyHash, string value)
        {
            var task = SetValueAsync(keyHash, value);
            task.Wait();
        }

        public new string GetValue(TKey keyHash)
        {
            var task = GetValueAsync(keyHash);
            task.Wait();
            return task.Result;
        }

        public async Task SetValueAsync(TKey key, string value)
        {
            await base.SetValueAsync(key, new StringValue
            {
                Value = value
            });
        }

        public new async Task<string> GetValueAsync(TKey key)
        {
            var retval = await base.GetValueAsync(key);
            return retval?.Value ?? "";
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
            await DataProvider.SetAsync<UserTypeHolder>(Hash.FromMessage(key), value.ToPbMessage().ToByteArray());
        }

        public async Task<TValue> GetValueAsync(TKey key)
        {
            var obj = (TValue) Activator.CreateInstance(typeof(TValue));
            var bytes = await DataProvider.GetAsync<UserTypeHolder>(Hash.FromMessage(key));
            if (bytes == null)
            {
                return default(TValue);
            }

            var userTypeHolder = Api.Serializer.Deserialize<UserTypeHolder>(bytes);
            obj.Unpack(userTypeHolder);
            return obj;
        }

        public IDataProvider GetSubDataProvider(string dataProviderKey)
        {
            return Api.GetDataProvider(Name).GetDataProvider(dataProviderKey);
        }
    }
}