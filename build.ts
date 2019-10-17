import 'https://kwx.kodhe.com/x/v/0.7.3/std/dist/stdlib'
import Child, { ChildProcess } from 'child_process'
import Os from 'os'
import * as async from '/virtual/@kawix/std/util/async'
import Path from 'path'
import fs from '/virtual/@kawix/std/fs/mod'
import Exception from '/virtual/@kawix/std/util/exception'


main()

async function main(){
    
    // build the project 
    let def = new async.Deferred<void>()
    let p: ChildProcess
    let exe = "dotnet", dotnetversion
    if (Os.platform() == "win32") {
        let pathvar = process.env.PATH.split(";")
        for (let path of pathvar) {
            exe = Path.join(path, "dotnet.exe")
            if (fs.existsSync(exe)) break

            exe = ''
        }
    }

    if (!exe) throw Exception.create("Dotnet cannot be found").putCode("MISSING_NETCORE")



    p = Child.spawn(exe, ["build"], {
        cwd: Path.join(__dirname, "src", "netcore", "NetRPA")
    })
    if(Os.platform() == "win32") p.stdout.setEncoding('latin1')
    p.stdout.on("data", function (d) {
        process.stdout.write(d)
    })
    if(Os.platform() == "win32") p.stderr.setEncoding('latin1')
    p.stderr.on("data", function (d) {
        process.stderr.write(d)
    })
    p.on("error", def.reject)
    p.on("exit", def.resolve)
    await def.promise

    
    // Ahora copiar los archivos
    let out = Path.join(__dirname, "bin", "netcore")
    if(!fs.existsSync(out))
        await fs.mkdirAsync(out)
    
    let netcore = Path.join(__dirname, "src", "netcore", "NetRPA", "bin", "Debug")
    let filesx = await fs.readdirAsync(netcore)
    for (let i = 0; i < filesx.length; i++) {
        let file0 = filesx[i]
        if (file0.startsWith("netcoreapp")) {
            netcore = Path.join(netcore, file0)
            break
        }
    }


    // COPY FILES ...
    let files = [
        "Microsoft.CodeAnalysis.CSharp.dll",
        "Microsoft.CodeAnalysis.dll",
        "Microsoft.CodeAnalysis.VisualBasic.dll",
        "NetRPA.deps.json",
        "NetRPA.dll",
        "NetRPA.runtimeconfig.json",
        "Newtonsoft.Json.dll"
    ]

    for(let tocopy of files){
        await fs.copyFileAsync(Path.join(netcore, tocopy), Path.join(out, tocopy))
    }

}