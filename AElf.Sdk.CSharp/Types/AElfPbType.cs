using System;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using Google.Protobuf;

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
}
