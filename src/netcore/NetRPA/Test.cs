using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace NetRPA
{



    public class Test
    {
        

        public async Task<int> TestParam(dynamic param){
            int a = param.a; 
            int b = param.b;
            return await param.add(a, b);
        }

        public async Task Execute(dynamic remote){
            await remote("Hello James!");
        }

        public Task<int> SumAsync(int a, int b)
        {
            return Task.FromResult<int>(a+b);
        }

        public int Sum(int a, int b){
            return a+b;
        }

    }
}
