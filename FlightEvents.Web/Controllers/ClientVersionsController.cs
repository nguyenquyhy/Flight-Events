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
#pragma warning disable CS0618 // Type or member is obsolete
                "2.5.2.0" => new ClientVersion
                {
                    Version = version,
                    Features = new ClientFeatures
                    {
                        UseMessagePack = true,
                        UseWebpack = true
                    }
                },
                "2.5.1.0" => new ClientVersion
                {
                    Version = version,
                    Features = new ClientFeatures
                    {
                        UseMessagePack = true,
                        UseWebpack = true
                    }
                },
                "2.5.0.0" => new ClientVersion
                {
                    Version = version,
                    Features = new ClientFeatures
                    {
                        UseMessagePack = true,
                        UseWebpack = true
                    }
                },
                _ => new ClientVersion
                {
                    Version = version
                }
#pragma warning restore CS0618 // Type or member is obsolete
            };
        }
    }
    }
