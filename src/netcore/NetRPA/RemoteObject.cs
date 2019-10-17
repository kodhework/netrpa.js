using System;
using System.Threading.Tasks;
using System.Dynamic;
using System.Collections.Generic;

namespace NetRPA
{

    public class RemoteSender{

        public static Task<object> Invoke(Server server, CrossSocket socket, string rpa_id, object[] args)
        {

            args = server.ConvertArguments(args, socket);
            Request req = new Request();
            req.method = "rpa_run";
            req.target = rpa_id;
            req.arguments = args;
            return server.Send(socket, req);

        }

        public static Task<object> Invoke(Server server, CrossSocket socket, string rpa_id )
        {
            return Invoke(server, socket, rpa_id, new object[] { });
        }

        public static Task<object> InvokeMethod(Server server, CrossSocket socket, string rpa_id, string method)
        {
            return InvokeMethod(server, socket, rpa_id, method, new object[] { });
        }

        public static Task<object> InvokeMethod(Server server, CrossSocket socket, string rpa_id, string method, object[] args)
        {

            args = server.ConvertArguments(args, socket);
            Request req = new Request();
            req.method = method;
            req.target = rpa_id;
            req.arguments = args;
            return server.Send(socket, req);
        }

    }



    class RemoteObject
    {

        public string rpa_id;
        public bool rpa_from;

        public int preserved = 0;

        public bool rpa_function;

        CrossSocket socket;
        Server server; 

        public RemoteObject(){}
        
        public RemoteObject(Server server, CrossSocket client){
            socket = client;
            this.server = server;
        }

        /*     
        public DynamicRemoteObject ConvertToDynamic(){
            return new DynamicRemoteObject(this);
        }
        */


        public Task<object> Invoke(object[] args){

            args = server.ConvertArguments(args, socket);
            Request req = new Request();
            req.method = "rpa_run";
            req.target = rpa_id;
            req.arguments = args;
            return server.Send(socket, req);

        }

        public Task<object> Invoke()
        {
            return Invoke(new object[] { });
        }

        public Task<object> InvokeMethod(string method)
        {
            return InvokeMethod(method, new object[] { });
        }

        public Task<object> InvokeMethod(string method, object[] args){

            args = server.ConvertArguments(args, socket);
            Request req = new Request();
            req.method = method;
            req.target = rpa_id;
            req.arguments = args;
            return server.Send(socket, req);


        }

        public void Preserve(){
            this.preserved = 1;
        }

        public async Task UnRef(){
            Request req = new Request();
            req.method = "UnRef";
            req.target = "R>y";
            req.arguments = new object[]{ rpa_id, RemoteSocketObject.instance};
            await server.Send(socket, req, true);
            return;
        }

        

    }
}
