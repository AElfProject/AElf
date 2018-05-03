using System;

namespace AElf.Kernel
{
    public partial class Fee : IComparable<Fee>
    {
        /*/// <summary>
        /// unit type
        /// </summary>
        public FeeUnit Unit { get; }*/
        
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
      
        public int CompareTo(Fee other)
        {
            throw new NotImplementedException();
        }
    }

}