using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.CSharp.CodeOps.Policies;
using AElf.CSharp.CodeOps.Validators;
using AElf.CSharp.CodeOps.Validators.Whitelist;
using Mono.Cecil;


namespace AElf.CSharp.CodeOps
{
    public class ContractAuditor
    {
        readonly AbstractPolicy _defaultPolicy = new DefaultPolicy();
        readonly AbstractPolicy _priviligePolicy = new PrivilegePolicy();

        public ContractAuditor(IEnumerable<string> blackList, IEnumerable<string> whiteList)
        {
            // Allow custom whitelisting / blacklisting namespaces, only for privilege policy
            whiteList?.ToList().ForEach(nm => _priviligePolicy.Whitelist.Namespace(nm, Permission.Allowed));
            blackList?.ToList().ForEach(nm => _priviligePolicy.Whitelist.Namespace(nm, Permission.Denied));
        }

        public void Audit(byte[] code, bool priority)
        {
            var findings = new List<ValidationResult>();
            var modDef = ModuleDefinition.ReadModule(new MemoryStream(code));
            var policy = priority ? _priviligePolicy : _defaultPolicy;

            // Do not validate further if contract assembly is not verifiable
            if (findings.Count == 0)
            {
                // Check against whitelist
                findings.AddRange(policy.Whitelist.Validate(modDef));

                // Run module validators
                findings.AddRange(policy.ModuleValidators.SelectMany(v => v.Validate(modDef)));

                // Run method validators
                foreach (var typ in modDef.Types)
                {
                    #if UNIT_TEST
                    // Skip validation if it is a coverlet injected type, only in unit test mode
                    if (typ.Namespace.StartsWith("Coverlet."))
                        continue;
                    #endif
                    foreach (var method in typ.Methods)
                    {
                        findings.AddRange(policy.MethodValidators.SelectMany(v => v.Validate(method)));
                    }
                }
            }

            if (findings.Count > 0)
            {
                throw new InvalidCodeException($"Audit failed for contract: {modDef.Assembly.MainModule.Name }\n"
                                                        + string.Join("\n", findings)
                                                        , findings);
            }
        }
    }
}
