import { Channel as NetRPA } from '../Channel'
import fs from 'fs'
import Path from 'path'

process.env.NETRPA_RETURN_FUNC = "1"

Invoke()

export async function Invoke(){
    let files = fs.readdirSync(__dirname)
    let main, rpa 
    rpa = await NetRPA.create()

    for(let i=0;i<files.length;i++){
        let file = files[i]
        if(file.endsWith(".ts") && file != "0_all.ts"){
            let ufile = Path.join(__dirname, file)
            main = await import(ufile)
            try{
                console.info("")
                console.info("> Executing sample: ", file)
                await main.Invoke(rpa)
            }catch(e){
                console.error("Failed executing sample: ", e)
            }
        }
    }
}