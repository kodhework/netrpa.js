using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
namespace NetRPA
{

    public class RemoteException : System.Exception
    {
        public string Code; 
        public string Stack;
        public RemoteException() { }
        public RemoteException(string message) : base(message) { }
        public RemoteException(string message, System.Exception inner) : base(message, inner) { }

        public override String StackTrace{
            get{
                return base.StackTrace + "\nRemote stack: " + this.Stack ;
            }
        }
        protected RemoteException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    
}
