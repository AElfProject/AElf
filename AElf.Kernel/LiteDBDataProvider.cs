using System;
using System.Threading.Tasks;
using LiteDB;

namespace AElf.Kernel
{
    public class LiteDBDataProvider:IAccountDataProvider
    {
        class Record:ISerializable
        {
            public IHash Key { get; set; }
            public byte[] Value { get; set; }

            public byte[] Serialize()
            {
                return Value;
            }
        }

        private LiteDatabase db;


        public LiteDBDataProvider(string path)
        {
            this.db = new LiteDatabase(@"path");
        }

        Task<ISerializable> IAccountDataProvider.GetAsync(IHash key)
        {
            var c = this.db.GetCollection<Record>("data");
            var result = c.Find(x => x.Key.Equals(key));
            return result;
        }

        IHash<IMerkleTree<ISerializable>> IAccountDataProvider.GetDataMerkleTreeRoot()
        {
            var c = this.db.GetCollection<Record>("accounts");
            var result = c.Find(x => x.Key.Equals("root"));
            return null;        
        }

        Task IAccountDataProvider.SetAsync(IHash key, ISerializable obj)
        {
            throw new NotImplementedException();
        }
    }
}
