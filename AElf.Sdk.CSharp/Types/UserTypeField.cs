using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using Google.Protobuf;
using AElf.Types.CSharp;

namespace AElf.Sdk.CSharp.Types
{
    public class UserTypeField<T> where T : UserType
    {
        private readonly string _name;

        public UserTypeField(string name)
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
                await Api.GetDataProvider("")
                    .SetAsync<UserTypeHolder>(Hash.FromString(_name), value.Pack().ToByteArray());
            }
        }

        public async Task<T> GetAsync()
        {
            var obj = (T) Activator.CreateInstance(typeof(T));
            byte[] bytes = await Api.GetDataProvider("").GetAsync<UserTypeHolder>(Hash.FromString(_name)) ??
                           new byte[0];
            var userTypeValue = Api.Serializer.Deserialize<UserTypeHolder>(bytes);
            obj.Unpack(userTypeValue);
            return obj;
        }
    }
}