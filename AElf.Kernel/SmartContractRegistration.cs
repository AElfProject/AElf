using System.IO;

namespace AElf.Kernel
{
    public class SmartContractRegistration 
    {

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

        public Hash Hash { get; set; }

        public byte[] Bytes { get; set; }
        
        public ulong IncrementId { get; set; }
        
        public Hash BaseAccount { get; set; }
    }
}