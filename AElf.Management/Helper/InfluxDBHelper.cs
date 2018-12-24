using System;
using System.Collections.Generic;
using System.Linq;
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

        public static void Set(string database, string measurement, Dictionary<string, object> fields, Dictionary<string, object> tags, DateTime timestamp)
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

            InfluxDb.Client.WriteAsync(point, database);
        }

        public static List<Serie> Get(string database, string query)
        {
            var series = InfluxDb.Client.QueryAsync(query, database).Result;
            return series.ToList();
        }

        public static string Version()
        {
            var pong = InfluxDb.Diagnostics.PingAsync();
            return pong.Result.Version;
        }

        public static void CreateDatabase(string database)
        {
            InfluxDb.Database.CreateDatabaseAsync(database);
        }

        public static void DropDatabase(string database)
        {
            InfluxDb.Database.DropDatabaseAsync(database);
        }
    }
}