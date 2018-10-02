using System;
using System.Threading.Tasks;
using AElf.Common;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AElf.Types.CSharp;

namespace AElf.Sdk.CSharp.Types
{
    public class PbField<T> where T : IMessage, new()
    {
        private readonly string _name;
        public PbField(string name)
        {
            _name = name;
        }

        public void SetValue(T value)
        {
            var task = SetAsync(value);
            task.Wait();
        }

        public T GetValue()
        {
            var task = GetAsync();
            task.Wait();
            return task.Result;
        }
        
        public async Task SetAsync(T value)
        {
            if (value != null)
            {
                await Api.GetDataProvider("").SetAsync<T>(Hash.FromString(_name), value.ToByteArray());
            }
        }

        public async Task<T> GetAsync()
        {
            var bytes = await Api.GetDataProvider("").GetAsync<T>(Hash.FromString(_name));
            return bytes == null ? default(T) : Api.Serializer.Deserialize<T>(bytes);
        }

        public async Task SetDataAsync(T value)
        {
            if (value != null)
            {
                await Api.GetDataProvider("").SetDataAsync(Hash.FromString(_name), value);
            }
        }
    }

    public class BoolField
    {
        private PbField<BoolValue> _inner;
        public BoolField(string name)
        {
            _inner = new PbField<BoolValue>(name);
        }
        
        public void SetValue(bool value)
        {
            var task = SetAsync(value);
            task.Wait();
        }

        public bool GetValue()
        {
            var task = GetAsync();
            task.Wait();
            return task.Result;
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
        
        public void SetValue(uint value)
        {
            var task = SetAsync(value);
            task.Wait();
        }

        public uint GetValue()
        {
            var task = GetAsync();
            task.Wait();
            return task.Result;
        }
        
        public async Task SetAsync(uint value)
        {
            await _inner.SetAsync(new UInt32Value { Value = value });
        }
        public async Task<uint> GetAsync()
        {
            return (await _inner.GetAsync())?.Value ?? default(uint);
        }
    }

    public class Int32Field
    {
        private readonly PbField<SInt32Value> _inner;
        public Int32Field(string name)
        {
            _inner = new PbField<SInt32Value>(name);
        }
        
        public void SetValue(int value)
        {
            var task = SetAsync(value);
            task.Wait();
        }

        public int GetValue()
        {
            var task = GetAsync();
            task.Wait();
            return task.Result;
        }
        
        public async Task SetAsync(int value)
        {
            await _inner.SetAsync(new SInt32Value { Value = value });
        }
        
        public async Task<int> GetAsync()
        {
            return (await _inner.GetAsync())?.Value ?? 0;
        }
    }

    public class UInt64Field
    {
        private readonly PbField<UInt64Value> _inner;
        public UInt64Field(string name)
        {
            _inner = new PbField<UInt64Value>(name);
        }
        
        public void SetValue(ulong value)
        {
            var task = SetAsync(value);
            task.Wait();
        }

        public ulong GetValue()
        {
            var task = GetAsync();
            task.Wait();
            return task.Result;
        }
        
        public async Task SetAsync(ulong value)
        {
            await _inner.SetAsync(new UInt64Value { Value = value });
        }
        
        public async Task<ulong> GetAsync()
        {
            return (await _inner.GetAsync())?.Value ?? 0;
        }
    }

    public class Int64Field
    {
        private readonly PbField<SInt64Value> _inner;
        public Int64Field(string name)
        {
            _inner = new PbField<SInt64Value>(name);
        }
        
        public void SetValue(long value)
        {
            var task = SetAsync(value);
            task.Wait();
        }

        public long GetValue()
        {
            var task = GetAsync();
            task.Wait();
            return task.Result;
        }
        
        public async Task SetAsync(long value)
        {
            await _inner.SetAsync(new SInt64Value { Value = value });
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
        
        public void SetValue(string value)
        {
            var task = SetAsync(value);
            task.Wait();
        }

        public string GetValue()
        {
            var task = GetAsync();
            task.Wait();
            return task.Result;
        }
        
        public async Task SetAsync(string value)
        {
            await _inner.SetAsync(new StringValue() { Value = value });
        }
        public async Task<string> GetAsync()
        {
            return (await _inner.GetAsync())?.Value;
        }
    }

    public class BytesField
    {
        private PbField<BytesValue> _inner;
        public BytesField(string name)
        {
            _inner = new PbField<BytesValue>(name);
        }
        
        public void SetValue(byte[] value)
        {
            var task = SetAsync(value);
            task.Wait();
        }

        public byte[] GetValue()
        {
            var task = GetAsync();
            task.Wait();
            return task.Result;
        }
        
        public async Task SetAsync(byte[] value)
        {
            await _inner.SetAsync(new BytesValue { Value = ByteString.CopyFrom(value) });
        }
        
        public async Task<byte[]> GetAsync()
        {
            return (await _inner.GetAsync())?.Value.ToByteArray() ?? new byte[] { };
        }
    }

}
