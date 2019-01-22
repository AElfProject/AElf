using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Database.RedisProtocol;
using Microsoft.Extensions.Options;

namespace AElf.Database
{
    public class SsdbDatabase<TKeyValueDbContext> : RedisDatabase<TKeyValueDbContext>
        where TKeyValueDbContext:KeyValueDbContext<TKeyValueDbContext>
    {
        public SsdbDatabase(KeyValueDatabaseOptions<TKeyValueDbContext> options) : base(options)
        {
        }
    }
}