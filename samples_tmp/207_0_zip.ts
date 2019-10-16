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

    let params = {
        source: __dirname,
        destination: Path.join(__dirname, "..", "samples.zip")
    }
    
    if(fs.existsSync(params.destination)) fs.unlinkSync(params.destination)

    
    await Test.Invoke(params)
    console.log("The samples directory has been compressed to ../samples.zip file.")

    if (!process.env.NETRPA_RETURN_FUNC) channel.close()
}