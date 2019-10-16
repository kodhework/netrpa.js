import { Channel as NetRPA } from '../Channel'

if (!process.env.NETRPA_RETURN_FUNC) {
    Invoke().then(function () { }).catch(function (er) {
        console.error("Error executing sample: ", er)
        process.exit()
    })
}

export async function Invoke(channel?) {
    if (!channel) channel = await NetRPA.create()

    let compiler = await channel.client.CSharpCompiler()
    let Test = await compiler.CompileString(`


    using System;
    using System.Threading.Tasks;
    public class Test
    {
        public Person Invoke(int startingSalary)
        {
            var person = new Person(startingSalary);
            return person;
        }
    }

    public class Person
    {
        public int Salary { get; private set; }
        public Person(int startingSalary)
        {
            this.Salary = startingSalary;
        }
        public int GiveRaise(int amount)
        {
            this.Salary += amount;
            return this.Salary;
        }
    }


    `, 'Test')



    var person = await Test.Invoke(120)
    console.log(await person.get_Salary())
    console.log(await person.GiveRaise(100))
    console.log(await person.get_Salary())

    if (!process.env.NETRPA_RETURN_FUNC) channel.close()
}