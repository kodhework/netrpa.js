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

    class Test{
        public async Task<int> Invoke(dynamic data){
            int sum = (int)data.a + (int)data.b;
            
            return await data.multiplyBy2(sum);
        }
    }
    `, 'Test')

    

    var payload = {
        a: 2,
        b: 3,
        multiplyBy2: function (input) {
            return input * 2
        }
    }

    // By default rpa pass parameters in mixed mode (primitive values are serialized
    // but also object is proxied for make  functions from CLR)
    console.log(await Test.Invoke(payload))

    if (!process.env.NETRPA_RETURN_FUNC) channel.close()
}