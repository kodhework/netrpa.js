import { Channel as NetRPA } from '../Channel'
import Path from 'path'



main()
async function main() {
    let channel = await NetRPA.create()

    // Load Assemblies 
    await channel.client.LoadAssemblyPartialName("System.IO.Compression.FileSystem")
    let compiler = await channel.client.CSharpCompiler()
    let Test = await compiler.CompileString(`
    
    using System;
    using System.Threading.Tasks;
    using System.IO.Compression;

    public class Test
    {
        public async Task Invoke(dynamic input)
        {

            await Task.Run(async () => {
                ZipFile.CreateFromDirectory((string)input.source, (string)input.destination);
            });          

        }
    }
    
    `, 'Test')

    await Test.Invoke({
        source: __dirname,
        destination: Path.join(__dirname,"..", "samples.zip")
    })
    console.log("The samples directory has been compressed to ../samples.zip file.")

    channel.close()
}