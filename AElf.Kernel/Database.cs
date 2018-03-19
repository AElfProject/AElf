using System.Collections.Generic;

namespace AElf.Kernel
{
    /// <summary>
    /// Temporary Database.
    /// </summary>
    public class Database
    {
        private static readonly Dictionary<IHash, ISerializable> Data = new Dictionary<IHash, ISerializable>();
        
        #region Get
        public static ISerializable Select(IHash address)
        {
            return address == null ? null : (Data.TryGetValue(address, out var result) ? result : null);
        }
        #endregion

        #region Set
        public static void Insert(IHash address, ISerializable serialized)
        {
            Data[address] = serialized;
        }
        #endregion
    }
}
