using System;
using System.Collections.Generic;

namespace AElf.Common.Enums
{
    // ReSharper disable InconsistentNaming
    public enum ConsensusType
    {
        PoW = 0,
        AElfDPoS = 1,
        SingleNode = 2
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