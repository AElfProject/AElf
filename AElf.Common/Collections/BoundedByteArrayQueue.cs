using System.Collections.Generic;

namespace AElf.Common.Collections
{
    public class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[] x, byte[] y)
        {
            return x.BytesEqual(y);
        }

        public int GetHashCode(byte[] obj)
        {
            return obj.GetHashCode();
        }
    }
    
    public class BoundedByteArrayQueue : FixedSizedQueue<byte[]>
    {
        private ByteArrayComparer comparer = new ByteArrayComparer();
        
        public BoundedByteArrayQueue(int sizeLimit) : base(sizeLimit)
        {
        }
        
        public override bool Contains(byte[] element)
        {
            return base.Contains(element, comparer);
        }
    }
}