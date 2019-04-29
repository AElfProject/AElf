using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Runtime.CSharp.Policies;
using AElf.Runtime.CSharp.Validators;
using Mono.Cecil;

namespace AElf.Runtime.CSharp
{
    public class ContractAuditor
    {
        AbstractPolicy policy = new DefaultPolicy();

        public void Audit(byte[] code, bool priority)
        {
            var errors = new List<ValidationResult>();
            var modDef = ModuleDefinition.ReadModule(new MemoryStream(code));
            
            // Check against whitelist
            errors.AddRange(policy.Whitelist.Validate(modDef));
            
            // Run module validators
            errors.AddRange(policy.ModuleValidators.SelectMany(v => v.Validate(modDef)));
            
            // Run method validators
            foreach (var typ in modDef.Types)
            {
                foreach (var method in typ.Methods)
                {
                    errors.AddRange(policy.MethodValidators.SelectMany(v => v.Validate(method)));    
                }
            }

            if (errors.Count > 0)
            {
                throw new InvalidCodeException("Audit failed for contract: " 
                                                       + modDef.Assembly.MainModule.Name + "\r\n"
                                                       + string.Concat("\r\n", errors.Select(e => e.ToString()).Distinct().ToArray()));
            }
        }
    }
}
