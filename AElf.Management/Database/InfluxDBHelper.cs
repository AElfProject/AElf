using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InfluxDB.Net;
using InfluxDB.Net.Infrastructure.Influx;
using InfluxDB.Net.Models;

namespace AElf.Management.Database
{
    public class InfluxDBHelper
    {

        private static readonly InfluxDb _client;

        static InfluxDBHelper()
        {
            _client=new InfluxDb("http://localhost:8086","root", "root");
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

        public static async Task Get(string database, string query)
        {
            var result = await _client.QueryAsync(database, query);
        }

        public static async void Ping()
        {
            Pong pong =await _client.PingAsync();
        }
        
        public static string Version()
        {
            var version = _client.GetClientVersion();

            return version.ToString();
        }
    }
}