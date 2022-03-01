using EventStore.Replicator.Shared.Logging;
using Jint;
using Jint.Native;

// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

namespace EventStore.Replicator.JavaScript; 

public class JsFunction {
    protected readonly JsValue _func;

    public Engine Engine { get; }

    protected JsFunction(string jsFunc, string name) {
        var jsLog = new JsLog(name);

        Engine = new Engine(cfg => cfg.AllowClr())
            .SetValue("log", jsLog);

        _func = Engine.Execute(jsFunc).GetValue(name);
    }
}

public class TypedJsFunction<T, TResult> : JsFunction where T : class {
    readonly Func<JsValue, T, TResult> _convert;

    public TypedJsFunction(string jsFunc, string name, Func<JsValue, T, TResult> convert) : base(jsFunc, name)
        => _convert = convert;

    public TResult Execute(T arg) {
        var result = _func.Invoke(JsValue.FromObject(Engine, arg));
        return _convert(result, arg);
    }
}

class JsLog {
    readonly ILog _log;

    public JsLog(string context) => _log = LogProvider.GetLogger($"js-{context}");

    public void info(
        string  message,
        object? arg1 = null,
        object? arg2 = null,
        object? arg3 = null,
        object? arg4 = null,
        object? arg5 = null
    ) => _log.Info(message, arg1, arg2, arg3, arg4, arg5);

    public void debug(
        string  message,
        object? arg1 = null,
        object? arg2 = null,
        object? arg3 = null,
        object? arg4 = null,
        object? arg5 = null
    ) => _log.Debug(message, arg1, arg2, arg3, arg4, arg5);

    public void warn(
        string  message,
        object? arg1 = null,
        object? arg2 = null,
        object? arg3 = null,
        object? arg4 = null,
        object? arg5 = null
    ) => _log.Warn(message, arg1, arg2, arg3, arg4, arg5);

    public void error(
        string  message,
        object? arg1 = null,
        object? arg2 = null,
        object? arg3 = null,
        object? arg4 = null,
        object? arg5 = null
    ) => _log.Error(message, arg1, arg2, arg3, arg4, arg5);
}