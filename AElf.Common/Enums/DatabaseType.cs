using System;
using System.Collections.Generic;

namespace AElf.Common.Enums
{
    public enum DatabaseType
    {
        InMemory,
        Redis,
        Ssdb
    }

    public static class DatabaseTypeHelper
    {
        private static readonly Dictionary<string, DatabaseType> TypeDic = new Dictionary<string, DatabaseType>();

        static DatabaseTypeHelper()
        {
            var dbType = typeof(DatabaseType);
            var dbNames = Enum.GetNames(dbType);
            var dbValues = Enum.GetValues(dbType);
            for (var i = 0; i < dbNames.Length; ++i)
            {
                TypeDic.Add(dbNames[i].ToLower(), (DatabaseType) dbValues.GetValue(i));
            }
        }

        public static DatabaseType GetType(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return DatabaseType.Ssdb;
            var typeLower = type.ToLower();
            if (!TypeDic.ContainsKey(typeLower))
                throw new ArgumentException("Database type is incorrect.");

            return TypeDic[typeLower];
        }
    }
}