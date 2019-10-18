
// use this file for run samples from nodejs instead of kawix/core

var Path  = require("path")
var sample = process.argv[2]
if (!sample) {
    console.info("Specify a sample for run")
    process.exit(0)
}

sample = Path.resolve(__dirname, sample)


process.env.NETRPA_RETURN_FUNC = "1"
require("@kawix/core")
var NetRPAModule = require("..")

NetRPAModule.ready(function (NetRPA) {
    NetRPA.create().then(function (channel) {
        // ignore promise errors
        kawix.KModule.import(sample).then(function(mod){
            mod.Invoke(channel).then(process.exit.bind(process)).catch(process.exit.bind(process,1))
        })
    }).catch(function () { })
}).error(function (e) {
    console.error(e)
})