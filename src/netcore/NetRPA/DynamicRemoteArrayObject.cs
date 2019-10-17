using System.Collections;
using System.Collections.Generic;
using System;
using System.Dynamic;

namespace NetRPA{
    public class DynamicRemoteArrayObject : DynamicRemoteObject, System.Collections.IList
    {

        //List<object> list;

        public DynamicRemoteArrayObject()
        {
        }

        public DynamicRemoteArrayObject(Server server, CrossSocket client) : base(server, client)
        {
        }


        public new int Count
        {
            get
            {
                return (int)dictionary["length"];
            }
        }

        public int Add(object value)
        {
            int count = this.Count;
            this[count] = value;
            dictionary["length"] = count + 1;
            return count;
        }

        public bool Contains(object value)
        {
            return dictionary.ContainsValue(value);
        }

        public int IndexOf(object value)
        {
            foreach (KeyValuePair<string, object> Item in dictionary)
            {
                if (!Item.Key.StartsWith("rpa") && Item.Key.ToLower() != "length")
                {
                    if (value == Item.Value)
                    {
                        return int.Parse(Item.Key);
                    }
                }
            }
            return -1;
        }

        public void RemoveAt(int index)
        {
            if (dictionary.Remove(index.ToString()))
            {
                object o = null;
                while (true)
                {
                    if (dictionary.TryGetValue((index + 1).ToString(), out o))
                    {
                        dictionary[index.ToString()] = o;
                    }
                    else
                    {
                        break;
                    }
                    index++;
                }
                int count = (int)this["length"];
                this["length"] = count + 1;
            }


        }

        public void Insert(int index, object value)
        {
            int d = (int)dictionary["length"];
            object o = null;
            while (true)
            {
                if (dictionary.TryGetValue((d - 1).ToString(), out o))
                {
                    dictionary[d.ToString()] = o;
                }
                else
                {
                    break;
                }
                d--;
            }
            dictionary[index.ToString()] = value;
        }

        public void Remove(object value)
        {
            int index = this.IndexOf(value);
            if (index >= 0) RemoveAt(index);
        }

        public bool IsFixedSize
        {
            get
            {
                return false;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return true;
            }
        }

        public object SyncRoot
        {
            get
            {
                return dictionary;
            }
        }



        public object this[int a]
        {
            get
            {
                object o = null;
                dictionary.TryGetValue(a.ToString(), out o);
                return o;
            }
            set
            {
                dictionary[a.ToString()] = value;
            }
        }




        public void CopyTo(Array array, int count)
        {
            int len = this.Count;
            len = Math.Max(Math.Min(count, len), 0);
            for (int i = 0; i < len; i++)
            {
                array.SetValue(dictionary[i.ToString()], i);
            }

            //return len;
        }
        public new object ConvertTo(Type t)
        {
            int len = this.Count;
            if (t.IsArray)
            {
                var array = Array.CreateInstance(t.GetElementType(), len);
                this.CopyTo(array, len);
                return array;
            }
            return this;
        }

        public override bool TryGetMember(
        GetMemberBinder binder, out object result)
        {



            // Converting the property name to lowercase
            // so that property names become case-insensitive.
            string name = binder.Name;
            if (name == "Length")
            {
                result = dictionary["length"];
                return true;
            }

            return _TryGetMember(name, out result);
        }


    }
}