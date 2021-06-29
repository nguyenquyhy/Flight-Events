using Microsoft.AspNetCore.Mvc;

namespace FlightEvents.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientVersionsController : ControllerBase
    {
        [Route("{version}")]
        [HttpGet]
        public ClientVersion Get(string version)
        {
            return version switch
            {
                "2.6.0.0" => new ClientVersion
                {
                    Version = version,
                    Features = new ClientFeatures
                    {
                        UseMessagePack = true
                    }
                },
                "2.5.2.0" => new ClientVersion
                {
                    Version = version,
                    Features = new ClientFeatures
                    {
                        UseWebpack = true
                    }
                },
                "2.5.1.0" => new ClientVersion
                {
                    Version = version,
                    Features = new ClientFeatures
                    {
                        UseWebpack = true
                    }
                },
                "2.5.0.0" => new ClientVersion
                {
                    Version = version,
                    Features = new ClientFeatures
                    {
                        UseWebpack = true
                    }
                },
                _ => new ClientVersion
                {
                    Version = version
                }
            };
        }
    }
    }
