import {Channel as NetRPA} from '../Channel'

main()
async function main(){
    let channel = await NetRPA.create()
    let compiler = await channel.client.CSharpCompiler()
    let Hello = await compiler.CompileLambdaString(`
    async (input)=>{
        return ".NET Core Welcomes " + (string)input;
    }
    `)

    console.info(await Hello("javascript"))
    console.info(await Hello("node.js"))

    channel.close()
}