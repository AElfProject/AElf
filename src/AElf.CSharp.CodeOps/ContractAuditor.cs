using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AElf.CSharp.CodeOps.Policies;
using AElf.CSharp.CodeOps.Validators;
using AElf.CSharp.CodeOps.Validators.Assembly;
using AElf.CSharp.CodeOps.Validators.Whitelist;
using Mono.Cecil;


namespace AElf.CSharp.CodeOps
{
    public class ContractAuditor
    {
        readonly AbstractPolicy _defaultPolicy = new DefaultPolicy();
        readonly AbstractPolicy _priviligePolicy = new PrivilegePolicy();
        
        readonly AcsValidator _acsValidator = new AcsValidator();

        public ContractAuditor(IEnumerable<string> blackList, IEnumerable<string> whiteList)
        {
            // Allow custom whitelisting / blacklisting namespaces, only for privilege policy
            whiteList?.ToList().ForEach(nm => _priviligePolicy.Whitelist.Namespace(nm, Permission.Allowed));
            blackList?.ToList().ForEach(nm => _priviligePolicy.Whitelist.Namespace(nm, Permission.Denied));
        }

        public void Audit(byte[] code, RequiredAcsDto requiredAcs, bool priority)
        {
            var findings = new List<ValidationResult>();
            var asm = Assembly.Load(code);
            var modDef = ModuleDefinition.ReadModule(new MemoryStream(code));
            var policy = priority ? _priviligePolicy : _defaultPolicy;
            
            // Check against whitelist
            findings.AddRange(policy.Whitelist.Validate(modDef));

            // Run module validators
            findings.AddRange(policy.ModuleValidators.SelectMany(v => v.Validate(modDef)));
            
            // Run assembly validators (run after module validators since we invoke BindService method below)
            findings.AddRange(policy.AssemblyValidators.SelectMany(v => v.Validate(asm)));

            // Run method validators
            foreach (var typ in modDef.Types)
            {
                foreach (var method in typ.Methods)
                {
                    findings.AddRange(policy.MethodValidators.SelectMany(v => v.Validate(method)));
                }
            }
            
            // Perform ACS validation
            findings.AddRange(_acsValidator.Validate(asm, requiredAcs));

            if (findings.Count > 0)
            {
                throw new InvalidCodeException(
                    $"Contract code did not pass audit. Audit failed for contract: {modDef.Assembly.MainModule.Name}\n" +
                    string.Join("\n", findings), findings);
            }
        }
    }
}
