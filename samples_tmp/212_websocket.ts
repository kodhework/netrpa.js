import { Channel as NetRPA } from '../Channel'


import http from 'http'
import 'npm://ws@7.1.2'
import { Server as WebSocketServer} from 'ws'

//var WebSocketServer = ws.WebSocketServer

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
    // The NetWebSocket class is an adapter between the prescriptive interop model
    
    public abstract class NetWebSocket
    {


        Func<object, Task<object>> SendImpl { get; set; }
        public NetWebSocket(Func<object, Task<object>> sendImpl) {
            this.SendImpl = sendImpl;
        }

        public abstract Task ReceiveAsync(string message);
        public async Task SendAsync(string message)
        {
            await this.SendImpl(message);
            
        }
    }

    // The MyNetWebSocket embeds application specific websocket logic:
    // it sends a message back to the client for every message it receives
    // from the client.

    public class MyNetWebSocket : NetWebSocket
    {
        public MyNetWebSocket(Func<object, Task<object>> sendImpl)
            : base(sendImpl) {}

        public override async Task ReceiveAsync(string message) 
        {
            Console.WriteLine(message);
            await this.SendAsync("Hello from .NET server on " + Environment.OSVersion.ToString()  + 
                " at " + DateTime.Now.ToString());
            
        }
    }

    // The Startup class with Invoke method provide the implementation of the
    // createMyNetWebSocket function. 
    public class Test
    {

        
        public MyNetWebSocket Invoke(dynamic sendImpl)
        {
            // In NetRPA functions are automatically unref
            // after method execution, so executing preserve
            // will be allow to use after method exits 
            sendImpl.Preserve();

            // In NetRPA functions are passed as DynamicRemoteObject 
            // and can be converted to Func<object, Task<object>>
            // or Func<object[], Task<object>>. See the following line            
            var ws = new MyNetWebSocket(sendImpl.ConvertTo(typeof(Func<object, Task<object>>)));
            return ws;
        }
    }
    
    `, 'Test')



    // Create an HTTP server that returns an HTML page to the browser client.
    // The JavaScript on the HTML page establishes a WebSocket connection
    // back to the server it came from, and sends a message to the server
    // every second. Messages received from the server are displayed in the
    // browser.

    var server = http.createServer(function (req, res) {
        res.writeHead(200, { 'Content-Type': 'text/html' });
        res.end(`
        <!DOCTYPE html>
        <html>
          <body>
            <script>
                var ws = new WebSocket('ws://' + window.document.location.host);
                ws.onmessage = function (event) {
                    var div = document.createElement('div');
                    div.appendChild(document.createTextNode(event.data));
                    document.getElementsByTagName('body')[0].appendChild(div);   
                };
                ws.onopen = function () {
                  setInterval(function () {
                    ws.send('Hello from client at ' + new Date().toString());
                  }, 1000);
                };
            </script>
          </body>
        </html> `)
    });

    // Create a WebSocket server associated with the HTTP server.

    var wss = new WebSocketServer({ server: server });

    // For every new WebSocket connection, create an instance of the
    // WebSocket handler in .NET using the createMyNetWebSocket function.
    // Pass all incoming WebSocket messages to .NET, and allow .NET
    // to send messages back to the client over the connection.
    


    wss.on('connection', async function (ws) {

        var sendImpl = function (message) {
            ws.send(message)
        }
        var MyNetWebSocket = await Test.Invoke(sendImpl)
        
        ws.on('message', async function (message) {
            await MyNetWebSocket.ReceiveAsync(message)
        })
    })

    let port = process.env.PORT || 8080
    server.listen(port)
    console.log(`Please open 127.0.0.1:${port} on your browser`)
}