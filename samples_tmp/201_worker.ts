import { Channel as NetRPA } from '../Channel'

if (!process.env.NETRPA_RETURN_FUNC) {
    Invoke().then(function () { }).catch(function (er) {
        console.error("Error executing sample: ", er)
        process.exit()
    })
}

export async function Invoke(channel?) {
    if (!channel) channel = await NetRPA.create()

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

    if (!process.env.NETRPA_RETURN_FUNC) channel.close()
}