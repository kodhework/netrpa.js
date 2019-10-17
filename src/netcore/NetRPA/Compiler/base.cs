using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using NetRPA;
using System.Collections.Generic;

using System.Text;

namespace DynamicRun.Builder
{
    internal class Compiler
    {
        string name;
        AssemblyInfo[] references;
        public Compiler(string name, AssemblyInfo[] references){
            this.name = name;
            this.references = references;
        }
        public byte[] Compile(string sourceCode)
        {
            
            using (var peStream = new MemoryStream())
            {
                var result = GenerateCode(sourceCode, this.name, this.references).Emit(peStream);

                if (!result.Success)
                {
                    StringBuilder stringBuilder= new StringBuilder("Compilation errors: ");
                    var failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);
                    foreach (var diagnostic in failures)
                    {
                        //Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                        stringBuilder.AppendLine(diagnostic.Id.ToString() + ". " + diagnostic.GetMessage());
                    }

                    var ex = new RemoteException(stringBuilder.ToString());
                    ex.Code = "COMPILATION_ERROR";
                    throw ex;
                }

                //Console.WriteConsole.WriteLine("Compilation done without any error.");
                peStream.Seek(0, SeekOrigin.Begin);
                return peStream.ToArray();
            }
        }

        public static void _loadAssembly(List<MetadataReference> ref1, List<System.Reflection.Assembly> list, System.Reflection.Assembly assem)
        {
            if(list.IndexOf(assem)>=0)
                return; 

            list.Add(assem);
            var names = assem.GetReferencedAssemblies();
            foreach (var name in names)
            {
                try{
                    System.Reflection.Assembly a = System.Reflection.Assembly.Load(name);
                    _loadAssembly(ref1, list, a);
                }catch(Exception e){
                    Console.WriteLine("Possible error loading assembly: " + e.Message);
                }
            }

            //Console.WriteLine(assem.Location);
            ref1.Add(MetadataReference.CreateFromFile(assem.Location));

            
        }


        private static CSharpCompilation GenerateCode(string sourceCode, string name, AssemblyInfo[] areferences )
        {
            var codeString = SourceText.From(sourceCode);
            var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7);

            var parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(codeString, options);
            /* 
            var references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo).Assembly.Location),
            };*/

            var references = new List<MetadataReference>();
            var assems = new List<System.Reflection.Assembly>();

            for (int i = 0; i < areferences.Length; i++)
            {
                if (areferences[i].rawData != null)
                {
                    references.Add(MetadataReference.CreateFromImage(areferences[i].rawData));
                }
            }

            for (int i = 0; i < areferences.Length; i++)
            {
                if (areferences[i].rawData == null)
                {
                    _loadAssembly(references, assems, areferences[i].assembly);
                }
            }


           
            

            return CSharpCompilation.Create(name,
                new[] { parsedSyntaxTree }, 
                references: references, 
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, 
                    optimizationLevel: OptimizationLevel.Release,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default));
        }
    }
}