using System;
using System.Collections.Generic;

namespace AElf.Common.Enums
{
    public enum ConsensusType
    {
        PoW = 0,
        // ReSharper disable once InconsistentNaming
        AElfDPoS = 1,
        // ReSharper disable once InconsistentNaming
        PoTC = 2,//Proof of Transaction Count. Used for testing execution performance of single node.
        // ReSharper disable once InconsistentNaming
        SingleNode = 3
    }
    
    public static class ConsensusTypeHelper
    {
        private static readonly Dictionary<string, ConsensusType> TypeDic = new Dictionary<string, ConsensusType>();

        static ConsensusTypeHelper()
        {
            var type = typeof(ConsensusType);
            var names = Enum.GetNames(type);
            var values = Enum.GetValues(type);
            for (var i = 0; i < names.Length; ++i)
            {
                TypeDic.Add(names[i].ToLower(), (ConsensusType) values.GetValue(i));
            }
        }

        public static ConsensusType GetType(string type)
        {
            var typeLower = type.ToLower();
            if (!TypeDic.ContainsKey(typeLower))
                throw new ArgumentException("Consensus type is incorrect.");

            return TypeDic[typeLower];
        }
    }
}