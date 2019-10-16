import { Channel as NetRPA } from '../Channel'
import Path from 'path'
import Os from 'os'


// For run this example on GNU/Linux, you need install libgdiplus
// sudo apt-get install libgdiplus


main()
async function main() {
    let channel = await NetRPA.create()

    

    
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


    await Test.Invoke(params)
    console.log('The netrpa.png has been asynchronously converted to netrpa.jpg.')

    channel.close()
}