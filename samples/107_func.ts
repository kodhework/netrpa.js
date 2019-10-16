import { Channel as NetRPA } from '../Channel'

main()
async function main() {
    let channel = await NetRPA.create()
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

    channel.close()
}