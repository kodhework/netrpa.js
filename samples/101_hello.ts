import {Channel as NetRPA} from '../Channel'

main()
async function main(){
    let channel = await NetRPA.create()
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

    channel.close()
}