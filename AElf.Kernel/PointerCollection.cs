using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    public class PointerCollection : IPointerCollection
    {
        private readonly Dictionary<Hash, Hash> _dictionary = new Dictionary<Hash, Hash>();
        
        public Task UpdateAsync(Hash pathHash, Hash pointerHash)
        {
            _dictionary[pathHash] = pointerHash;
            return Task.CompletedTask;
        }

        public Task<Hash> GetAsync(Hash pathHash)
        {
            if (_dictionary.TryGetValue(pathHash, out var pointerHash))
            {
                return Task.FromResult(pointerHash);
            }

            UpdateAsync(pathHash, Hash.Zero);
            return Task.FromResult(Hash.Zero);
        }
    }
}