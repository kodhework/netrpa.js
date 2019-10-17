using System;
using System.Reflection;
using System.Linq.Expressions;
using System.Collections.Generic;
namespace NetRPA
{


    public class ClassWrapper
    {

        static Dictionary<string, TypeInfo> typeInfo = new Dictionary<string, TypeInfo>();
        static MethodInfo SelectBestOverloadMethod = null;
        static MethodInfo CastValueMethod = null;
        static MethodInfo InvokeMethodInfo = null;


        public static TypeInfo GetFromObject(object o)
        {
            TypeInfo tinfo = null;
            Type t = o.GetType();
            string typetext = t.GUID.ToString() + "$" + t.FullName;

            if (!typeInfo.TryGetValue(typetext, out tinfo))
            {


                MethodInfo[] methods = t.GetMethods(BindingFlags.Instance  | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.GetProperty);
                Dictionary<string, List<MethodInfo>> methodsByName = new Dictionary<string, List<MethodInfo>>();
                Dictionary<string, Func<object, object[], object>> funcs = new Dictionary<string, Func<object, object[], object>>();
                foreach (MethodInfo method in methods)
                {
                    string name = method.Name;
                    if (!methodsByName.ContainsKey(name))
                    {
                        methodsByName[name] = new List<MethodInfo>();
                    }
                    methodsByName[name].Add(method);
                }

                foreach (KeyValuePair<string, List<MethodInfo>> item in methodsByName)
                {
                    //Console.WriteLine(item.Key);
                    try{
                        funcs.Add(item.Key, ConvertMethodInfoOverloadsToDelegate(item.Value.ToArray()));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Failed getting method: " + item.Key + ": " + e.Message + ", " + e.StackTrace);
                    }
                }

                tinfo = new TypeInfo();
                tinfo.type = t;
                tinfo.methods = funcs;

                typeInfo.Add(typetext, tinfo);

            }
            return tinfo;
            // return tinfo.Create(o);
        }

        public static object CastValue(object o, Type t){
            if(o != null){
                if(o is  DynamicRemoteObject){
                    o = ((DynamicRemoteObject)o).ConvertTo(t);
                }       
            }
            return o;
        }


        public static TypeInfo GetFromType(Type t)
        {
            TypeInfo tinfo = null;
            string typetext = t.GUID.ToString() +  "$" + t.FullName + ">static";

            if (!typeInfo.TryGetValue(typetext, out tinfo))
            {


                MethodInfo[] methods = t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.GetProperty);
                ConstructorInfo[] constructors = t.GetConstructors();


                Dictionary<string, List<MethodBase>> methodsByName = new Dictionary<string, List<MethodBase>>();
                Dictionary<string, Func<object, object[], object>> funcs = new Dictionary<string, Func<object, object[], object>>();
                foreach (MethodInfo method in methods)
                {
                    string name = method.Name;
                    if (!methodsByName.ContainsKey(name))
                    {
                        methodsByName[name] = new List<MethodBase>();
                    }
                    methodsByName[name].Add(method);
                }

                foreach (ConstructorInfo method in constructors)
                {
                    string name = ".ctor";
                    if (!methodsByName.ContainsKey(name))
                    {
                        methodsByName[name] = new List<MethodBase>();
                    }
                    methodsByName[name].Add(method);
                }

                foreach (KeyValuePair<string, List<MethodBase>> item in methodsByName)
                {
                    
                    try{
                        funcs.Add(item.Key, ConvertMethodInfoOverloadsToDelegate(item.Value.ToArray()));
                    }catch(Exception e){
                        Console.WriteLine("Failed getting method: " + item.Key + ": " + e.Message + ", " + e.StackTrace);
                    }
                }
                
                tinfo = new TypeInfo();
                tinfo.type = t;
                tinfo.noninstance = true;
                tinfo.methods = funcs;

                typeInfo.Add(typetext, tinfo);

            }

            
            return tinfo;
            // return tinfo.Create(o);
        }

        public static int SelectBestOverload(Type[][] typeInfos, object[] args)
        {             
            if(typeInfos.Length == 1){
                return 0;
            }

            Type[] types = Type.GetTypeArray(args);
            for (int i = 0; i < typeInfos.Length; i++)
            {
                
                var pars = typeInfos[i];
                if (pars.Length != args.Length)
                {
                    continue;
                }


                bool good = true;
                for (int y = 0; y < types.Length; y++)
                {
                    if (!pars[y].IsAssignableFrom(types[y]))
                    {
                        if(types[y] == typeof(DynamicRemoteArrayObject) && pars[y].IsArray ){

                        }
                        else{
                            good = false;
                            break;
                        }
                    }                    
                }

                if (good) return i;
            }

            throw new System.Reflection.TargetException("Cannot find the best overload of methods for parameters");

        }


        public static Func<object, object[], object> ConvertMethodInfoOverloadsToDelegate(MethodBase[] methods){


            var typeInfo = new List<Type[]> ();
            for(int i=0;i<methods.Length;i++){
                var pars  = methods[i].GetParameters();
                Type[] typeinfo = new Type[pars.Length];
                for(int y=0;y<pars.Length;y++){
                    typeinfo[y] = pars[y].ParameterType;
                }
                typeInfo.Add(typeinfo);
            }

            var _methods = Expression.Constant(typeInfo.ToArray());
            var parameterObjectArray = Expression.Parameter(typeof(object[]));
            var target = Expression.Parameter(typeof(object), "target");

            if (SelectBestOverloadMethod == null)
            {
                SelectBestOverloadMethod = typeof(ClassWrapper).GetMethod("SelectBestOverload");
            }
            var methodIndex = Expression.Call(SelectBestOverloadMethod, _methods, parameterObjectArray);


            /* 
            var compiled = new List<Func<object, object[], object>>();
            for(int i=0;i<methods.Length;i++){
                compiled.Add(ConvertMethodInfoToDelegate(methods[i]));
            }
            var compiledMethods = Expression.Constant(compiled.ToArray());
            var func = Expression.ArrayAccess(compiledMethods, methodIndex);
            var invokeMethod = typeof(Func<object, object[], object>).GetMethod("Invoke");
            */

            var result = Expression.Variable(typeof(object), "result");
            var blocks = new List<BlockExpression>();
            for (int i = 0; i < methods.Length; i++)
            {
                blocks.Add(ConstructBlock(methods[i],target, parameterObjectArray, result));
            }


            
            
            
            var expressions = new List<Expression>();
            //expressions.Add(result);
            for (int i = 0; i < methods.Length; i++)
            {
                expressions.Add(Expression.IfThen(Expression.Equal(methodIndex, Expression.Constant(i)), blocks[i]));
            }
            expressions.Add(result);
            return Expression.Lambda<Func<object, object[], object>>(Expression.Block(new ParameterExpression[] { result }, expressions), target, parameterObjectArray).Compile();


            //var call = Expression.Call(func, invokeMethod, target, parameterObjectArray);
            //return Expression.Lambda<Func<object, object[], object>>(call, target, parameterObjectArray).Compile();
        }

        public static BlockExpression ConstructBlock(MethodBase method, Expression target, Expression parameterObjectArray, ParameterExpression result)
        {
            var block = new List<Expression>();
            var parameters = new List<Expression>();
            

            var pars = method.GetParameters();
            for (int i = 0; i < pars.Length; i++)
            {
                var par = pars[i];
                if(par.ParameterType.ContainsGenericParameters || par.ParameterType.IsByRef || par.ParameterType.IsPointer){

                    // por ahora no sé la manera
                    // de compilar eficientemente llamadas con parámetros genéricos

                    if(InvokeMethodInfo == null){
                        
                        InvokeMethodInfo = method.GetType().GetMethod("Invoke", new Type[]{typeof(object), typeof(object[])});
                    }
                    
                    if (result != null)
                    {
                        block.Add(Expression.Assign(result, Expression.Call(Expression.Constant(method), InvokeMethodInfo, target, parameterObjectArray)));
                    }
                    else
                    {
                        
                        block.Add(Expression.Call(Expression.Constant(method), InvokeMethodInfo, target, parameterObjectArray));
                    }
                    var Block1 = Expression.Block(block.ToArray());
                    return Block1;

                }

                
                if(CastValueMethod == null) CastValueMethod= typeof(ClassWrapper).GetMethod("CastValue");

                
                parameters.Add(Expression.Convert(Expression.Call(CastValueMethod, Expression.ArrayAccess(parameterObjectArray, Expression.Constant(i)), Expression.Constant(par.ParameterType)), par.ParameterType));
                
            }

            Expression call = null;
            if (method.IsStatic)
            {
                if(method is MethodInfo){
                    call = Expression.Call((MethodInfo)method, parameters.ToArray());
                }else if(method is ConstructorInfo){
                    call = Expression.New((ConstructorInfo)method, parameters.ToArray());
                }
                
            }
            else
            {
                if (method is MethodInfo)
                {
                    call = Expression.Call(Expression.Convert(target, method.DeclaringType), (MethodInfo)method, parameters.ToArray());
                }
                else if (method is ConstructorInfo)
                {
                    call = Expression.New((ConstructorInfo)method, parameters.ToArray());
                }
                
            }

            Type returnType = null;
            if(method is MethodInfo){
                returnType = ((MethodInfo)method).ReturnType;
            }else{
                returnType = method.DeclaringType;
            }
            if (returnType == typeof(void))
            {
                block.Add(call);
                if (result != null)
                {
                    block.Add(Expression.Assign(result, Expression.Constant(null)));
                }
                else{
                    block.Add(Expression.Constant(null));
                }
            }
            else
            {
                if (result != null)
                {
                    block.Add(Expression.Assign(result, Expression.Convert(call, typeof(object))));
                }
                else
                {
                    block.Add(Expression.Convert(call, typeof(object)));
                }
                
            }
            
            var Block = Expression.Block(block.ToArray());
            return Block;
        }

        public static Func<object, object[], object> ConvertMethodInfoToDelegate(MethodBase method){

            var parameterObjectArray = Expression.Parameter(typeof(object[]));
            var target = Expression.Parameter(typeof(object), "target");
            return Expression.Lambda<Func<object, object[],object>>(ConstructBlock(method, target, parameterObjectArray, null), target, parameterObjectArray).Compile();
                
        }
    }
}
