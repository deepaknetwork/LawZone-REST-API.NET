using last.Models;
using last.Services;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace last.Controllers
{
    [ApiController]
    [Route("")]
    public class WeatherForecastController : ControllerBase
    {
       

        private readonly IConnectionMultiplexer _redis;
        private readonly LawService _lawService;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IConnectionMultiplexer redis, LawService lawService, IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _redis = redis;
            _lawService = lawService;
            _clientFactory = clientFactory;
        }


        [HttpPost("add")]
        public async Task<IActionResult> AddLaw([FromBody] Laws laws)
        {
            Console.Write(laws.Name + " \n");
            var result = await _lawService.AddLawAsync(laws);

            if (result == "added")
            {
                return Ok(result);
            }
            else
            {
                return BadRequest("error");
            }
        }


        [HttpPost("zonefind")]
        public async Task<ActionResult<List<string>>> Data([FromBody] Loc loc)
        {
            try
            {
                string aa = $"https://overpass-api.de/api/interpreter?data=[out:json];node(around:2000,{loc.Lat},{loc.Lon})[\"amenity\"];out;";

                using (var httpClient = _clientFactory.CreateClient())
                {
                    var response = await httpClient.GetStringAsync(aa);
                    var jsonObject = JsonConvert.DeserializeObject<JObject>(response);

                    // Get the "elements" array from the JSON object
                    var elementsArray = jsonObject["elements"] as JArray;

                    // Create a dynamic array to store amenity values
                    var amenities = new List<string>();

                    // Iterate through the elements array and extract the amenity values
                    foreach (var element in elementsArray)
                    {
                        var elementObject = element as JObject;
                        var tagsObject = elementObject["tags"] as JObject;
                        if (tagsObject != null && tagsObject["amenity"] != null)
                        {
                            var amenity = tagsObject["amenity"].ToString();
                            amenities.Add(amenity);
                        }
                    }
                    List<String> aalzone = await _lawService.GetAllKeysAsync();


                   
                    if (amenities.FirstOrDefault() == null)
                    {
                        var laws = await _lawService.GetSingleZoneLaws("common");
                        return Ok(laws);
                    }
                    else if (aalzone.Contains(amenities.FirstOrDefault()))
                    {
                        var laws = await _lawService.GetSingleZoneLaws(amenities.FirstOrDefault());
                        return Ok(laws);

                    }
                    else
                    {
                        var laws = await _lawService.GetSingleZoneLaws("common");
                        return Ok(laws);

                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return BadRequest();
            }
        }





        [HttpPost("addAll")]
        public async Task<IActionResult> AddLaws([FromBody] List<Laws> laws)
        {
            var result = await _lawService.AddListOfLaws(laws);

            if (result == "added")
            {
                return Ok(result);
            }
            else
            {
                return BadRequest("error");
            }
        }


        [HttpGet("all")]
        public async Task<IActionResult> GetAllLaws()
        {
            var laws = await _lawService.GetAllLawsAsync();
            return Ok(laws);
        }

        [HttpGet("zoneLaw/{zone}")]
        public async Task<IActionResult> GetZoneLaws(string zone)
        {
            var laws = await _lawService.GetSingleZoneLaws(zone);
            return Ok(laws);
        }

        [HttpGet("zone")]
        public async Task<IActionResult> GetAllKeysAsync()
        {
            var laws = await _lawService.GetAllKeysAsync();
            return Ok(laws);
        }

        [HttpDelete("allzone")]
        public async Task<IActionResult> DeleteAllKeysAsync()
        {
            await _lawService.DeleteAllKeysAsync();
            return Ok("deleted");
        }

        [HttpDelete("zone/{zone}")]
        public async Task<IActionResult> DeleteZoneLaws(string zone)
        {
            var result = await _lawService.DeleteZoneLawsAsync(zone);

            if (result)
            {
                return Ok("ZONE deleted successfully.");
            }
            else
            {
                return NotFound("Key not found.");
            }
        }


        [HttpPost("delete")]
        public async Task<IActionResult> DeleteLaw([FromBody] Laws lawToDelete)
        {
            var result = await _lawService.DeleteOneLaw(lawToDelete);

            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return NotFound($"Law with ID {lawToDelete.Id} in zone {lawToDelete.Zone} not found");
            }
        }


        [HttpPost("adduserdetail")]
        public async Task<IActionResult> AddUserDetail([FromBody] UsernameModel userDetails)
        {
            var result = await _lawService.AddUserDetail(userDetails);
            Responceen r = new Responceen();

            if (result == "added")
            {
                r.status = "done";
                return Ok(r);
            }
            else
            {
                r.status = "try again";
                return BadRequest(r);
            }
        }

        [HttpGet("getusers")]
        public async Task<IActionResult> GetAllUserDetails()
        {
            var userDetails = await _lawService.Getusers();
            return Ok(userDetails);
        }


    }
}
