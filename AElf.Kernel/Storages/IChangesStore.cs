using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IChangesStore
    {
        Task InsertAsync(Hash path, Change change);

        Task<Change> GetAsync(Hash path);
    }
}