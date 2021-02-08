using Microsoft.AspNetCore.Mvc;

namespace es_replicator {
    [Route("")]
    public class HealthController : ControllerBase {
        [HttpGet]
        [Route("/ping")]
        public string Ping() => "Pong";
        
        [HttpGet]
        [Route("/health")]
        public string Health() => "OK";
    }
}
