using System;
using Microsoft.ClearScript.V8;

namespace EventStore.Replicator.JavaScript {
    public class JsFunction {
        public JsFunction(string jsFunc) {
            var engine = new V8ScriptEngine();
            engine.Execute(jsFunc);
            Script = engine.Script;
        }

        public dynamic Script { get; }
    }

    public class TypedJsFunction<T, TResult> : JsFunction {
        readonly Func<dynamic, T, dynamic> _execute;
        readonly Func<dynamic, T, TResult>    _convert;

        public TypedJsFunction(string jsFunc, Func<dynamic, T, dynamic> execute, Func<dynamic, T, TResult> convert)
            : base(jsFunc) {
            _execute = execute;
            _convert = convert;
        }

        public TResult Execute(T arg) {
            var result = _execute(Script, arg);
            return _convert(result, arg);
        }
    }
}