using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Configuration;
using InfluxData.Net.Common.Enums;
using InfluxData.Net.InfluxDb;
using InfluxData.Net.InfluxDb.Models;
using InfluxData.Net.InfluxDb.Models.Responses;

namespace AElf.Management.Helper
{
    public class InfluxDBHelper
    {
        private static readonly InfluxDbClient InfluxDb;

        static InfluxDBHelper()
        {
            InfluxDb = new InfluxDbClient(
                MonitorDatabaseConfig.Instance.Url, MonitorDatabaseConfig.Instance.Username,
                MonitorDatabaseConfig.Instance.Password, InfluxDbVersion.Latest);
        }

        public static async Task Set(string database, string measurement, Dictionary<string, object> fields, Dictionary<string, object> tags, DateTime timestamp)
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

        public static async Task<List<Serie>> Get(string database, string query)
        {
            var series = await InfluxDb.Client.QueryAsync(query, database);
            return series.ToList();
        }

        public static async Task<string> Version()
        {
            var pong = await InfluxDb.Diagnostics.PingAsync();
            return pong.Version;
        }

        public static async Task CreateDatabase(string database)
        {
            await InfluxDb.Database.CreateDatabaseAsync(database);
        }

        public static async Task DropDatabase(string database)
        {
            await InfluxDb.Database.DropDatabaseAsync(database);
        }
    }
}