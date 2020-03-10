using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AElf.CSharp.CodeOps.Policies;
using AElf.CSharp.CodeOps.Validators;
using AElf.CSharp.CodeOps.Validators.Assembly;
using AElf.CSharp.CodeOps.Validators.Whitelist;
using AElf.Kernel.CodeCheck.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using Mono.Cecil;


namespace AElf.CSharp.CodeOps
{
    public class ContractAuditor : IContractAuditor
    {
        readonly AbstractPolicy _defaultPolicy = new DefaultPolicy();
        readonly AbstractPolicy _priviligePolicy = new PrivilegePolicy();
        
        readonly AcsValidator _acsValidator = new AcsValidator();

        public int Category { get; } = 0;

        public ContractAuditor(IEnumerable<string> blackList, IEnumerable<string> whiteList)
        {
            // Allow custom whitelisting / blacklisting namespaces, only for privilege policy
            whiteList?.ToList().ForEach(nm => _priviligePolicy.Whitelist.Namespace(nm, Permission.Allowed));
            blackList?.ToList().ForEach(nm => _priviligePolicy.Whitelist.Namespace(nm, Permission.Denied));
        }

        public void Audit(byte[] code, RequiredAcs requiredAcs)
        {
            AuditWithPolicy(code, requiredAcs, _priviligePolicy);
        }

        private IEnumerable<ValidationResult> ValidateMethodsInType(AbstractPolicy policy, TypeDefinition type)
        {
            var findings = new List<ValidationResult>();
            
            foreach (var method in type.Methods)
            {
                findings.AddRange(policy.MethodValidators.SelectMany(v => v.Validate(method)));
            }
            
            foreach (var nestedType in type.NestedTypes)
            {
                findings.AddRange(ValidateMethodsInType(policy, nestedType));
            }

            return findings;
        }

        private void AuditWithPolicy(byte[] code, RequiredAcs requiredAcs, AbstractPolicy policy)
        {
            var findings = new List<ValidationResult>();

            var modDef = ModuleDefinition.ReadModule(new MemoryStream(code));

            // Check against whitelist
            findings.AddRange(policy.Whitelist.Validate(modDef));

            // Run module validators
            findings.AddRange(policy.ModuleValidators.SelectMany(v => v.Validate(modDef)));
            
            var asm = Assembly.Load(code);
            // Run assembly validators (run after module validators since we invoke BindService method below)
            findings.AddRange(policy.AssemblyValidators.SelectMany(v => v.Validate(asm)));

            // Run method validators
            foreach (var type in modDef.Types)
            {
                findings.AddRange(ValidateMethodsInType(policy, type));
            }
            
            // Perform ACS validation
            if (requiredAcs != null)
                findings.AddRange(_acsValidator.Validate(asm, requiredAcs));
            
            if (findings.Count > 0)
            {
                throw new CSharpInvalidCodeException(
                    $"Contract code did not pass audit. Audit failed for contract: {modDef.Assembly.MainModule.Name}\n" +
                    string.Join("\n", findings), findings);
            }
        }
    }
}
