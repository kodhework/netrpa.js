import { Channel as NetRPA } from '../Channel'
import Os from 'os'
import fs from 'fs'

// JUST NOW THIS EXAMPLE DOESN'T WORK, BECAUSE WAS FOR NET FRAMEWORK
// NEED WORK FOR CONVERT THE CS FILE 205_soap.cs TO NET CORE


main()
async function main() {
    let channel = await NetRPA.create()

    // Load Assemblies 
    await channel.client.LoadAssemblyPartialName("System.Xml")
    await channel.client.LoadAssemblyPartialName("System.Data")
    await channel.client.LoadAssemblyPartialName("System.Data.DataSetExtensions")
    await channel.client.LoadAssemblyPartialName("System.Runtime.Serialization")
    await channel.client.LoadAssemblyPartialName("System.ServiceModel")
    await channel.client.LoadAssemblyPartialName("System.Xml.Linq")


    let compiler = await channel.client.CSharpCompiler()

    let convertKilograms = await compiler.CompileString(fs.readFileSync(__dirname + "/dll/205_soap.cs",'utf8'), 'Startup')

    console.log(await convertKilograms.Invoke(123.0))
    channel.close()
}