import { Channel as NetRPA } from '../Channel'
import Os from 'os'

main()
async function main() {
    let channel = await NetRPA.create()
    let compiler = await channel.client.CSharpCompiler()
    let Test = await compiler.CompileString(`

    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Collections.Generic;

    public class Test
    {
        public NetRPA.Value<List<string>> Invoke(dynamic data)
        {
            
            X509Store store = new X509Store(
                (string)data.storeName, 
                (StoreLocation)Enum.Parse(typeof(StoreLocation), (string)data.storeLocation));
            store.Open(OpenFlags.ReadOnly);
            try
            {
                List<string> result = new List<string>();
                foreach (X509Certificate2 certificate in store.Certificates)
                {
                    result.Add(certificate.Subject);
                }

                // Remember, NetRPA.Value for serialize the object instead of proxying
                return new NetRPA.Value<List<string>>(result);
            }
            finally
            {
                store.Close();
            }

        }
    }

    `, 'Test')


    if(Os.platform() == "win32"){
        console.log(await Test.Invoke({
            storeName: 'My',
            storeLocation: 'LocalMachine'
        }))
    }
    else{
        console.log(await Test.Invoke({
            storeName: 'Root',
            storeLocation: 'LocalMachine'
        }))
    }

    channel.close()
}