namespace AElf.Kernel
{
    
    
    public class SmartContractRegistration
    {
        
        /// <summary>
        /// 0: Smart Contract Zero
        /// 1: C# bytes
        /// 2: Javascript
        /// </summary>
        public int Category { get; set; }
        
        public IHash Hash { get; set; }
        
        public byte[] Bytes { get; set; }
    }
}