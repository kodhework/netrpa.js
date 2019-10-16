import { Channel as NetRPA } from '../Channel'

main()
async function main() {
    let channel = await NetRPA.create()
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

    channel.close()
}