using System.Collections.Generic;
using ProtoBuf;

namespace AElf.CLI.Data.Protobuf
{
    /*message Field
    {
        string Type = 1;
        string Name = 2;
    } */
    
    [ProtoContract]
    public class Field
    {
        [ProtoMember(1)] 
        public string Type {get; set; }
        
        [ProtoMember(2)] 
        public string Name {get; set; }
    }
    
    /*message Type
    {
        string Name = 1;
        repeated Field Fields = 2;
    }*/
    
    [ProtoContract]
    public class Type
    {
        [ProtoMember(1)] 
        public string Name {get; set; }
        
        [ProtoMember(2)]
        public List<Field> Fields { get; set; }
    }
    
    /*message Event
    {
        string Name = 1;
        repeated Field Indexed = 2;
        repeated Field NonIndexed = 3;
    }*/
    
    [ProtoContract]
    public class Event
    {
        [ProtoMember(1)] 
        public string Name {get; set; }
        
        [ProtoMember(2)]
        public List<Field> Indexed { get; set; }
        
        [ProtoMember(3)]
        public List<Field> NonIndexed { get; set; }
    }
    
    /*
     message Method
    {
        string Name = 1;
        repeated Field Params = 2;
        string ReturnType = 3;
        bool IsAsync = 4;
    }*/
    
    [ProtoContract]
    public class Method
    {
        [ProtoMember(1)] 
        public string Name {get; set; }
        
        [ProtoMember(2)]
        public List<Field> Params { get; set; }
        
        [ProtoMember(3)] 
        public string ReturnType {get; set; }
        
        [ProtoMember(4)]
        public bool IsAsync { get; set; }
    }
    
    /*message Module
    {
        string Name = 1;
        repeated Method Methods = 2;
        repeated Event Events = 3;
        repeated Type Types = 4;
    }*/
    
    [ProtoContract]
    public class Module
    {
        [ProtoMember(1)] 
        public string Name {get; set; }
        
        [ProtoMember(2)]
        public List<Method> Methods { get; set; }
        
        [ProtoMember(3)]
        public List<Event> Events { get; set; }
        
        [ProtoMember(4)]
        public List<Type> Types { get; set; }
    }
}