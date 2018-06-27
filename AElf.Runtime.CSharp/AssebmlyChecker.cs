using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Mono.Cecil;

namespace AElf.Runtime.CSharp
{
    public class AssemblyChecker
    {
        private readonly IEnumerable<Regex> _blackList;
        private readonly IEnumerable<Regex> _whiteList;
        private readonly string[] _systemWhiteList = new string[]
        {
            "System.Reflection.AssemblyCompanyAttribute".Replace(".", "\\."),
            "System.Reflection.AssemblyConfigurationAttribute".Replace(".", "\\."),
            "System.Reflection.AssemblyFileVersionAttribute".Replace(".", "\\."),
            "System.Reflection.AssemblyInformationalVersionAttribute".Replace(".", "\\."),
            "System.Reflection.AssemblyProductAttribute".Replace(".", "\\."),
            "System.Reflection.AssemblyTitleAttribute".Replace(".", "\\."),
            @"AElf\..+"
        };
        
        public AssemblyChecker(IEnumerable<string> blackList, IEnumerable<string> whiteList)
        {
            _blackList = (blackList ?? new string[0]).Select(x => new Regex(x)).ToList();
            _whiteList = (whiteList ?? new string[0]).Concat(_systemWhiteList).Select(x => new Regex(x)).ToList();
        }

        public List<TypeReference> GetBlackListedTypeReferences(ModuleDefinition moduleDefinition)
        {
            var blackListed = moduleDefinition.GetTypeReferences()
                .Where(x => _blackList.Any(y => y.IsMatch(x.FullName)))
                .Where(x => _whiteList.All(y => !y.IsMatch(x.FullName)))
                .Select(x => x).ToList();
            return blackListed;
        }
    }
}