using System;
using System.IO;
using System.IO.Pipes;
// using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
namespace NetRPA{

    public class CrossSocket{

        internal Socket socket; 
        internal string pipeid;

        internal NamedPipeServerStream pipe;
        internal NamedPipeClientStream pipec;
        internal TaskCompletionSource<object> disconnectWaiter;

        

        public void Close(){
            if(socket != null){
                socket.Close();
            }else if(pipe != null){
                pipe.Close();
            }else if(pipec != null){
                pipec.Close();
            }
        }

        public bool Connected{
            get{
                if (pipe != null)
                {
                    return pipe.IsConnected;
                }
                if (pipec != null)
                {
                    return pipec.IsConnected;
                }
                return socket.Connected;
            }
        }
        
        public async Task<CrossSocket> AcceptAsync(){

            if(pipeid !=null && pipeid.Length != 0){
                NamedPipeServerStream nm = new NamedPipeServerStream(pipeid, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                await nm.WaitForConnectionAsync();

                var csocket = new CrossSocket();
                csocket.pipe = nm;
                csocket.pipeid = pipeid;
                return csocket;

            }
            else if(socket == null){
                throw new Exception("This socket is not server");
            }
            else{
                var socket= await this.socket.AcceptAsync();
                var csocket = new CrossSocket();
                csocket.socket = socket;
                return csocket;
            }

        }


        public async void Validate(){


            if(pipe != null || pipec != null){
                return; 
            }

            Socket client = socket;
            // This is how you can determine whether a socket is still connected.
            bool blockingState = client.Blocking;
            try
            {
                byte[] tmp = new byte[0];
                client.Blocking = false;
                await client.SendAsync(tmp,SocketFlags.None);
                //Console.WriteLine("Connected!");
            }
            catch (SocketException c)
            {
                // 10035 == WSAEWOULDBLOCK
                if (c.NativeErrorCode.Equals(10035))
                {
                }
                else
                {
                    _Terminate();
                }
            }
            finally
            {
                client.Blocking = blockingState;
            }
        }

        public async Task<int>  ReceiveAsync(byte[] buffer)
        {

            if (pipe != null)
            {
                try
                {
                    return await pipe.ReadAsync(buffer, 0, buffer.Length);
                }
                catch (Exception i)
                {
                    if (i is IOException)
                    {
                        _Terminate();
                    }
                    throw i;
                }
            }

            if (pipec != null)
            {
                try
                {
                    return await pipec.ReadAsync(buffer, 0, buffer.Length);
                }
                catch (Exception i)
                {
                    if (i is IOException)
                    {
                        _Terminate();
                    }
                    throw i;
                }
            }

            var task = new TaskCompletionSource<bool>();
            var e = new SocketAsyncEventArgs();                
            e.SetBuffer(buffer, 0, buffer.Length);
            e.Completed += (e, a) =>
            {
                task.SetResult(true);
            };
            if(socket.ReceiveAsync(e)){
                await task.Task;
            }
            return  e.BytesTransferred;
        }

        public async Task<int> SendAsync(byte[] buffer){
            if (pipe != null)
            {
                try
                {
                    await pipe.WriteAsync(buffer, 0, buffer.Length);
                    return buffer.Length;
                }
                catch (Exception e)
                {
                    if (e is IOException)
                    {
                        _Terminate();
                    }
                    throw e;
                }
            }
            if (pipec != null)
            {
                try
                {
                    await pipec.WriteAsync(buffer, 0, buffer.Length);
                    return buffer.Length;
                }
                catch (Exception e)
                {
                    if (e is IOException)
                    {
                        _Terminate();
                    }
                    throw e;
                }
            }
            return await socket.SendAsync(buffer, SocketFlags.None);
        }


        public void _Terminate(){
            if(disconnectWaiter != null){
                disconnectWaiter.SetResult(null);
            }
        }

        public Task WaitDisconnect(){
            disconnectWaiter = new TaskCompletionSource<object>();
            return disconnectWaiter.Task;
        }


    }
    class SocketWrapper
    {

        
        string id; 
        public SocketWrapper(string id){
            this.id = id;
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




        public static bool IsUnix
        {
            get
            {
                int p = (int)Environment.OSVersion.Platform;
                return (p == 4) || (p == 6) || (p == 128);
            }
        }


        public async Task<CrossSocket> Connect(){
            if (IsUnix)
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!((new DirectoryInfo(home)).Exists))
                {
                    home = "/Users/" + Environment.GetEnvironmentVariable("USER");
                }
                var kawi = home + "/.kawi";
                var kawidir = new DirectoryInfo(kawi);
                if (!kawidir.Exists) kawidir.Create();
                kawi = kawi + "/rpa";
                kawidir = new DirectoryInfo(kawi);
                if (!kawidir.Exists) kawidir.Create();

                var file = kawi + "/" + GetSha1Id();
                var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
                var unixEndPoint = new UnixEndPoint(file);
                await socket.ConnectAsync(unixEndPoint);
                CrossSocket csocket = new CrossSocket();
                csocket.socket = socket;
                return csocket;
            }else{
                
                var pipe = new NamedPipeClientStream(GetSha1Id());
                CrossSocket csocket = new CrossSocket();
                csocket.pipec = pipe;                
                return csocket;
                
            }
            

        }


        public async Task<bool> IsActive(){
            var serveractive = true;
            try{
                CrossSocket sock = await Connect();
                sock.Close();
            }
            catch (Exception)
            {
                serveractive = false;
            }
            return serveractive;
        }

        public async Task<CrossSocket> Create(){



            if(!IsUnix){
                
                
                var csocket = new CrossSocket();
                csocket.pipeid = GetSha1Id(); 
                return csocket;


            }
            else
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                if(!((new DirectoryInfo(home)).Exists)){
                    home = "/Users/" + Environment.GetEnvironmentVariable("USER");
                }
                var kawi = home + "/.kawi";
                var kawidir = new DirectoryInfo(kawi);
                if (!kawidir.Exists) kawidir.Create();
                kawi = kawi + "/rpa";
                kawidir = new DirectoryInfo(kawi);
                if (!kawidir.Exists) kawidir.Create();

                var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
                var file = kawi + "/" + GetSha1Id();
                var unixEndPoint = new UnixEndPoint(file);
                //Console.WriteLine(file);
                

                try{
                    socket.Bind(unixEndPoint);
                    socket.Listen(100);
                }catch(Exception){
                    bool isactive = await this.IsActive();
                    if(!isactive){
                        (new FileInfo(file)).Delete();
                        socket.Bind(unixEndPoint);
                        socket.Listen(100);
                    }else{
                        var ex = new RemoteException("RPA cannot register, id " + id + " is already used");
                        ex.Code = "RPA_ID_USED";
                        throw ex;
                    }
                }

                var csocket = new CrossSocket();
                csocket.socket = socket;
                return csocket;

            }

            
        }

    }


}