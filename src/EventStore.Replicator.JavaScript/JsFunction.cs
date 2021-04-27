using System;
using EventStore.Replicator.Shared.Logging;
using Jint;
using Jint.Native;

namespace EventStore.Replicator.JavaScript {
    public class JsFunction {
        protected readonly JsValue _func;

        public JsFunction(string jsFunc, string name) {
            var log    = LogProvider.GetLogger($"js-{name}");
            var engine = new Engine().SetValue("log", new Action<string>(log.Debug));

            _func = engine.Execute(jsFunc).GetValue(name);
        }
    }

    public class TypedJsFunction<T, TResult> : JsFunction where T : class {
        readonly Func<JsValue, T, JsValue> _execute;
        readonly Func<JsValue, T, TResult> _convert;

        public TypedJsFunction(
            string jsFunc, string name, Func<JsValue, T, JsValue> execute, Func<JsValue, T, TResult> convert
        )
            : base(jsFunc, name) {
            _execute = execute;
            _convert = convert;
        }

        public TResult Execute(T arg) {
            var result = _execute(_func, arg);
            return _convert(result, arg);
        }
    }
}