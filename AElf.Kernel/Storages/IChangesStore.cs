using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IChangesStore : ICloneable
    {
        Task InsertAsync(Hash path, Change change);

        Task<Change> GetAsync(Hash path);

        Task<List<Change>> GetChangesAsync();

        Task<List<Hash>> GetChangedPathsAsync();
    }
}