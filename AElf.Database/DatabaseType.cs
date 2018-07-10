using System;
using System.Collections.Generic;
using System.Linq;

namespace AElf.Database
{
    public enum DatabaseType
    {
        KeyValue,

        Redis,

        Ssdb
    }

    public static class DatabaseTypeHelper
    {
        private static readonly Dictionary<string, DatabaseType> _typeDic = new Dictionary<string, DatabaseType>();

        static DatabaseTypeHelper()
        {
            var dbType = typeof(DatabaseType);
            var dbNames = Enum.GetNames(dbType);
            var dbValues = Enum.GetValues(dbType);
            for (var i = 0; i < dbNames.Length; ++i)
            {
                _typeDic.Add(dbNames[i].ToLower(), (DatabaseType) dbValues.GetValue(i));
            }
        }

        public static DatabaseType GetType(string type)
        {
            var dbType = DatabaseType.Ssdb;
            if (!string.IsNullOrWhiteSpace(type))
            {
                var typeLower = type.ToLower();
                if (_typeDic.ContainsKey(typeLower))
                {
                    dbType = _typeDic[typeLower];
                }
                else
                {
                    throw new ArgumentException("Database type is incorrect.");
                }
            }

            return dbType;
        }
    }
}