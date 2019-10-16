
// import STDLIB for RPA Library 
// import 'https://kwx.kodhe.com/x/v/0.7.3/std/dist/stdlib'


import {Channel as RPA} from '/virtual/@kawix/std/rpa/channel'
import * as async from '/virtual/@kawix/std/util/async'
import uniqid from '/virtual/@kawix/std/util/uniqid'
import Child from 'child_process'
import Path from 'path'
import Os from 'os'



export class Channel{

        

    static async create() : Promise<RPA>{

        // open .NET Core child_process
        let file = Path.join(__dirname, "bin", "Debug", "netcoreapp3.0", "NetRPA")
        if(Os.platform() == "win32") file+= ".exe"

        let id = "netcore.rpa." + uniqid()
        let p = Child.spawn(file, [id])
        let def = new async.Deferred<void>()

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