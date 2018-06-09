using System;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Sdk.CSharp.Types
{
    public class AElfPbType<T> where T : IMessage
    {
        public string _name;
        public AElfPbType(string name)
        {
            _name = name;
        }

        public async Task SetAsync(T value)
        {
            if (value != null)
            {
                await Api.GetDataProvider("").SetAsync(_name.CalculateHash(), value.ToByteArray());
            }
        }

        public async Task<T> GetAsync()
        {
            byte[] bytes = await Api.GetDataProvider("").GetAsync(_name.CalculateHash());
            return Api.Serializer.Deserialize<T>(bytes);
        }
    }

    public class AElfBool
    {
        private AElfPbType<BoolValue> _inner;
        public AElfBool(string name)
        {
            _inner = new AElfPbType<BoolValue>(name);
        }
        public async Task SetAsync(bool value)
        {
            await _inner.SetAsync(new BoolValue() { Value = value });
        }
        public async Task<bool> GetAsync()
        {
            return (await _inner.GetAsync())?.Value ?? default(bool);
        }
    }

    public class AElfUInt32
    {
        private AElfPbType<UInt32Value> _inner;
        public AElfUInt32(string name)
        {
            _inner = new AElfPbType<UInt32Value>(name);
        }
        public async Task SetAsync(uint value)
        {
            await _inner.SetAsync(new UInt32Value() { Value = value });
        }
        public async Task<uint> GetAsync()
        {
            return (await _inner.GetAsync())?.Value ?? default(uint);
        }
    }

    public class AElfInt32
    {
        private AElfPbType<Int32Value> _inner;
        public AElfInt32(string name)
        {
            _inner = new AElfPbType<Int32Value>(name);
        }
        public async Task SetAsync(int value)
        {
            await _inner.SetAsync(new Int32Value() { Value = value });
        }
        public async Task<int> GetAsync()
        {
            return (await _inner.GetAsync())?.Value ?? default(int);
        }
    }

    public class AElfUInt64
    {
        private AElfPbType<UInt64Value> _inner;
        public AElfUInt64(string name)
        {
            _inner = new AElfPbType<UInt64Value>(name);
        }
        public async Task SetAsync(ulong value)
        {
            await _inner.SetAsync(new UInt64Value() { Value = value });
        }
        public async Task<ulong> GetAsync()
        {
            return (await _inner.GetAsync())?.Value ?? default(ulong);
        }
    }

    public class AElfInt64
    {
        private AElfPbType<Int64Value> _inner;
        public AElfInt64(string name)
        {
            _inner = new AElfPbType<Int64Value>(name);
        }
        public async Task SetAsync(long value)
        {
            await _inner.SetAsync(new Int64Value() { Value = value });
        }
        public async Task<long> GetAsync()
        {
            return (await _inner.GetAsync())?.Value ?? default(long);
        }
    }

    public class AElfBytes
    {
        private AElfPbType<BytesValue> _inner;
        public AElfBytes(string name)
        {
            _inner = new AElfPbType<BytesValue>(name);
        }
        public async Task SetAsync(byte[] value)
        {
            await _inner.SetAsync(new BytesValue() { Value = ByteString.CopyFrom(value) });
        }
        public async Task<byte[]> GetAsync()
        {
            return (await _inner.GetAsync())?.Value.ToByteArray() ?? new byte[]{};
        }
    }

}
