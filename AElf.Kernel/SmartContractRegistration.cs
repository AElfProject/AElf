﻿namespace AElf.Kernel
{
    public class SmartContractRegistration : ISerializable
    {
        /// <summary>
        /// 0: Smart Contract Zero
        /// 1: C# bytes
        /// 2: Javascript
        /// </summary>
        public int Category { get; set; }
        
        /// <summary>
        /// Smart contract name for recognition just like hash calculating
        /// </summary>
        public string Name { get; set; }

        public IHash Hash { get; set; }

        public byte[] Bytes { get; set; }

        public byte[] Serialize()
        {
            throw new System.NotImplementedException();
        }
    }
}