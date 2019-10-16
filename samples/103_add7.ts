import { Channel as NetRPA } from '../Channel'

main()
async function main() {
    let channel = await NetRPA.create()
    let compiler = await channel.client.CSharpCompiler()
    let Add7 = await compiler.CompileString(`
    
    class Test{
        public int Invoke(int input){
            return Helper.Add7(input);
        }
    }

    static class Helper{
        public static int Add7(int v)
		{
			return v + 7;
		}
    }

    `, 'Test')

    console.info(await Add7.Invoke(12))
    console.info(await Add7.Invoke(20))

    channel.close()
}