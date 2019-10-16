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
    let createCounter = await compiler.CompileLambdaString(`

    async (start) => 
    {
        var k = (int)start;
        return (System.Func<object,System.Threading.Tasks.Task<object>>)(
            async (i) => 
            { 
                return k++;
            }
        );
    }

    `)



    var nextNumber = await createCounter(12)
    console.log(await nextNumber(null))
    console.log(await nextNumber(null))
    console.log(await nextNumber(null))
    console.log(await nextNumber(null))

    if (!process.env.NETRPA_RETURN_FUNC) channel.close()
}