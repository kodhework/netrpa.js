using System; 
using System.Collections.Generic;
namespace NetRPA{
    public class TypeInfo
    {
        public Type type;
        public bool noninstance= false;
        public Dictionary<string, Func<object, object[], object>> methods;

        public object target;

        public TypeInfo Create(object target)
        {
            TypeInfo t = new TypeInfo();
            t.type = type;
            t.methods = methods;
            t.target = target;
            return t;
        }
    }
}