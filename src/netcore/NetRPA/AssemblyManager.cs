using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

namespace NetRPA{

    public class AssemblyInfo{
        internal Assembly assembly;
        internal byte[] rawData;

        public Assembly Assembly{
            get{
                return assembly;
            }
        }

        public byte[] RawData{
            get{
                return rawData;
            }
        }


    }

    public class AssemblyManager{

        Dictionary<string, Type> loadedTypes = new Dictionary<string, Type>();
        List<AssemblyInfo> loadedAssemblies = new List<AssemblyInfo>();
        List<string> Paths = new List<string>();
        List<Assembly> loadedAssemblies_0 = new List<Assembly>();

        public Test TestType(){
            return new Test();
        }

        public List<AssemblyInfo> LoadedAssemblies{
            get{
                return loadedAssemblies;
            }
        }


        public void _loadAssembly(Assembly running){
            var names = running.GetReferencedAssemblies();
            foreach (var name in names)
            {
                var a = Assembly.Load(name);
                _loadAssembly(a);
            }
            this.LoadAssembly(running);
        }
        
        public AssemblyManager(){


            //var running = typeof(AssemblyManager).Assembly;
            //_loadAssembly(running);           

            this.LoadAssembly(typeof(string).Assembly);
            this.LoadAssembly(typeof(System.Net.Sockets.Socket).Assembly);
            this.LoadAssembly(typeof(AssemblyManager).Assembly);


            AppDomain.CurrentDomain.AssemblyResolve +=
                new ResolveEventHandler(CurrentDomain_AssemblyResolve);

        }

        Assembly CurrentDomain_AssemblyResolve(object sender,
                                              ResolveEventArgs args)
        {

            var assemblyname = new AssemblyName(args.Name);

            /* 
            foreach(var path in Paths){
                var assemblyFileName = Path.Combine(path, assemblyname + ".dll");
                var fileInfo = new FileInfo(assemblyFileName);
                if(fileInfo.Exists){
                    var assembly = Assembly.LoadFrom(assemblyFileName);
                    return assembly;
                }
            }*/

            foreach(Assembly a in loadedAssemblies_0){
                if(a.GetName().FullName == assemblyname.FullName){
                    // Console.WriteLine("Loaded assembly: " + assemblyname.FullName);
                    return a;
                }
            }
            return null;
        }


        /* 
        public TypeInfo GetTypeDefinition(Type t)
        {
            return ClassWrapper.GetFromType(t);
        }*/

        public TypeInfo GetTypeDefinition(object def)
        {
            Type t = GetType(def);
            return ClassWrapper.GetFromType(t);
        }

        public Type GetType(object def)
        {
            if(def is string){
                Type type= null;
                if(loadedTypes.TryGetValue(def.ToString(), out type)){
                    return type; 
                }

            
                var ex = new RemoteException("Type " + def.ToString() + " was not found");
                ex.Code = "TYPE_NOT_FOUND";
                throw ex;
            }else{

                if(def is IDictionary<string, object>){
                    var dict = (IDictionary<string, object>)def;
                    var ts = dict["type"];
                    Type t = GetType(ts);
                    if(dict.ContainsKey("generic")){
                        System.Collections.IList generic = (System.Collections.IList)dict["generic"];
                        Type[] rgeneric = new Type[generic.Count];
                        for(int i=0;i<generic.Count;i++){
                            rgeneric[i] = GetType(generic[i]);
                        }
                        t = t.MakeGenericType(rgeneric);
                    }else{
                        if(t.GetGenericArguments().Length > 0){
                            var ex= new RemoteException("You need pass generic type arguments");
                            ex.Code = "INVALID_ARGUMENTS";
                            throw ex;
                        }
                    }
                    return t;
                }
                return null;
            }
        }


        public Type GetTypeOf(object obj)
        {
            if(obj == null){
                return typeof(object);
            }
            return obj.GetType();
        }

        public TypeInfo GetTypeDefinition(Type t, Type[] generic)
        {            
            
            return ClassWrapper.GetFromType(t.MakeGenericType(generic));
        }


        public void LoadAssemblyFile(string file){
            LoadAssembly(Assembly.LoadFile(file));
        }

        public void LoadAssemblyPartialName(string name){
            LoadAssembly(Assembly.LoadWithPartialName(name));
        }

        public void LoadAssembly(string name)
        {
            LoadAssembly(Assembly.Load(name));
        }

        public void LoadAssembly(Assembly a){
            AssemblyInfo n = new AssemblyInfo();
            n.assembly = a;
            LoadAssemblyInfo(n);
            
        }

        public void LoadAssemblyInfo(AssemblyInfo assemblyInfo){

            if(loadedAssemblies_0.IndexOf(assemblyInfo.assembly) >= 0) return;

            Assembly assem = assemblyInfo.assembly;
            Type[] types = assem.GetTypes();
            foreach(Type t in types){
                //Console.WriteLine(t.GUID.ToString());
                loadedTypes[t.FullName] = t;
                loadedTypes[assem.FullName + "@" + t.FullName] = t;
            }
            loadedAssemblies.Add(assemblyInfo);            
            loadedAssemblies_0.Add(assemblyInfo.assembly);
        }




        public AssemblyManager CreateNewScope(){
            return new AssemblyManager();
        }

        public Compiler.CSharp CSharpCompiler(){
            return new Compiler.CSharp(this);
        }

        public object Construct(object typedefinition)
        {
            return Construct(typedefinition, new object[]{});
        }

        public void Show(object[] args)
        {
            Console.WriteLine(args[0]);
        }

        public void Show1(object arg)
        {
            Console.WriteLine(arg);
        }

        public object Construct(object typedefinition, object[] args)
        {
            TypeInfo d = GetTypeDefinition(typedefinition);
            return d.methods[".ctor"].Invoke(null, args);
        }

    }

}