using System;
using System.Threading.Tasks;
using LiteDB;

namespace AElf.Kernel
{
    public class LiteDBDataProvider:IAccountDataProvider
    {
        private struct Record
        {
            public IHash Key { get; set; }
            public byte[] Value { get; set; }
        }

        private LiteDatabase db;


        public LiteDBDataProvider(string path)
        {
            this.db = new LiteDatabase(@"path");
        }

        Task<ISerializable> IAccountDataProvider.GetAsync(IHash key)
        {
            var c = db.GetCollection<ISerializable>("accounts");
            c.Query(x => x.Key.Contains(key));
        }

        IHash<IMerkleTree<ISerializable>> IAccountDataProvider.GetDataMerkleTreeRoot()
        {
            throw new NotImplementedException();
        }

        Task IAccountDataProvider.SetAsync(IHash key, ISerializable obj)
        {
            throw new NotImplementedException();
        }
    }
}
