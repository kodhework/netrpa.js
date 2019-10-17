using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;


namespace NetRPA.Compiler{



    public class CSharp{
        static int count=0;
        static Dictionary<string, AssemblyInfo> AssemblyCache= new Dictionary<string, AssemblyInfo>();

        AssemblyManager manager;

        public CSharp(AssemblyManager manager){
            this.manager = manager;
        }


        public string GetSha1Id(string source)
        {
            SHA1 sha1 = SHA1CryptoServiceProvider.Create();
            Byte[] textOriginal = Encoding.UTF8.GetBytes(source);
            Byte[] hash = sha1.ComputeHash(textOriginal);
            StringBuilder cadena = new StringBuilder();
            foreach (byte i in hash)
            {
                cadena.AppendFormat("{0:x2}", i);
            }
            return cadena.ToString();
        }


        public Delegate CompileLambdaString(string source){

            source = @"
            class Remote{
                public static object GetLambda(){
                    return new System.Func<object, System.Threading.Tasks.Task<object>>("+source+@");
                }
            }
            ";

            /* 
            string sha1 = GetSha1Id(source);
            if (AssemblyCache.ContainsKey(sha1))
            {
                manager.LoadAssemblyInfo(AssemblyCache[sha1]);
                return null;
            }*/

            var compiler = new DynamicRun.Builder.Compiler("compiled.dll", manager.LoadedAssemblies.ToArray());
            byte[] data = compiler.Compile(source);
            Assembly a = Assembly.Load(data);
            Type t = a.GetType("Remote");
            var Func = (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), t.GetMethod("GetLambda"));
            return (Delegate)Func();
        }

        public object CompileString(string source, string type)
        {

            string sha1 = GetSha1Id(source);
            Assembly assembly = CompileString(source);
            return manager.Construct(assembly.FullName + "@" + type);

        }
        public Assembly CompileString(string source)
        {

            string sha1 = GetSha1Id(source);
            if (AssemblyCache.ContainsKey(sha1))
            {
                manager.LoadAssemblyInfo(AssemblyCache[sha1]);
                return AssemblyCache[sha1].assembly;
            }

            var compiler = new DynamicRun.Builder.Compiler("compiled" + (count++).ToString()  + ".dll", manager.LoadedAssemblies.ToArray());
            byte[] data = compiler.Compile(source);
            AssemblyInfo ax = new AssemblyInfo();
            ax.assembly = Assembly.Load(data);
            ax.rawData = data;
            manager.LoadAssemblyInfo(ax);
            AssemblyCache[sha1] = ax;
            return ax.assembly;
        }

        


    }

}