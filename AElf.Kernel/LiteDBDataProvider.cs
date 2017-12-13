using System;
using System.Threading.Tasks;
using LiteDB;

namespace AElf.Kernel
{
    public class LiteDBDataProvider:IAccountDataProvider
    {
        struct Record:ISerializable
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

        async Task<ISerializable> IAccountDataProvider.GetAsync(IHash key)
        {
            var c = this.db.GetCollection<Record>("data");
            Task<ISerializable> task = new Task<ISerializable>(() => c.FindOne(x => x.Key.Equals(key)));
            task.Start();
            return await task;
        }

        IHash<IMerkleTree<ISerializable>> IAccountDataProvider.GetDataMerkleTreeRoot()
        {
            var c = this.db.GetCollection<Record>("merkleroot");
           // return c.FindOne(x => x.Key.Equals("root")); 
        }

        async Task IAccountDataProvider.SetAsync(IHash key, ISerializable obj)
        {
            var c = this.db.GetCollection<Record>("data");
            Task<bool> task = new Task<bool>(() => c.Upsert(new Record(){Key = key, Value = obj.Serialize()}));
            task.Start();
            await task;
            return;
        }
    }
}
