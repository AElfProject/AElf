using System.IO;

namespace AElf.Kernel
{
    public class SmartContractRegistration : ISerializable
    {
        public SmartContractRegistration(ISerializable serializable)
        {
            throw new System.NotImplementedException();
        }

        public SmartContractRegistration()
        {
            
        }

        /// <summary>
        /// 0: Smart Contract Zero
        /// 1: C# bytes
        /// 2: Javascript
        /// </summary>
        public int Category { get; set; }
        
        /// <summary>
        /// Smart contract name for recognition just like hash calculating
        /// </summary>
        //public string Name { get; set; }

        public IHash<IAccount> Hash { get; set; }

        public byte[] Bytes { get; set; }

        public byte[] Serialize()
        {
            var ms = new MemoryStream();
            using (var sm = new BinaryWriter(ms))
            {
            }
            throw new System.NotImplementedException();
        }
        
        public ulong IncrementId { get; set; }
        
        public IHash<IAccount> BaseAccount { get; set; }
    }
}