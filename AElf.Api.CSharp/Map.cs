using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Api.CSharp
{
    public class Map
    {
        private string _name;

        public Map(string name)
        {
            _name = name;
        }
        
        public async Task SetValueAsync(Hash keyHash, byte[] value)
        {
            await Api.GetDataProvider(_name).SetAsync(keyHash, value);
        }

        public async Task<byte[]> GetValue(Hash keyHash)
        {
            return await Api.GetDataProvider(_name).GetAsync(keyHash);
        }
    }
}