using System;
using System.IO;
using EventStore.Replicator.Shared.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace EventStore.Replicator.Shared {
    public class Settings {
        static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        static readonly IDeserializer Yaml = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        public static Settings Load(string fileName) {
            try {
                var settingsFile = File.ReadAllText(fileName);
                return Yaml.Deserialize<Settings>(settingsFile);
            }
            catch (Exception e) {
                Log.Fatal(e, "Unable to read the configuration");
                return null;
            }
        }

        public SourceConnectionSettings Source { get; set; }
        public ConnectionSettings       Target { get; set; }

        public class ConnectionSettings {
            public string Connection { get; set; }
        }

        public class SourceConnectionSettings : ConnectionSettings {
            public StreamSettings    Stream { get; set; }
            public AllStreamSettings All    { get; set; }
        }

        public class StreamSettings {
            public string Name     { get; set; }
            public long   Position { get; set; }
        }

        public class AllStreamSettings {
            public long Prepare { get; set; }
            public long Commit  { get; set; }
        }
    }
}
