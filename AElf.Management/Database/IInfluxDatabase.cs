using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InfluxData.Net.InfluxDb.Models.Responses;

namespace AElf.Management.Database
{
    public interface IInfluxDatabase
    {
        Task Set(string database, string measurement, Dictionary<string, object> fields,
            Dictionary<string, object> tags, DateTime timestamp);

        Task<List<Serie>> Get(string database, string query);

        Task<string> Version();

        Task CreateDatabase(string database);

        Task DropDatabase(string database);
    }
}