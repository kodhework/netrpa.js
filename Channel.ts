
// import STDLIB for RPA Library 
// should be 0.7.4 or superior
import 'https://kwx.kodhe.com/x/v/0.7.4/std/dist/stdlib'


import {Channel as RPA} from '/virtual/@kawix/std/rpa/channel'
import * as async from '/virtual/@kawix/std/util/async'
import uniqid from '/virtual/@kawix/std/util/uniqid'
import Child, { ChildProcess } from 'child_process'
import Path from 'path'
import Os from 'os'
import fs from '/virtual/@kawix/std/fs/mod'
import Exception from '/virtual/@kawix/std/util/exception'



export class Channel{

        

    static async create() : Promise<RPA>{

        let def = new async.Deferred<void>()
        let p : ChildProcess
        let exe = "dotnet", dotnetversion = ''
        if(Os.platform() == "win32"){
            let pathvar = process.env.PATH.split(";")
            for(let path of pathvar){
                exe  = Path.join(path, "dotnet.exe")
                if(fs.existsSync(exe)) break 

                exe = ''
            }
        }
        if(!exe) throw Exception.create("Dotnet cannot be found").putCode("MISSING_NETCORE")

        p = Child.spawn(exe, ["--info"])
        p.stdout.on("data", function(d){
            d.toString().replace(/Microsoft\.NETCore\.App\s([0-9A-Za-z\.\-\_\s]+)\s\[/g, function(_, a){
                if(a &&  a >= dotnetversion)
                    dotnetversion = a 
            })
        })
        p.on("error", def.reject)
        p.on("exit" , def.resolve)
        await def.promise


        if(!dotnetversion){
            throw Exception.create("Dotnet version cannot be found").putCode("NETCORE_ERROR")
        }
        

        // read app version 
        let runtimeconfig = Path.join(__dirname, "bin", "netcore", "NetRPA.runtimeconfig.json")
        let content:any = await fs.readFileAsync(runtimeconfig, 'utf8')
        content = JSON.parse(content)
        if (content.runtimeOptions.framework.version != dotnetversion){
            content.runtimeOptions.framework.version = dotnetversion
            await fs.writeFileAsync(runtimeconfig, JSON.stringify(content))
        }



        // open .NET Core child_process
        let file = Path.join(__dirname, "bin", "netcore", "NetRPA.dll")

        let id = "netcore.rpa." + uniqid()
        p = Child.spawn(exe, [file, id])
        def = new async.Deferred<void>()

        p.stdout.on("data", function(d){

            process.stdout.write(d)
            if(def){
                let data = d.toString()
                if(data.indexOf("Application started") >= 0){
                    def.resolve()
                    def = null 
                    return 
                }
            }
            
        })

        p.stderr.on("data", function(d){
            process.stderr.write(d)
        })
        
        p.on("error", function(e){
            if(def){
                def.reject(e)
                def = null
            }
        })

        await def.promise


        let channel = await RPA.connectLocal(id)
        let close1 = channel.close
        channel.close = function(){
            close1.call(channel)
            p.kill('SIGINT')
        }
        return channel

    }


}