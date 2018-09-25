using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Configuration;
using InfluxDB.Net;
using InfluxDB.Net.Models;

namespace AElf.Management.Helper
{
    public class InfluxDBHelper
    {

        private static readonly InfluxDb _client;

        static InfluxDBHelper()
        {
            _client = new InfluxDb(MonitorDatabaseConfig.Instance.Url, MonitorDatabaseConfig.Instance.Username, MonitorDatabaseConfig.Instance.Password);
        }

        public static async Task Set(string database, string measurement, Dictionary<string, object> fields, Dictionary<string, object> tags, DateTime timestamp)
        {
            var point = new Point();
//            point.Measurement = "disk_free";
//            point.Fields=new Dictionary<string, object>{{ "value", 345356677f }};
//            point.Tags=new Dictionary<string, object>{{ "hostname", "testhost2" },{ "port", "8082" }};
//            point.Timestamp = DateTime.Now;
//            InfluxDbApiResponse writeResponse = await _client.WriteAsync("test", point);

            point.Measurement = measurement;
            point.Fields = fields;
            if (tags != null)
            {
                point.Tags = tags;
            }
            point.Timestamp = timestamp;
            await _client.WriteAsync(database, point);
        }

        public static List<Serie> Get(string database, string query)
        {
            var result = _client.QueryAsync(database, query);
            return result.Result;
        }
        
        public static string Version()
        {
            var version = _client.GetClientVersion();

            return version.ToString();
        }

        public static void AddDatabase(string database)
        {
            _client.CreateDatabaseAsync(database);
        }
    }
}