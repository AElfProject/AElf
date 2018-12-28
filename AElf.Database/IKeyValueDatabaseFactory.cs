using System.Xml;

namespace AElf.Database
{
    public interface IKeyValueDatabaseFactory
    {
        IKeyValueDatabase Create(string connectionName);
    }
}