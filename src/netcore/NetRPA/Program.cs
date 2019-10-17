using System;



using System.Collections;
using System.Collections.Generic;
namespace NetRPA
{
    class Program
    {

        static void Main(string[] args)
        {

            if(args.Length > 0){

                var c = new Server(args[0], new AssemblyManager());
                c.Create();

                while(true){
                    Console.WriteLine("Application started.");
                    Console.ReadLine();
                }

            }


        }
    }
}
