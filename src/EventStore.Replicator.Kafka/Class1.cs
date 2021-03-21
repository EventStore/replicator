using System;
using Confluent.Kafka;

namespace EventStore.Replicator.Kafka {
    public class Class1 {

        public void Test() {
            var config          = new ProducerConfig();
            var producerBuilder = new ProducerBuilder<string, byte[]>(config);
            var producer        = producerBuilder.Build();

            var message = new Message<string, byte[]> { };
            producer.Produce("key", message, Report);
                
        }

        void Report(DeliveryReport<string, byte[]> report) {
            // report.Status
        }
    }
}