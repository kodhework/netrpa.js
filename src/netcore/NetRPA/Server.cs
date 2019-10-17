using System;

//using System.Net.Sockets;
//using System.Net;

using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

//using System.IO.Pipes;
//using System.IO;
//using System.Runtime.InteropServices;

namespace NetRPA
{

    public class Server
    {   

        string id; 
        object service; 

        Dictionary<string, Reference> references = new Dictionary<string, Reference>();
        Hashtable hash = new Hashtable();
        Hashtable socketStore = new Hashtable();

        int count = 0;
        int ccount=0;
        int taskid=0; 
        bool Autounref = true;

        public Server(string id, object service){

            this.id =  id; 
            this.service = service;
            AddRef(service);
            AddRef(service, "R>y");
            
            
        }


        public SocketStore GetStoreForSocket(CrossSocket client, bool create){
            var value = socketStore[client];
            SocketStore store = null;
            if(value != null){
                store = (SocketStore)value;
            }else if(create){
                store = new SocketStore();
                socketStore[client] = store;
            }
            return store;
        }

        /* Compatibility with JS */
        public Task unRef(object target)
        {
            return UnRef(target, null);
        }
        public Task unRef(object target, CrossSocket client){
            return UnRef(target, client);
        }


        public async Task UnRef(object target, CrossSocket client){
            
            if(target is DynamicRemoteObject){
                var remote = (DynamicRemoteObject)target;
                if(remote.dictionary.ContainsKey("rpa_from") && (bool)remote.dictionary["rpa_from"]){
                    await remote.UnRef();
                    return; 
                }
            }

            string id = "";
            if(target is string){
                id = target.ToString();
            }else{
                object search = hash[target];
                if(search != null){
                    id = search.ToString();
                }
            }

            if(id != ""){
                if (references.ContainsKey(id))
                {
                    var refered = references[id];
                    refered.references--;
                    if (refered.references <= 0)
                    {
                        this.hash.Remove(refered.target);
                        references.Remove(id);
                    }


                }
                if (client != null)
                {
                    var store = GetStoreForSocket(client, false);
                    if (store != null)
                    {
                        if (store.refs.ContainsKey(id))
                        {
                            store.refs[id]--;
                            if(store.refs[id] == 0) store.refs.Remove(id);
                        }
                    }
                }

            }

            

        }

        public string AddRef(object target)
        {
            return AddRef(target, "");
        }

        public string AddRef(object target, string id)
        {
            return AddRef(target, id, null);
        }

        public string AddRef(object target, string id, CrossSocket client){
            if(id == ""){
                object nid = hash[target];
                if(nid != null){
                    id = (string)nid;
                }
                else{
                    id = "R>" + (count++).ToString();
                    hash[target] = id;
                }
            }



            Reference r =null;
            if(!references.ContainsKey(id)){
                r = new Reference();
                references.Add(id, r);
                r.id = id; 
                r.target = target;
            }else{
                r = references[id];
            }
            r.references++;

            if(client != null){
                var store = GetStoreForSocket(client, true);
                if(!store.refs.ContainsKey(id)){
                    store.refs[id] = 1;
                }else{
                    store.refs[id]++;
                }
            }

            return id;
        }


        public object GetTarget(string id){
            if(this.references.ContainsKey(id)){
                Reference r = this.references[id];
                return r.target;
            }
            throw new Exception("The remote object doesn't exists or was disconnected");
        }


        public string GetSha1Id(){
            SHA1 sha1 = SHA1CryptoServiceProvider.Create();
            Byte[] textOriginal = Encoding.UTF8.GetBytes(this.id);
            Byte[] hash = sha1.ComputeHash(textOriginal);
            StringBuilder cadena = new StringBuilder();
            foreach (byte i in hash)
            {
                cadena.AppendFormat("{0:x2}", i);
            }
            return cadena.ToString();
        }


        public async void Create()
        {
            SocketWrapper socw = new SocketWrapper(this.id);
            CrossSocket socket= await socw.Create();

            while(true){
                var client = await socket.AcceptAsync();
                this.Connection(client);
                this.AttachDisconnect(client);
            }           

        }

        public async Task AttachDisconnect(CrossSocket client){
            await client.WaitDisconnect();
            var store = GetStoreForSocket(client, false);
            if(store !=null){
                var tasks = store.tasks;
                if(tasks != null){
                    var ex = new RemoteException("RPA connection was destroyed");
                    ex.Code = "RPA_DESTROYED";
                    foreach(var Item in tasks){                        
                        Item.Value.SetException(ex);
                    }
                    store.tasks = null; 
                }

                if(store.refs != null){
                    foreach(var Item in store.refs){
                        int count = Item.Value;
                        for(int i=0;i < count;i++){
                            this.UnRef(Item.Key, client);
                        }
                    }
                    store.refs = null;
                }
                socketStore.Remove(client);
            }
        }


        public object GetArgument(CrossSocket client, object value1){
            if(value1 is JValue){
                var value =(JValue)value1;
                if (value.Type == JTokenType.Integer)
                {
                    long val = value.ToObject<long>();
                    if (val <= Int32.MaxValue || val >= Int32.MinValue)
                    {
                        return (int)val;
                    }
                    else
                    {
                        return val;
                    }
                }
                else if (value.Type == JTokenType.Float)
                {
                    double val = value.ToObject<double>();
                    if (val <= Single.MaxValue || val >= Single.MinValue)
                    {
                        return (float)val;
                    }
                    else
                    {
                        return val;
                    }
                }
                else if (value.Type == JTokenType.String)
                {
                    return value.ToObject<string>();
                }
                else if (value.Type == JTokenType.Null || value.Type == JTokenType.Undefined)
                {
                    return null;
                }
                
                else if (value.Type == JTokenType.Boolean)
                {
                    return value.ToObject<bool>();
                }
            }
            else if(value1 is JArray){
                return GetArguments(client, (JArray)value1);
            }

            else if (value1 is JObject)
            {
                var obj= (JObject)value1;
                IDictionary<string, object> data=  GetFromJObject(client, obj);
                if(data.ContainsKey("rpa_socket")){
                    return client;
                }
                else if(data.ContainsKey("rpa_id")){
                    if(data.ContainsKey("rpa_from") && (bool)data["rpa_from"]){
                        DynamicRemoteObject val = null;
                        if(data.ContainsKey("rpa_array") && (bool)data["rpa_array"]){
                            val = new DynamicRemoteArrayObject(this, client);
                            //val["Length"] = data["length"];
                        }else{
                            val = new DynamicRemoteObject(this, client);
                        }
                        foreach(var Item in data){
                            val.dictionary.Add(Item.Key, Item.Value);
                        }

                        return val;

                        /* 
                        var vall = new RemoteObject(this, client);
                        vall.rpa_id = (string)data["rpa_id"];
                        vall.rpa_from = true; 
                        if(data.ContainsKey("rpa_function"))
                            vall.rpa_function = (bool)data["rpa_function"];
                        return vall.ConvertToDynamic();
                        */

                    }else{
                        string id = data["rpa_id"].ToString();
                        return GetTarget(id);
                    }

                }
                else if(data.ContainsKey("rpa_array") && (bool)data["rpa_array"]){

                    var val = new DynamicRemoteArrayObject(this, client);
                    val.dictionary = ((DynamicRemoteObject)data).dictionary;
                    //val["Length"] = data["length"];
                    return val;


                }
                else{
                    return data;
                }

            }

            return null;
        }

        public IDictionary<string, object> GetFromJObject(CrossSocket client,JObject value){
            DynamicRemoteObject d = new DynamicRemoteObject();
            IDictionary<string,object> data = (IDictionary<string, object>)d;
            foreach(JProperty property in value.Properties()){
                var val = property.Value;
                data[property.Name] = GetArgument(client, val);
            }
            return data;
        }


        public object[] ConvertArguments(object[] args, CrossSocket client)
        {

            for (int i=0;i<args.Length;i++)
            {
                args[i] = ConvertArgument(args[i], client);
            }
            return args;
        }

        public object ConvertArgument(object arg, CrossSocket client){
            
            if(arg == null) return arg; 
            if(arg is string || arg.GetType().IsPrimitive) return arg; 


            if(arg is IDictionary<string, object>){
                var dict = (IDictionary<string, object>)arg;
                if(dict.ContainsKey("rpa_id")){
                    var newarg1 = new RemoteObject();
                    newarg1.rpa_id =  (string)dict["rpa_id"];
                    return newarg1;
                }                
            }

            if(arg is Value){
                arg = ((Value)arg).GetOriginalValue();
                return arg;
            }

            string id = this.AddRef(arg, "", client);
            var newarg = new RemoteObject();
            newarg.rpa_id = id;
            newarg.rpa_from = true; 
            if(typeof(MulticastDelegate).IsAssignableFrom(arg.GetType())){
                newarg.rpa_function = true;
            }            
            return newarg;


        }

        public object[] GetArguments(CrossSocket client, JArray args1){

            object[] args = new object[args1.Count];
            int i = 0;
            foreach (object value in args1)
            {
                args[i]= GetArgument(client, value);                
                i++;
            }
            return args;
        }


        public Task<object> Send(CrossSocket client, Request request)
        {
            return Send(client, request, false);
        }

        public async Task<object> Send(CrossSocket client, Request request, bool nowait)
        {
            int taskid = this.taskid++;
            if(nowait){
                request.taskid=-1;
                this.taskid--;
            }else{
                request.taskid = taskid;
            }

            string content = JsonConvert.SerializeObject(request);
            byte[] data = Encoding.UTF8.GetBytes(content + "\n");
            await client.SendAsync(data);

            if(!nowait){
                SocketStore store = GetStoreForSocket(client, true);
                store.tasks[taskid] = new TaskCompletionSource<object>();
                return await store.tasks[taskid].Task;
            }
            return null;
        }


        public async void SendAnswer(CrossSocket client, JObject command, object result)
        {
            Answer answer = new Answer();
            answer.taskid = command.Value<int>("taskid");
            answer.result.data = ConvertArgument(result, client);
            string content = JsonConvert.SerializeObject(answer);
            //Console.WriteLine("sending: " + content);
            byte[] data = Encoding.UTF8.GetBytes(content + "\n");
            await client.SendAsync(data);
        }

        public async void SendAnswerError(CrossSocket client, JObject command, Exception error)
        {

            //Console.WriteLine(error.Message);
            Answer answer = new Answer();
            answer.taskid = command.Value<int>("taskid");
            answer.result.error = new Error();
            answer.result.error.message= error.Message;
            answer.result.error.stack = error.Message + "\n" + error.StackTrace;
            answer.result.error.code = error.GetType().FullName;

            string content = JsonConvert.SerializeObject(answer);
            byte[] data = Encoding.UTF8.GetBytes(content + "\n");
            await client.SendAsync(data);
        }

        public async void CommandReceived(CrossSocket client, string text){
            //Console.WriteLine(text);
            Newtonsoft.Json.Linq.JObject command= (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(text);
            
            var answer = command.Property("answer");
            if(answer != null && answer.ToObject<bool>()){
                // is an answer ...

                // get the response 
                var store = GetStoreForSocket(client, false);
                if(store != null){
                    TaskCompletionSource<object> taskPromise = null;
                    var taskid = command.Value<int>("taskid");
                    if(store.tasks.TryGetValue(taskid, out taskPromise)){

                        store.tasks.Remove(taskid);
                        JObject Result = command.Value<JObject>("result");
                        if(Result != null){
                            var dict = (IDictionary<string, object>)GetArgument(client, Result);
                            if(dict.ContainsKey("error")){
                                var error = new RemoteException((string)((IDictionary<string, object>)dict["error"])["message"]);
                                var Ex = (IDictionary<string, object>)dict["error"] ;
                                error.Code = (string)Ex["code"];
                                if(Ex.ContainsKey("stack")){
                                    error.Stack = (string)Ex["stack"];
                                }
                                taskPromise.SetException(error);
                            }
                            else{
                                taskPromise.SetResult(GetArgument(client, Result["data"]));
                            }
                        }
                    }
                }

            }
            else{

                object result = null;
                object[] args2 = null;
                try{
                    string targetid = command.Property("target").ToObject<string>();
                    object o = GetTarget(targetid);
                    TypeInfo t = null; 
                    if(o is TypeInfo){
                        t = (TypeInfo)o;
                        if(!t.noninstance){
                            t = ClassWrapper.GetFromObject(o);
                        }
                    }
                    else{
                        t = ClassWrapper.GetFromObject(o);
                    }


                    string method = command.Property("method").ToObject<string>();
                    
                    if(!t.methods.ContainsKey(method)){
                        if (method == "rpa_run")
                        {
                            method = "Invoke";
                            if (!t.methods.ContainsKey(method))
                            {
                                throw new Exception("Method " + method + " not found in " + targetid);
                            }
                        }else{
                            throw new Exception("Method " + method + " not found in " + targetid);
                        }
                    }

                    
                    JArray args1 = (JArray) command["arguments"];
                    args2 = GetArguments(client, args1);
                    if(t.noninstance){
                        result = t.methods[method].Invoke(null, args2);
                    }else{
                        result = t.methods[method].Invoke(o, args2);
                    }
                    if(result is System.Threading.Tasks.Task){
                        System.Threading.Tasks.Task task = (System.Threading.Tasks.Task) result;
                        await task ;

                        if(task.Exception != null){
                            SendAnswerError(client, command, task.Exception);
                        }else{
                            Type taskType = result.GetType();
                            Type[] generic= taskType.GetGenericArguments();
                            if(generic.Length > 0){
                                TypeInfo taskTypeInfo = ClassWrapper.GetFromObject(result);
                                result = taskTypeInfo.methods["get_Result"].Invoke(result, new object[]{});
                            }else{
                                result = null;
                            }
                            SendAnswer(client, command, result);
                        }
                    }else{
                        SendAnswer(client, command, result);
                    }
                }catch(Exception e){
                    SendAnswerError(client,command, e);
                }
                finally{
                    
                    try{
                        if(args2 != null && Autounref){
                            foreach(object arg in args2){
                                if(arg is DynamicRemoteObject ){
                                    var remote = (DynamicRemoteObject)arg;
                                    if (remote.preserved <= 0)
                                    {
                                        await remote.UnRef();
                                    }                              
                                }
                            }
                        }
                    }catch(Exception e){
                        Console.WriteLine("Failed unref: " + e.Message);
                    }

                }
                //Console.WriteLine(result);
                // send the result 
            }
            
        }

        public async void Connection(CrossSocket client){
            
            List<byte> alldata = new List<byte>();
            byte[] data = new byte[100];            
            int client1 = ccount++;

            while(client.Connected){                

                int received = await client.ReceiveAsync(data);
                int offset = 0;

                if(received > 0){
                    
                    while(true){
                        int searched= Array.IndexOf(data, (byte)10, offset);

                        if(searched >= 0 && searched < received){
                            byte[] newdata = new byte[searched - offset];
                            Array.Copy(data, offset, newdata, 0, searched - offset);
                            alldata.InsertRange(alldata.Count, newdata);

                            string text = Encoding.UTF8.GetString(alldata.ToArray());
                            alldata.Clear(); 
                            this.CommandReceived(client, text);
                            offset= searched+1;
                            
                        }
                        else{

                            byte[] newdata = new byte[received - offset];
                            Array.Copy(data, offset, newdata, 0, received - offset);
                            alldata.InsertRange(alldata.Count, newdata);
                            break;
                            
                        }
                    }
                }
                else{
                    client.Validate();
                }
            }

        }



    }
}
