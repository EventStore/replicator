using System;
using EventStore.ClientAPI;
using EventStore.Replicator.Shared.Logging;

namespace EventStore.Replicator.Tcp {
    public class TcpClientLogger : ILogger {
        static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        public void Error(string format, params object[] args) => Log.Error(format, args);

        public void Error(Exception ex, string format, params object[] args) => Log.Error(ex, format, args);

        public void Info(string format, params object[] args) => Log.Info(format, args);

        public void Info(Exception ex, string format, params object[] args) => Log.Info(ex, format, args);

        public void Debug(string format, params object[] args) => Log.Debug(format, args);

        public void Debug(Exception ex, string format, params object[] args) => Log.Debug(ex, format, args);
    }
}