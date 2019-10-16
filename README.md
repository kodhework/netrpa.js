# NetRPA.js (NET Core inside Node.js)

This project allows using all .NET Core functionality on Windows, GNU/Linux and Mac from Node.js. This project aims to be a replacement of ```edge``` module, without relying on native (node-pregyp) dependencies

By default you can compile dynamically C# source code

If you will use with [```@kawix/core```](https://github.com/kodhework/kawix/blob/master/core/INSTALL.md) executable: 

### Import module

```javascript
import 'npm://@kawix/netrpa@0.1.0'
import {Channel as NetRPA} from 'netrpa'

main()
async function main(){
    let channel = await NetRPA.create()
    // ....
}

```


If you use with pure node: 

Install the modules:
```bash
npm install @kawix/core
npm install @kawix/netrpa
```

```javascript 
require("@kawix/core")
var NetRPAMod = require("@kawix/netrpa")

NetRPAMod.ready(function(NetRPA){
    NetRPA.create().then(function(channel){
        // ....
    }).catch(function(){})
}).error(function(e){
    console.error(e)
})
```


**NOTE**: From here, all examples will use the @kawix/core executable style, but you can use with nodejs without @kawix/core executable. 


### Usage

* See how NetRPA can be compatible with ```edge``` module for lambdas: 

```javascript
import 'npm://@kawix/netrpa@0.1.0'
import {Channel as NetRPA} from 'netrpa'

main()
async function main(){
    
    var Channel = await NetRPA.create()
    var CsharpCompiler = await Channel.client.CSharpCompiler()
    var helloWorld = await CsharpCompiler.CompileLambdaString(`
    async (input) => {
        return ".NET Welcomes " + input.ToString();
    }
    `)
    console.info(await helloWorld("Javascript"))

}
```

Previous example will work, but you can get faster results using full Class declaration with strong types: 

```javascript
import 'npm://@kawix/netrpa@0.1.0'
import {Channel as NetRPA} from 'netrpa'

main()
async function main(){
    
    var Channel = await NetRPA.createChannel()
    var CsharpCompiler = await Channel.client.CSharpCompiler()
    var TestClass = await CsharpCompiler.CompileString(`
    class Test{
        public string Hello(string input){
            return ".NET Welcomes " + input;
        }
    }
    `, 'Test')
    console.info(await TestClass.Hello("Javascript"))

}
```

This second example produces the same results, but will be quite faster than compiling lambda. 


### Why NetRPA? 

* No native dependencies
* Allow using any class in NET Core environments (maybe later NET framework)
* Easier for use than edge.js and Faster in some cases
* Always async javascript client side 
* No require follow an strict declaration style 


### How works NetRPA?

NetRPA use RPA protocol, is a  custom made and good design protocol for inter process comunication.
In Unix uses unix sockets, in windows use named pipes.
You can create a RPA Channel to .NET Core using: 

```javascript
var Channel = await NetRPA.createChannel()
```

Channel have some methods like: 

```javascript
class NetRPA.Channel{
    plain(target: object){
        // gets and object that can be used as parameters for Call,
        // indicates arguments will be serialized, and not proxied to NET core
    }

    get client() : NetRPA.AssemblyManager {
        // get the main NET Core  service(object)  exported  
        // The service is a NetRPA.AssemblyManager instance Proxy
        // The NetRPA.AssemblyManager is defined on c# code
    }
}
``` 

You can inspect the code of AssemblyManager in the c# project. Here some of its usefull methods: 

```typescript


interface TypeDef{
    type: string | TypeDef,
    generic?: Array<string | TypeDef>
}

class NetRPA.AssemblyManager{
    async LoadAssemblyFile(file: string) : Promise<void>{
        // Load an assembly file. Types present on assembly will be loaded
        // and available for use in GetType and GetTypeDefinition methods
        // NOTE: Assembly loaded with this method, will be referenced in future compilations
        await Channel.client.LoadAssemblyFile("/path/to/mydll")
    }

    async LoadAssembly(name: string) : Promise<void>{
        // Load an assembly by full name. Types present on assembly will be loaded
        // and available for use in GetType and GetTypeDefinition methods
        // NOTE: Assembly loaded with this method, will be referenced in future compilations   
    }

    async LoadAssemblyPartialName(name: string) : Promise<void>{
        // Load an assembly by partial name. This is deprecated in .NET. Types present on assembly will be loaded
        // and available for use in GetType and GetTypeDefinition methods
        // NOTE: Assembly loaded with this method, will be referenced in future compilations   
        await Channel.client.LoadAssemblyPartialName("System.Xml.dll")
    }

    async LoadAssembly(name: System.Reflection.Assembly): Promise<void>{
        // Load an assembly and types present on assembly will be loaded
        // and available for use in GetType and GetTypeDefinition methods
        // NOTE: Assembly loaded with this method, will be referenced in future compilations   

        // example: 
        var assembly = await (await Channel.client.GetType("System.String")).get_Assembly()
        // of course this is not required, because mscorlib and System.dll is loaded by default
        await Channel.client.LoadAssembly(assembly)

    }




    async GetType(type: string | TypeDef) : Promise<System.Type>{
        // returns the System.Type from type parameter
        // The Net core type should be loaded using LoadAssembly methods in this object

        // example 
        var Type = await Channel.client.GetType({
            type: 'System.Collections.Hashtable'
        })
        console.info(await Type.get_AssemblyQualifiedName())

    }

    async GetTypeDefinition(type: string | TypeDef) : Promise<Proxy>{
        // return a proxied instance representing the Static Class of the NET Core Type
        // The Net core type should be loaded using LoadAssembly methods in this object
        // How use Generic Types? 
        //
        
        // this an example: 
        var collectionClass = await Channel.client.GetTypeDefinition({
            type: 'System.Collections.Generic`2'
            generic: ['System.String', 'System.Object']
        })
        var collection = await collectionClass[".ctor"]()
        await collection.Add("name", "James")
        await collection.Add("age", 25)

    }


    async CreateNewScope() : Promise<NetRPA.AssemblyManager>{
        // create a new instance of assemblymanager
        // is usefull if you want compile code with different referenced assemblies

        await Channel.client.LoadAssemblyPartialName("System.Xml.dll")
        // OK 
        var XmlDocumentClass = await Channel.client.Construct("System.Xml.XmlDocument")
        await XmlDocumentClass.Load("/path/to/file.xml")
        
        var scope = await Channel.client.CreateNewScope()
        // throws error, System.Xml dll not loaded
        XmlDocumentClass = await scope.Construct("System.Xml.XmlDocument")

    }

    async CSharpCompiler(): Promise<NetRPA.Compiler.CSharp>{
        // return an instance of compiler for csharp code (Roslyn)
    }

    async Construct(type: TypeDef | string, params?: Array<object>): Promise<Proxy>{
        // construct an instance of .NET Core type
        // example: 


        var collection = await Channel.client.Construct({
            type: 'System.Collections.Generic`2'
            generic: ['System.String', 'System.Object']
        })

        // is the same:         

        var collectionClass = await Channel.client.GetTypeDefinition({
            type: 'System.Collections.Generic`2'
            generic: ['System.String', 'System.Object']
        })
        var collection = await collectionClass[".ctor"]()
    }

}


class NetRPA.Compiler.CSharp{
    async CompileLambdaString(source: string):
        Promise<System.Func<object, System.Threading.Tasks.Task<Object>>>
    {
        // compile a lambda function and returns as
        // System.Func<object, System.Threading.Tasks.Task<Object>> proxied object
        // (callable as a function in javascript or using Invoke method)

        var CsharpCompiler = await Channel.client.CSharpCompiler()
        var helloWorld = await CsharpCompiler.CompileLambdaString(`
        async (input) => {
            return ".NET Welcomes " + input.ToString();
        }
        `)

        // using as function
        console.info(await helloWorld("Javascript"))

        // using Invoke method
        console.info(await helloWorld.Invoke("Javascript"))
    }

    async CompileString(source: string, type?: string): Promise<Proxy>{

        // compile a string as an assembly, and optionally returns 
        // a proxy object representing the static class  passed as parameter type

        var CsharpCompiler = await Channel.client.CSharpCompiler()
        var TestClass = await CsharpCompiler.CompileString(`
        class Test{
            public string Hello(string input){
                return ".NET Welcomes " + input;
            }
        }
        `, 'Test')
        console.info(await TestClass.Hello("Javascript"))

        // you can also omit the type parameter, and use like any other class

        CsharpCompiler = await Channel.client.CSharpCompiler()
        await CsharpCompiler.CompileString(`
        namespace MyNamespace{
            class Important{
                public string Hello(string input){
                    return ".NET Welcomes " + input;
                }
            }
        }
        `)
        var ImportantClass = await Channel.client.Construct("MyNamespace.Important")
        console.info(await ImportantClass.Hello("Javascript"))

    }




}

```

#### RPA Serialization

RPA Provides two types of serialization from javascript to C#: 

* Plain objects passed in c# as ExpandoObject (IDictionary<string, object>) 

```javascript

import 'npm://@kawix/netrpa@0.1.0'
import {Channel as NetRPA} from 'netrpa'

main()
async function main(){
    
    var Channel = await NetRPA.createChannel()
    var CsharpCompiler = await Channel.client.CSharpCompiler()
    var TestClass = await CsharpCompiler.CompileString(`
    class Test{
        public void ReadInput(dynamic input){
            Console.WriteLine("Your name is " + (string)input.name + " and your age is " + input.age.ToString() );
        }
    }
    `, 'Test')
    await TestClass.Hello(Channel.plain({
        name: 'James',
        age: 25
    }))

}
```


* Proxied objects passed in c# as DynamicRemoteObject (DynamicObject, IDictionary<string, object>)

**NOTE**: By default, RPA uses a mixed Plain/Proxied serialization. If ```parameter``` protype is object, all properties with primitive values will be passed as plain, but also is passed as Proxy for make available its functions. For understand this see the example: 


```javascript

import 'npm://@kawix/netrpa@0.1.0'
import {Channel as NetRPA} from 'netrpa'

main()
async function main(){
    
    var Channel = await NetRPA.createChannel()
    var CsharpCompiler = await Channel.client.CSharpCompiler()

    

    var TestClass = await CsharpCompiler.CompileString(`
    class Test{
        public async Task<int> ReadInput(dynamic input){
            
            return await input.add(input.a, input.b);
        }
    }
    `, 'Test')

    // in c# code 
    // input.a and input.b can be readed sync because will be passed
    // plain serialized, and function input.add can be called only async, because is proxied
    await TestClass.ReadInput({
        a: 10,
        b: 5,
        add(a, b){
            return a + b
        }
    })
}
```


From C# to Javascript 

* All types primitive (string,int,float,etc) will be serialized in plain mode.

* All object types will be Proxied to javascript (not mixed like javascript to C#)

```javascript

import 'npm://@kawix/netrpa@0.1.0'
import {Channel as NetRPA} from 'netrpa'

main()
async function main(){
    
    var Channel = await NetRPA.createChannel()
    var CsharpCompiler = await Channel.client.CSharpCompiler()

    

    var TestClass = await CsharpCompiler.CompileString(`
    class Person{
        public string Name{get;set;}
        public int Age{get;set;}
    }
    class Test{
        public Person CreatePerson(){
            var person= new Person();
            person.Name = "James";
            person.Age = 25;
            return person; 
        }
    }
    `, 'Test')

    
    var person = await TestClass.CreatePerson()
    // from javascript can access to properties only by async calls 
    // because object is proxied 
    console.info("My name: ", await person.get_Name(), " my age: ", await person.get_Age())
}
```

* You can also pass types plain serialized, using the type NetRPA.Value<T>


```javascript

import 'npm://@kawix/netrpa@0.1.0'
import {Channel as NetRPA} from 'netrpa'

main()
async function main(){
    
    var Channel = await NetRPA.createChannel()
    var CsharpCompiler = await Channel.client.CSharpCompiler()

    

    var TestClass = await CsharpCompiler.CompileString(`
    class Person{
        public string Name{get;set;}
        public int Age{get;set;}
    }
    class Test{
        public Value<Person> CreatePerson(){
            var person= new Person();
            person.Name = "James";
            person.Age = 25;
            return new Value<Person>(person); 
        }
    }
    `, 'Test')

    
    var person = await TestClass.CreatePerson()
    // from javascript can access to properties in sync mode
    // because object is plain serialized 
    console.info("My name: ", person.Name, " my age: ", person.Age)
}
```

### Exceptions 

Exceptions are fully managed with ```NetRPA```.

Remember always use the ```await``` keyword when calling functions from CLR to Node.js or viceversa. 


```javascript
// BAD, you get an unresolved promise
// and your app will close due to UncaughtException 
var type = channel.client.Construct("System.NonExistentType")

// GOOD you can get the error message
try{
    var type = await channel.client.Construct("System.NonExistentType")
}catch(e){
    console.error(e)
}

```


### Migrate from edge / Samples

You can see the ```samples``` folder. Current samples are extracted from npm ```edge``` package, and translated to use with ```NetRPA```. You can run the samples easy: 

```bash
cd samples
kwcore 207_zip
```


### Created by 

Kodhe, Copyright 2019©

You can contribute reporting bugs, improving README, or adding functionality. You are welcome to contribute