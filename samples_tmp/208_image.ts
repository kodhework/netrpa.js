import { Channel as NetRPA } from '../Channel'
import Path from 'path'
import Os from 'os'
import fs from 'fs'



// For run this example on GNU/Linux, you need install libgdiplus
// sudo apt-get install libgdiplus


if (!process.env.NETRPA_RETURN_FUNC) {
    Invoke().then(function () { }).catch(function (er) {
        console.error("Error executing sample: ", er)
        process.exit()
    })
}

export async function Invoke(channel?) {
    if (!channel) channel = await NetRPA.create()
    
    if(Os.platform() == "win32")
        await channel.client.LoadAssemblyFile(Path.join(__dirname, "dll", "Drawing/runtimes/win/lib/netcoreapp3.0/", "System.Drawing.Common.dll"))
    else 
        await channel.client.LoadAssemblyFile(Path.join(__dirname, "dll", "Drawing/runtimes/unix/lib/netcoreapp3.0/", "System.Drawing.Common.dll"))
     
    

    let compiler = await channel.client.CSharpCompiler()
    let Test = await compiler.CompileString(`
    
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;

    public class Test
    {
        static IDictionary<string, ImageFormat> formats = new Dictionary<string, ImageFormat> 
        {
            { "jpg", ImageFormat.Jpeg },
            { "bmp", ImageFormat.Bmp },
            { "gif", ImageFormat.Gif },
            { "tiff", ImageFormat.Tiff },
            { "png", ImageFormat.Png }
        };

        public async Task Invoke(dynamic input)
        {

            await Task.Run(async () => {
                using (Image image = Image.FromFile((string)input.source))
                {
                    image.Save((string)input.destination, formats[(string)input.toType]);
                }
            });


        }
    }
    
    `, 'Test')

    var params = {
        source: Path.join(__dirname, 'netrpa.png'),
        destination: Path.join(__dirname, 'netrpa.jpg'),
        toType: 'jpg'
    }
    if (fs.existsSync(params.destination)) fs.unlinkSync(params.destination)

    await Test.Invoke(params)
    console.log('The netrpa.png has been asynchronously converted to netrpa.jpg.')

    if (!process.env.NETRPA_RETURN_FUNC) channel.close()
}