using System;
using System.Collections.Generic;

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
            _typeDic.Add("keyvalue", DatabaseType.KeyValue);
            //_typeDic.Add("redis", DatabaseType.Redis);
            _typeDic.Add("ssdb", DatabaseType.Ssdb);
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