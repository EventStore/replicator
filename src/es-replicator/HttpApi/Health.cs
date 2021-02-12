using Microsoft.AspNetCore.Mvc;

namespace es_replicator.HttpApi {
    [Route("")]
    public class Health : ControllerBase {
        [HttpGet]
        [Route("/ping")]
        public string Ping() => "Pong";
        
        [HttpGet]
        [Route("/health")]
        public string Healthy() => "OK";
    }
}
