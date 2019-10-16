import { Channel as NetRPA } from '../Channel'

main()
async function main() {
    let channel = await NetRPA.create()
    let compiler = await channel.client.CSharpCompiler()
    let Test = await compiler.CompileString(`

    using System;
    using System.Threading.Tasks;
    public class Test
    {
        public async Task<string> Invoke(string input)
        {
            return await Task.Run<string>(async () => {

                // we are on CLR thread pool thread here
                // simulate long running operation
                await Task.Delay(5000); 
                return ".NET welcomes " + input;
            });

        }
    }

    `, 'Test')



    let timer = setInterval(function () {
        console.log('Node.js event loop is alive');
    }, 1000)

    console.log(await Test.Invoke("node.js"))
    clearInterval(timer)

    channel.close()
}