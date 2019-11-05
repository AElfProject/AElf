using System;
using System.Web;

namespace AElf.Database
{
    public enum DatabaseType
    {
        Redis,
        SSDB
    }

    public class DatabaseEndpoint
    {
        public DatabaseType DatabaseType { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public int DatabaseNumber { get; set; }

        public static DatabaseEndpoint ParseFromConnectionString(string connectionString)
        {
            var databaseEndpoint = new DatabaseEndpoint();
            var url = new Uri(connectionString);

            databaseEndpoint.Host = url.Host;
            databaseEndpoint.Port = url.Port;
            Enum.TryParse(url.Scheme, ignoreCase: true, out DatabaseType databaseType);
            databaseEndpoint.DatabaseType = databaseType;

            var valueCollection = HttpUtility.ParseQueryString(url.Query);
            var databaseNumber = valueCollection.Get("db");
            if (databaseNumber != null)
                databaseEndpoint.DatabaseNumber = int.Parse(databaseNumber);

            return databaseEndpoint;
        }
    }
}