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

    class Test{
        public NetRPA.Value<object> Invoke(){
            var result = new {
                anInteger = 1,
                aNumber = 3.1415,
                aString = "foobar",
                aBool = true,
                anObject = new { a = "b", c = 12 },
                anArray = new object[] { "a", 1, true },
                // aBuffer = new byte[1024]
            };

            // Using NetRPA.Value will be serialized to Node.js instead of proxying
            return new NetRPA.Value<object>(result);
        }
    }
    `, 'Test')

    
    console.log('-----> In node.js:')
    console.log(await Test.Invoke())

    if (!process.env.NETRPA_RETURN_FUNC) channel.close()
}