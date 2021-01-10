using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Jering.Javascript.NodeJS;
using Microsoft.ClearScript;
using Microsoft.ClearScript.JavaScript;
using Microsoft.ClearScript.V8;

namespace EventStore.Replicator.Transforms.NodeJS {
    public static class JsExperiments {
        static V8ScriptEngine engine = new();

        static async Task TestJs() {
            // TestV8();
            // return;

            var javascriptModule = @"
const { MongoClient } = require('mongodb');
const uri  = 'mongodb://mongoadmin:secret@localhost:27017';
const client  = new MongoClient(uri);

module.exports = {
    async test(x) {
        return { test: 'blah', ...x }
    },
    async query(x, y) {
        const database    = client.db('sample_mflix');
        const collection  = database.collection('movies');
        const query  = { title: 'Back to the Future' };
        const movie  = await collection.findOne(query);
        return query;
    },
    async open() { 
        await client.connect(); 
    },
    async close() { 
        await client.close(); 
    }
}";

            var w = new Stopwatch();
            w.Start();
            // await StaticNodeJSService.InvokeFromStringAsync(javascriptModule, "test", "open");

            // for (var x = 0; x < 10000; x++) {
            var result = await StaticNodeJSService.InvokeFromStringAsync<object>(
                javascriptModule,
                "test",
                "test",
                new object[] {new {Blah = "ghgh"}}
            );
            // }
            // await StaticNodeJSService.InvokeFromStringAsync(javascriptModule, "test", "close");

            w.Stop();
            Console.WriteLine(w.ElapsedMilliseconds);
            return;

            // using var module = engine.Compile(new DocumentInfo { Category = ModuleCategory.Standard }, @"
            //     import * as Geometry from 'Scripts/geometry.js';
            //     new Geometry.Square(25).Area;
            // ");
            // engine.DocumentSettings.AccessFlags = DocumentAccessFlags.EnableFileLoading;
            // Console.WriteLine(engine.Evaluate(module));
            // return;

            using var module1 = engine.Compile(
                new DocumentInfo {Category = ModuleCategory.CommonJS},
                @"
            const { MongoClient } = require('Scripts/node_modules/mongodb/index.js');
            // Replace the uri string with your MongoDB deployment's connection string.
            const uri  = 'mongodb://mongoadmin:secret@localhost:27017';
            const client  = new MongoClient(uri);
            async function run() {
                try {
                    await client.connect();
                    const database    = client.db('sample_mflix');
                    const collection  = database.collection('movies');
                    // Query for a movie that has the title 'Back to the Future'
                    const query  = { title: 'Back to the Future' };
                    const movie  = await collection.findOne(query);
                    console.log(movie);
                } finally {
                    // Ensures that the client will close when you finish/error
                    await client.close();
                }
            }
            run().catch(console.dir);
            "
            );
            engine.DocumentSettings.AccessFlags = DocumentAccessFlags.EnableFileLoading;

            Console.WriteLine(engine.Evaluate(module1));

            return;
        }

        static void TestV8() {
            var func = @"
function myFunc(x, y, z) {
    return { xValue: x, yValue: y, zValue: z };
}

function json(o) { return JSON.stringify(o); }

function test(evt) { return json(myFunc(evt.x, evt.y, evt.z)); }
";
            var v = engine.Compile(func);
            engine.Execute(v);

            var watch = new Stopwatch();
            watch.Start();

            for (var i = 0; i < 10000; i++) {
                var evt = new {x = 1, y = 2.0, z = "tree"};
                var res = engine.Script.test(evt);
            }

            watch.Stop();
            Console.WriteLine(watch.ElapsedMilliseconds);
        }
    }
}
