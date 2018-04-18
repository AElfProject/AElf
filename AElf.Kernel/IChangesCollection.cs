using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IChangesCollection : ICloneable
    {
        Task InsertAsync(Hash path, Change change);

        Task<Change> GetAsync(Hash path);

        Task<List<Change>> GetChangesAsync();

        Task<List<Hash>> GetChangedPathHashesAsync();

        Task<Dictionary<Hash, Change>> GetChangesDictionary();

        Task Clear();
    }
}