using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetRPA{
    public class SocketStore
    {
        public Dictionary<int, TaskCompletionSource<object>> tasks = new Dictionary<int, TaskCompletionSource<object>>();
        public Dictionary<string, int> refs = new Dictionary<string, int>();
    }
}