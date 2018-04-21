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
        public ulong Amount{
            get;
        }
        
        public static bool operator <(Fee left, Fee right)
        {
            if (left == null)
                throw new ArgumentNullException("left");
            if (right == null)
                throw new ArgumentNullException("right");
            // TODO: need compare unit
            return left.Amount < right.Amount;
        }

        public static bool operator >(Fee left, Fee right)
        {
            if (left == null)
                throw new ArgumentNullException("left");
            if (right == null)
                throw new ArgumentNullException("right");
            // TODO: need compare unit
            return left.Amount > right.Amount;
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