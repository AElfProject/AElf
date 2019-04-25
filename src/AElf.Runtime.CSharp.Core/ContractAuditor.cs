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

        public IEnumerable<ValidationResult> Audit(byte[] code, bool priority)
        {
            var errors = new List<ValidationResult>();
            var modDef = ModuleDefinition.ReadModule(new MemoryStream(code));
            
            errors.AddRange(policy.ModuleValidators.SelectMany(v => v.Validate(modDef)));
            
            foreach (var typ in modDef.Types)
            {
                foreach (var method in typ.Methods)
                {
                    errors.AddRange(policy.MethodValidators.SelectMany(v => v.Validate(method)));    
                }
            }
            
            return errors;
        }
    }
}
