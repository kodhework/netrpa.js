
var netrpa = module.exports = exports = {
    Channel: null, 
    ready: function(ready){
        this._ready = ready
        return this 
    },
    error: function(func){
        this._error  = func 
        return this 
    }

}


// start RPA 
if(!global.kawix){
    netrpa._error && netrpa._error(new Error("You need load @kawix/core"))
    return 
}


global.kawix.KModule.import(__dirname + "/Channel.ts").then(function(mod){
    netrpa.Channel = mod.Channel 
    netrpa._ready && netrpa._ready(mod.Channel)
}).catch(function(e){
    netrpa._error && netrpa._error(e) 
})
