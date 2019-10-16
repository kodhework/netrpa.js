// you can compile the file in GNU/Linux
// source dll/csc
// csc -target:library -out:dll/104_add7.dll dll/104_add7.cs


// NOTE: Using precompiled dll is noticeable faster than compile at runtime

import { Channel as NetRPA } from '../Channel'

if (!process.env.NETRPA_RETURN_FUNC) {
    Invoke().then(function () { }).catch(function (er) {
        console.error("Error executing sample: ", er)
        process.exit()
    })
}

export async function Invoke(channel?) {
    if (!channel) channel = await NetRPA.create()
    
    await channel.client.LoadAssemblyFile(__dirname + "/dll/104_add7.dll")
    let Add7 = await channel.client.Construct('Test')

    console.info(await Add7.Invoke(12))
    console.info(await Add7.Invoke(20))

    if (!process.env.NETRPA_RETURN_FUNC) channel.close()
}