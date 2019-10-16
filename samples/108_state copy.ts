import { Channel as NetRPA } from '../Channel'

main()
async function main() {
    let channel = await NetRPA.create()
    let compiler = await channel.client.CSharpCompiler()
    let Test = await compiler.CompileString(`

    using System;
    using System.Threading.Tasks;

    class Test{
        public int counter = 0;
        public Func<int> SetCounter(int number){
            counter = number;
            return new Func<int>(Increment);
        }
        public int Increment(){
            return counter++;
        }
    }


    `, 'Test')



    var nextNumber = await Test.SetCounter(12)
    console.log(await nextNumber(null))
    console.log(await nextNumber(null))
    console.log(await nextNumber(null))
    console.log(await nextNumber(null))

    channel.close()
}