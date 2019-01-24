using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InfluxData.Net.Common.Enums;
using InfluxData.Net.InfluxDb;
using InfluxData.Net.InfluxDb.Models;
using InfluxData.Net.InfluxDb.Models.Responses;
using Microsoft.Extensions.Options;

namespace AElf.Management.Database
{
    public class InfluxDatabase : IInfluxDatabase
    {
        private readonly InfluxDbClient InfluxDb;

        public InfluxDatabase(IOptionsSnapshot<MonitorDbOptions> options)
        {
            InfluxDb = new InfluxDbClient(options.Value.Url, options.Value.Username, options.Value.Password,
                InfluxDbVersion.Latest);
        }

        public async Task Set(string database, string measurement, Dictionary<string, object> fields,
            Dictionary<string, object> tags, DateTime timestamp)
        {
            var point = new Point
            {
                Name = measurement,
                Fields = fields,
                Timestamp = timestamp
            };
            if (tags != null)
            {
                point.Tags = tags;
            }

            await InfluxDb.Client.WriteAsync(point, database);
        }

        public async Task<List<Serie>> Get(string database, string query)
        {
            var series = await InfluxDb.Client.QueryAsync(query, database);
            return series.ToList();
        }

        public async Task<string> Version()
        {
            var pong = await InfluxDb.Diagnostics.PingAsync();
            return pong.Version;
        }

        public async Task CreateDatabase(string database)
        {
            await InfluxDb.Database.CreateDatabaseAsync(database);
        }

        public async Task DropDatabase(string database)
        {
            await InfluxDb.Database.DropDatabaseAsync(database);
        }
    }
}