import { Channel as NetRPA } from '../Channel'
import Path from 'path'
import fs from 'fs'



if (!process.env.NETRPA_RETURN_FUNC) {
    Invoke().then(function () { }).catch(function (er) {
        console.error("Error executing sample: ", er)
        process.exit()
    })
}

export async function Invoke(channel?) {
    if (!channel) channel = await NetRPA.create()

    // Load Assemblies 
    await channel.client.LoadAssemblyPartialName("System.IO.Compression.FileSystem")
    let compiler = await channel.client.CSharpCompiler()
    let Test = await compiler.CompileString(`
    
    using System;
    using System.Threading.Tasks;
    using System.IO;
    using System.IO.Compression;

    public class Test
    {
        public async Task Invoke(dynamic input)
        {
            DirectoryInfo d = new DirectoryInfo(input.destination);
            if(d.Exists){
                d.Delete(true);
            }

            await Task.Run(async () => {
                ZipFile.ExtractToDirectory((string)input.source, (string)input.destination);
            });          

        }
    }
    
    `, 'Test')

    await Test.Invoke({
        source: Path.join(__dirname,"..", "samples.zip"),
        destination: Path.join(__dirname, "..", "samples_tmp")
    })
    console.log("The ../samples.zip file has been decompressed to ../samples_tmp directory")

    if (!process.env.NETRPA_RETURN_FUNC) channel.close()
}