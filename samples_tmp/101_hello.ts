import {Channel as NetRPA} from '../Channel'

if(!process.env.NETRPA_RETURN_FUNC){
    Invoke().then(function(){}).catch(function(er){
        console.error("Error executing sample: ", er)
        process.exit()
    })
}
    

export async function Invoke(channel?){
    if(!channel) channel = await NetRPA.create()


    let compiler = await channel.client.CSharpCompiler()
    let Hello = await compiler.CompileString(`
    
    class Test{
        public string Invoke(string input){
            return ".NET Core Welcomes " + input;
        }
    }

    `, 'Test')

    console.info(await Hello.Invoke("javascript"))
    console.info(await Hello.Invoke("node.js"))

    if (!process.env.NETRPA_RETURN_FUNC) channel.close()
}