
using System;
using System.Threading.Tasks;
using System.Dynamic;
using System.Collections.Generic;
using System.Reflection;


namespace NetRPA{




    public class DynamicRemoteObject : DynamicObject, IDictionary<string, object>
    {

        internal Dictionary<string, object> dictionary = new Dictionary<string, object>();
        CrossSocket socket;
        Server server;
        internal int preserved = 0;



        public bool TryGetValue(string a, out object b)
        {
            return dictionary.TryGetValue(a, out b);
        }

        public object this[string a]
        {
            get
            {
                return dictionary[a];
            }
            set
            {
                dictionary[a] = value;
            }
        }

        public ICollection<string> Keys
        {
            get
            {
                return dictionary.Keys;
            }
        }

        public ICollection<object> Values
        {
            get
            {
                return dictionary.Values;
            }
        }

        public void Add(KeyValuePair<string, object> item)
        {
            this.dictionary.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            this.dictionary.Clear();
        }



        public bool ContainsKey(string key)
        {
            return this.dictionary.ContainsKey(key);
        }

        public bool Contains(KeyValuePair<string, object> key)
        {
            return dictionary.ContainsKey(key.Key);
        }

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }


        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }





        public bool Remove(string key)
        {
            return this.dictionary.Remove(key);
        }

        public bool Remove(KeyValuePair<string, object> key)
        {
            return this.dictionary.Remove(key.Key);
        }

        public void Add(string key, object value)
        {
            this.dictionary.Add(key, value);
        }


        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }


        public int Count
        {
            get
            {
                return dictionary.Count;
            }
        }



        public void CopyTo(KeyValuePair<string, object>[] keyValue, int count)
        {
            int i = 0;
            foreach (KeyValuePair<string, object> item in dictionary)
            {
                if (i >= count) break;
                keyValue[i] = item;
            }
        }












        public DynamicRemoteObject()
        {
        }

        public DynamicRemoteObject(Server server, CrossSocket client)
        {
            socket = client;
            this.server = server;
        }

        public void Preserve()
        {
            this.preserved = 1;
        }

        public async Task UnRef()
        {
            
            string ids = "";
            if(dictionary.ContainsKey("rpa_references")){
                object[] rids = (object[])dictionary["rpa_references"];
                ids = String.Join(',', rids);
            }else{
                if(dictionary.ContainsKey("rpa_id")){
                    ids = (string)dictionary["rpa_id"];
                }
            }
            if(ids != ""){

                // Console.WriteLine("unrefing>" + ids);
                
                Request req = new Request();
                req.method = "UnRef";
                req.target = "R>y";
                req.arguments = new object[] { ids, RemoteSocketObject.instance };
                await server.Send(socket, req, true);
                return;
            }
        }


        public static implicit operator Dictionary<string, object>(DynamicRemoteObject r)
        {
            return r.dictionary;
        }

        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            if (server == null){
                result = null;
                return false;
            }
            result = RemoteSender.Invoke(server, socket, (string)dictionary["rpa_id"], args);
            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result){
            if (server == null)
            {
                result = null;
                return false;
            }


            object f = null;

            if(_TryGetMember(binder.Name, out f)){
                var Func = (Func<object[], Task<object>>)f;
                result = Func.Invoke(args);
                return true;
            }

            result = null;
            return false;
        }

        public override bool TryGetMember(
        GetMemberBinder binder, out object result)
        {

            

            // Converting the property name to lowercase
            // so that property names become case-insensitive.
            string name = binder.Name;
            return _TryGetMember(name, out result);
        }

        public bool _TryGetMember(string name, out object result)
        {

            // If the property name is found in a dictionary,
            // set the result parameter to the property value and return true.
            // Otherwise, return false.
            if (!dictionary.TryGetValue(name, out result))
            {
                if(server == null) return false;


                if (name == "UnRef" || name == "rpa_unref")
                {
                    result = new Func<Task>(this.UnRef);
                    dictionary.Add(name, result);
                }
                else if (name == "Preserve" || name == "rpa_preserve")
                {
                    result = new Action(this.Preserve);
                    dictionary.Add(name, result);
                }
                else if (name == "Invoke")
                {
                    result = new Func<object[], Task<object>>(this.Invoke);
                    dictionary.Add(name, result);
                }

                else
                {
                    result = new Func<object[], Task<object>>((object[] args) =>
                    {
                        return RemoteSender.InvokeMethod(server, socket, (string)dictionary["rpa_id"], name, args);
                    });
                    dictionary.Add(name, result);
                }
            }
            return true;
        }

        public object ConvertTo(Type t){

            if(t == typeof(Func<object, Task<object>>) || t == typeof(Func<object[], Task<object>>)){
                if(dictionary.ContainsKey("rpa_function") && (bool)dictionary["rpa_function"]){
                    if (t == typeof(Func<object, Task<object>>))
                    {
                        return new Func<object, Task<object>>((object arg) =>
                        {
                            return RemoteSender.Invoke(server, socket, (string)dictionary["rpa_id"], new object[] { arg });
                        });
                    }
                    else if (t == typeof(Func<object[], Task<object>>))
                    {
                        return new Func<object[], Task<object>>((object[] args) =>
                        {
                            return RemoteSender.Invoke(server, socket, (string)dictionary["rpa_id"], args);
                        });
                    }
                }else{
                    var ex = new RemoteException("Cannot cast this object as a Function");
                    ex.Code = "INVALID_CAST_EXCEPTION";
                    throw ex; 
                }
            }
            else if(t == typeof(Dictionary<string, object>)){
                return dictionary;
            }
            return this;
        }


        public Task<object> Invoke()
        {
            return RemoteSender.Invoke(server, socket, (string)dictionary["rpa_id"]);
        }
        public Task<object> Invoke(object[] args)
        {
            return RemoteSender.Invoke(server, socket, (string)dictionary["rpa_id"], args);
        }

    }
}