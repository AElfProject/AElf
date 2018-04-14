using System;

namespace AElf.Kernel.TxMemPool
{
    public class Fee : IEquatable<Fee>, IComparable<Fee>
    {
        /// <summary>
        /// unit type
        /// </summary>
        public FeeUnit Unit { get; }
        
        /// <summary>
        /// amount 
        /// </summary>
        public ulong Amount;

        public Fee(FeeUnit unit, ulong amount)
        {
            Unit = unit;
            Amount = amount;
        }

        public bool Equals(Fee other)
        {
            throw new NotImplementedException();
        }

        public int CompareTo(Fee other)
        {
            throw new NotImplementedException();
        }
    }

    public enum FeeUnit
    {
        
    }
}