import { Channel as NetRPA } from '../Channel'
import * as async from '/virtual/@kawix/std/util/async'



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
        public async Task<Action> Invoke(dynamic input)
        {

            // Parameters in NetRPA need to be preserved, if you want use 
            // after method execution completes
            input.Preserve();


            // Create a timer with the specifed interval. 
            // Conceptually this can be any event source. 
            var timer = new System.Timers.Timer(input.interval);

            // Hook up the Elapsed event for the timer and delegate 
            // the call to a Node.js event handler. 
            // Remember use NetRPA.Value for serialize the value insted of proxying
            timer.Elapsed += (Object source, System.Timers.ElapsedEventArgs e) => {
                input.event_handler(new NetRPA.Value<System.Timers.ElapsedEventArgs>(e));
            };


            // Start the timer            
            timer.Enabled = true;


            // Return a function that can be used by Node.js to 
            // unsubscribe from the event source.
            return (Action)(() => {

                // Is a good practice UnRef the input parameter
                input.UnRef();

                timer.Enabled = false;
            });      

        }
    }
    
    `, 'Test')

    var unsuscribe = await Test.Invoke({
        interval: 2000,
        event_handler: function (data) {
            console.log('Received event', data)
        }
    })
    console.log('Subscribed to .NET events. Unsubscribing in 8 seconds...');
    
    await async.sleep(8000)
    await unsuscribe()


    console.log('Unsubscribed from .NET events.');
    console.log('Waiting 5 seconds before exit to show that no more events are generated...')
    await async.sleep(5000)



    if (!process.env.NETRPA_RETURN_FUNC) channel.close()
}