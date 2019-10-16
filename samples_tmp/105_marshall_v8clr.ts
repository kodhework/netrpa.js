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
    using System.Collections.Generic;
    class Test{
        public void Invoke(object input){
            Console.WriteLine("-----> In .NET:");
            foreach (var kv in (IDictionary<string,object>)input)
            {
                Console.WriteLine(kv.Key + ": " + kv.Value.GetType());
            }
        }
    }
    `, 'Test')

    var payload = {
        anInteger: 1,
        aNumber: 3.1415,
        aString: 'foobar',
        aBool: true,
        anObject: {},
        anArray: ['a', 1, true]
    }
    
    console.log('-----> In node.js:')
    console.log(payload)


    // using channel.plain will serialize the value 
    await Test.Invoke(channel.plain(payload))

    if (!process.env.NETRPA_RETURN_FUNC) channel.close()
}