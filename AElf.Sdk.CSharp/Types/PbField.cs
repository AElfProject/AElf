using System;
using System.Threading.Tasks;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AElf.Types.CSharp;

namespace AElf.Sdk.CSharp.Types
{
    public class PbField<T> where T : IMessage
    {
        public string _name;
        public PbField(string name)
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

    public class BoolField
    {
        private PbField<BoolValue> _inner;
        public BoolField(string name)
        {
            _inner = new PbField<BoolValue>(name);
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

    public class UInt32Field
    {
        private PbField<UInt32Value> _inner;
        public UInt32Field(string name)
        {
            _inner = new PbField<UInt32Value>(name);
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

    public class Int32Field
    {
        private PbField<SInt32Value> _inner;
        public Int32Field(string name)
        {
            _inner = new PbField<SInt32Value>(name);
        }
        public async Task SetAsync(int value)
        {
            await _inner.SetAsync(new SInt32Value() { Value = value });
        }
        public async Task<int> GetAsync()
        {
            return (await _inner.GetAsync())?.Value ?? default(int);
        }
    }

    public class UInt64Field
    {
        private PbField<UInt64Value> _inner;
        public UInt64Field(string name)
        {
            _inner = new PbField<UInt64Value>(name);
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

    public class Int64Field
    {
        private PbField<SInt64Value> _inner;
        public Int64Field(string name)
        {
            _inner = new PbField<SInt64Value>(name);
        }
        public async Task SetAsync(long value)
        {
            await _inner.SetAsync(new SInt64Value() { Value = value });
        }
        public async Task<long> GetAsync()
        {
            return (await _inner.GetAsync())?.Value ?? default(long);
        }
    }

    public class StringField
    {
        private PbField<StringValue> _inner;
        public StringField(string name)
        {
            _inner = new PbField<StringValue>(name);
        }
        public async Task SetAsync(string value)
        {
            await _inner.SetAsync(new StringValue() { Value = value });
        }
        public async Task<string> GetAsync()
        {
            return (await _inner.GetAsync())?.Value ?? default(string);
        }
    }

    public class BytesField
    {
        private PbField<BytesValue> _inner;
        public BytesField(string name)
        {
            _inner = new PbField<BytesValue>(name);
        }
        public async Task SetAsync(byte[] value)
        {
            await _inner.SetAsync(new BytesValue() { Value = ByteString.CopyFrom(value) });
        }
        public async Task<byte[]> GetAsync()
        {
            return (await _inner.GetAsync())?.Value.ToByteArray() ?? new byte[] { };
        }
    }

}
