import {Channel as NetRPA} from '../Channel'

if (!process.env.NETRPA_RETURN_FUNC) {
    Invoke().then(function () { }).catch(function (er) {
        console.error("Error executing sample: ", er)
        process.exit()
    })
}

export async function Invoke(channel?){

    if (!channel) channel = await NetRPA.create()
    let compiler = await channel.client.CSharpCompiler()
    let Hello = await compiler.CompileLambdaString(`
    async (input)=>{
        return ".NET Core Welcomes " + (string)input;
    }
    `)

    console.info(await Hello("javascript"))
    console.info(await Hello("node.js"))

    if (!process.env.NETRPA_RETURN_FUNC) channel.close()
}