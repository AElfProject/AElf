using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using AElf.Types.CSharp;
using Mono.Cecil;

namespace AElf.ABI.CSharp
{
    public class Generator
    {
        public static Module GetABIModule(byte[] code, string name = null)
        {
            var module = new Module();
            using (var monoModule = ModuleDefinition.ReadModule(new MemoryStream(code)))
            {
                Container container = new Container("AElf.Sdk.CSharp.CSharpSmartContract", "AElf.Sdk.CSharp.Event",
                    typeof(UserType).FullName);
                foreach (var t in monoModule.GetTypes())
                {
                    container.AddType(t);
                }
            
                var contractTypePath = container.GetSmartContractTypePath(name);
                module.Name = name ?? contractTypePath.Last().FullName;
                module.Methods.AddRange(GetMethods(contractTypePath));
                module.Events.AddRange(GetEvents(container));
                module.Types_.AddRange(GetTypes(container));

                return module;                
            }
        }

        private static IEnumerable<Method> GetMethods(IEnumerable<TypeDefinition> contractTypePath)
        {
            List<Method> methods = new List<Method>();
            foreach (var sc in contractTypePath)
            {
                methods.AddRange(GetMethodsFromType(sc));
            }

            return methods;
        }

        private static IEnumerable<Method> GetMethodsFromType(TypeDefinition type)
        {
            List<Method> methods = new List<Method>();
            foreach (var m in type.Methods)
            {
                if (!m.IsPublic)
                    continue;
                if (m.Name == ".ctor")
                    continue;
                var method = new Method()
                {
                    Name = m.Name
                };
                foreach (var p in m.Parameters)
                {
                    method.Params.Add(new Field()
                    {
                        Name = p.Name,
                        Type = p.ParameterType.FullName.ToShorterName()
                    });
                }

                var rt = m.ReturnType.FullName;
                method.IsAsync = rt.StartsWith("System.Threading.Tasks.Task");
                method.IsView = m.CustomAttributes.Any(x => x.AttributeType.Name == "ViewAttribute");
                method.ReturnType = rt == "System.Threading.Tasks.Task"
                    ? "void"
                    : rt.Replace("System.Threading.Tasks.Task`1<", "").Replace(">", "");
                method.ReturnType = method.ReturnType.ToShorterName();
                methods.Add(method);
            }

            return methods;
        }

        private static IEnumerable<Event> GetEvents(Container container)
        {
            List<Event> events = new List<Event>();
            foreach (var e in container.Events)
            {
                var event_ = new Event()
                {
                    Name = e.FullName
                };
                foreach (var f in e.Fields)
                {
                    var field = new Field()
                    {
                        Name = f.Name.Replace("<", "").Replace(">k__BackingField", ""),
                        Type = f.FieldType.FullName.ToShorterName()
                    };
                    if (f.CustomAttributes.Any(x => x.AttributeType.Name == "IndexedAttribute"))
                    {
                        event_.Indexed.Add(field);
                    }
                    else
                    {
                        event_.NonIndexed.Add(field);
                    }
                }

                events.Add(event_);
            }

            return events;
        }

        private static IEnumerable<Type> GetTypes(Container container)
        {
            List<Type> types = new List<Type>();
            foreach (var t in container.Types)
            {
                var type_ = new Type()
                {
                    Name = t.FullName
                };
                foreach (var f in t.Fields)
                {
                    type_.Fields.Add(new Field()
                    {
                        Name = f.Name.Replace("<", "").Replace(">k__BackingField", ""),
                        Type = f.FieldType.FullName.ToShorterName()
                    });
                }

                types.Add(type_);
            }

            return types;
        }
    }
}