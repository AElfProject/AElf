﻿using AElf.Kernel.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    /// <summary>
    /// Temporary Database.
    /// </summary>
    public static class Database
    {
        private static readonly Dictionary<IHash, ISerializable> Data = new Dictionary<IHash, ISerializable>();
        
        #region Get
        public static ISerializable Select(IHash address)
        {
            return Data.TryGetValue(address, out var result) ? result : null;
        }
        #endregion

        #region Set
        public static bool Insert(IHash address, ISerializable serialized)
        {
            Data[address] = serialized;
            return true;
        }
        #endregion
    }
}
