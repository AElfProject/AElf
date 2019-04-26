using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AElf.Runtime.CSharp.Validators.Module
{
    public class AccessRule
    {
        private string name;
        private Regex pattern;
        private Permission permission;
        private Dictionary<string, Regex> except = new Dictionary<string, Regex>();

        public AccessRule(string name)
        {
            this.name = name;
            this.pattern = GetRegexPattern(name);
            this.permission = Permission.Disallowed;
        }

        private void CheckAccessConflict(string name)
        {
            if (!name.StartsWith(this.name))
            {
                throw new ConflictingAccessRuleException("Parent namespace " + this.name + " is parent of the exception rule " + name);
            }
        }

        public AccessRule Allow()
        {
            permission = Permission.Allowed;
            return this;
        }
        
        public AccessRule Except(string name)
        {
            CheckAccessConflict(name);
            except.Add(name, GetRegexPattern(name));
            return this;
        }

        public AccessRule Disallow()
        {
            permission = Permission.Disallowed;
            return this;
        }

        public bool IsAllowed(string name)
        {
            var allowed = permission == Permission.Allowed && pattern.IsMatch(name);

            if (except.Count <= 0) return allowed;
            
            foreach (var p in except)
            {
                if (p.Value.IsMatch(name))
                {
                    allowed = permission != Permission.Allowed;
                    break;
                }
            }

            return allowed;
        }

        public override string ToString()
        {
            return name;
        }

        private Regex GetRegexPattern(string name)
        {
            return new Regex($@"{name.Replace(".", "\\.")}.*");
        }
    }
    
    public enum Permission
    {
        Allowed,
        Disallowed
    }
    
    public class ConflictingAccessRuleException : Exception
    {
        public ConflictingAccessRuleException(string message) : base(message)
        {
        }
    }
}