using System;
using System.Threading.Tasks;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AElf.Types.CSharp;

namespace AElf.Sdk.CSharp.Types
{
    public class UserTypeField<T> where T : UserType
    {
        public string _name;
        public UserTypeField(string name)
        {
            _name = name;
        }

        public async Task SetAsync(T value)
        {
            if (value != null)
            {
                await Api.GetDataProvider("").SetAsync(_name.CalculateHash(), value.Pack().ToByteArray());
            }
        }

        public async Task<T> GetAsync()
        {
            var obj = (T)Activator.CreateInstance(typeof(T));
            byte[] bytes = await Api.GetDataProvider("").GetAsync(_name.CalculateHash());
            var userTypeValue = Api.Serializer.Deserialize<UserTypeHolder>(bytes);
            obj.Unpack(userTypeValue);
            return obj;
        }
    }
}
