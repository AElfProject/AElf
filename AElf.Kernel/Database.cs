using System.Collections.Generic;

namespace AElf.Kernel
{
    /// <summary>
    /// Temporary Database.
    /// </summary>
    public class Database
    {
        private static readonly Dictionary<IHash, byte[]> Data = new Dictionary<IHash, byte[]>();
        
        #region Get
        public static byte[] Select(IHash address)
        {
            return address == null ? null : (Data.TryGetValue(address, out var result) ? result : null);
        }
        #endregion

        #region Set
        public static void Insert(IHash address, byte[] serialized)
        {
            Data[address] = serialized;
        }
        #endregion
    }
}
