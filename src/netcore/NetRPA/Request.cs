using System.Collections;
using System.Collections.Generic;

public class Request
{
    public string target;
    public string method;

    public List<string> props = new List<string>();

    public object[] arguments;

    public int taskid = 0;
}