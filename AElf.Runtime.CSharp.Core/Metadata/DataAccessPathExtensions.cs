using AElf.Kernel.SmartContract.Metadata;

namespace AElf.Runtime.CSharp.Metadata
{
    public static class DataAccessPathExtensions
    {
        public static DataAccessPath GetSub(this DataAccessPath path, string subName,
            DataAccessPath.Types.AccessMode mode)
        {
            var dataAccessPath = path.Clone();
            dataAccessPath.Path.Add(subName);
            dataAccessPath.Mode = mode;
            return dataAccessPath;
        }

        public static DataAccessPath WithPrefix(this DataAccessPath original, string prefix)
        {
            var output = new DataAccessPath
            {
                Path = {prefix},
                Mode = original.Mode
            };
            output.Path.AddRange(original.Path);
            return output;
        }
    }
}