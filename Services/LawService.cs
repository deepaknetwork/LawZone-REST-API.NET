using StackExchange.Redis;
using System.Text.Json;
using last.Models;
using System.Net.NetworkInformation;

namespace last.Services
{
    public class LawService
    {
        private readonly IDatabase _redisDb;
        private readonly IHttpClientFactory _clientFactory;

        public LawService(IConnectionMultiplexer redis, IHttpClientFactory clientFactory)
        {
            _redisDb = redis.GetDatabase();
        }

        public async Task<List<string>> GetAllKeysAsync()
        {
            var keys = new List<string>();

            long cursor = 0;
            do
            {
                var scanResult = await _redisDb.ExecuteAsync("SCAN", cursor.ToString(), "MATCH", "*", "COUNT", "100");
                var resultArray = (RedisResult[])scanResult;

                cursor = (long)resultArray[0];
                var resultKeys = (RedisResult[])resultArray[1];

                foreach (var key in resultKeys)
                {
                    if (!key.ToString().Equals("userdetails"))
                    {
                        keys.Add((string)key);
                    }
                }

            } while (cursor != 0);

            return keys;
        }

        public async Task<List<Laws>> GetSingleZoneLaws(String zone)
        {
            var existingValues = await _redisDb.StringGetAsync(zone);
            List<Laws> lawsList = JsonSerializer.Deserialize<List<Laws>>(existingValues);
            return lawsList;
        }


        public async Task<List<List<Laws>>> GetAllLawsAsync()
        {
            var laws = new List<List<Laws>>();
            long cursor1 = 0;

            do
            {
                var keys = new List<string>();

                long cursor = 0;
                do
                {
                    var scanResult = await _redisDb.ExecuteAsync("SCAN", cursor.ToString(), "MATCH", "*", "COUNT", "100");
                    var resultArray = (RedisResult[])scanResult;

                    cursor = (long)resultArray[0];
                    var resultKeys = (RedisResult[])resultArray[1];

                    foreach (var key in resultKeys)
                    {
                        var existingValues = await _redisDb.StringGetAsync((string)key);
                        List<Laws> lawsList = JsonSerializer.Deserialize<List<Laws>>(existingValues);
                        laws.Add(lawsList);
                    }

                } while (cursor != 0);
                cursor1++;

            } while (cursor1 < 1);

            return laws;
        }


        public async Task<string> AddLawAsync(LawsView laws)
        {
            var existingValue = await _redisDb.StringGetAsync(laws.Zone);

            if (!existingValue.IsNullOrEmpty)
            {
                var lawsList = JsonSerializer.Deserialize<List<Laws>>(existingValue);

                lawsList.Add(new Laws { Id = laws.Id, Name = laws.Name, Description = laws.Description, Zone = laws.Zone, Views = 0 });
                await _redisDb.StringSetAsync(laws.Zone, JsonSerializer.Serialize(lawsList));
                return "added";
            }
            else
            {
                var newList = new List<Laws> { new Laws { Id = laws.Id, Name = laws.Name, Description = laws.Description, Zone = laws.Zone, Views = 0 } };
                await _redisDb.StringSetAsync(laws.Zone, JsonSerializer.Serialize(newList));
                return "added";
            }

        }


        public async Task<string> AddListOfLaws(List<LawsView> laws)
        {
            foreach (var law in laws)
            {
                var existingValue = await _redisDb.StringGetAsync(law.Zone);

                if (!existingValue.IsNullOrEmpty)
                {
                    var lawsList = JsonSerializer.Deserialize<List<Laws>>(existingValue);
                    lawsList.Add(new Laws { Id = law.Id, Name = law.Name, Description = law.Description, Zone = law.Zone, Views = 0 });
                    await _redisDb.StringSetAsync(law.Zone, JsonSerializer.Serialize(lawsList));
                }
                else
                {
                    var newList = new List<Laws> { new Laws { Id = law.Id, Name = law.Name, Description = law.Description, Zone = law.Zone, Views = 0 } };
                    await _redisDb.StringSetAsync(law.Zone, JsonSerializer.Serialize(newList));
                }
            }

            return "added";
        }

        public async Task<bool> DeleteZoneLawsAsync(string zone)
        {
            return await _redisDb.KeyDeleteAsync(zone);
        }


        public async Task DeleteAllKeysAsync()
        {
            long cursor = 0;
            do
            {
                var scanResult = await _redisDb.ExecuteAsync("SCAN", cursor.ToString(), "MATCH", "*", "COUNT", "100");
                var resultArray = (RedisResult[])scanResult;

                cursor = (long)resultArray[0];
                var resultKeys = (RedisResult[])resultArray[1];

                foreach (var key in resultKeys)
                {
                    await _redisDb.KeyDeleteAsync(key.ToString());
                }

            } while (cursor != 0);
        }


        public async Task<Laws> DeleteOneLaw(Laws lawToDelete)
        {
            var zone = lawToDelete.Zone;
            var lawId = lawToDelete.Id;

            Console.WriteLine(zone + " " + lawId);
            var lawsJson = await _redisDb.StringGetAsync(zone);

            if (!lawsJson.IsNullOrEmpty)
            {
                var lawsList = JsonSerializer.Deserialize<List<Laws>>(lawsJson);

                var lawToRemove = lawsList.Find(l => l.Id == lawId);

                if (lawToRemove != null)
                {
                    lawsList.Remove(lawToRemove);
                    await _redisDb.StringSetAsync(zone, JsonSerializer.Serialize(lawsList));
                    return lawToRemove;
                }
            }

            return null;
        }


        public async void LawView(Laws vi)
        {
            var zone = vi.Zone;
            var lawId = vi.Id;

            var lawsJson = await _redisDb.StringGetAsync(zone);

            if (!lawsJson.IsNullOrEmpty)
            {
                var lawsList = JsonSerializer.Deserialize<List<Laws>>(lawsJson);

                var lawToRemove = lawsList.Find(l => l.Id == lawId);

                if (lawToRemove != null)
                {
                    // Increment the views variable
                    lawToRemove.Views++;

                    // Update the law in the list
                    var index = lawsList.FindIndex(l => l.Id == lawId);
                    if (index != -1)
                    {
                        lawsList[index] = lawToRemove;
                    }

                    // Save the updated list back to Redis
                    await _redisDb.StringSetAsync(zone, JsonSerializer.Serialize(lawsList));
                }
            }

        }



        public async Task<string> AddUserDetail(UsernameModel username)
        {
            var existingValue = await _redisDb.StringGetAsync("userdetails");

            if (!existingValue.IsNullOrEmpty)
            {
                var lawsList = JsonSerializer.Deserialize<List<UsernameModel>>(existingValue);
                lawsList.Add(username);
                await _redisDb.StringSetAsync("userdetails", JsonSerializer.Serialize(lawsList));
                return "added";
            }
            else
            {
                var newList = new List<UsernameModel> { username };
                await _redisDb.StringSetAsync("userdetails", JsonSerializer.Serialize(newList));
                return "added";
            }
        }
        public async Task<List<UsernameModel>> Getusers()
        {
            var existingValue = await _redisDb.StringGetAsync("userdetails");
            return JsonSerializer.Deserialize<List<UsernameModel>>(existingValue);
        }

        public async Task<Analatics> GetAnalaticcs()
        {

            List<string> zones = await GetAllKeysAsync();
            long total_laws = 0;
            List<UsernameModel> users = await Getusers();
            List<EachZone> mostUsedLaw = new List<EachZone> { };
            string mostUsedZone;




            List<List<Laws>> all = await GetAllLawsAsync();
            foreach (List<Laws> zone in all)
            {
                total_laws = total_laws + zone.Count();
            }


            foreach (List<Laws> zone in all)
            {
                Laws la = new Laws();
                foreach (Laws laws in zone)
                {
                    try
                    {
                        if (laws.Views > la.Views)
                        {
                            la = laws;
                        }
                    }
                    catch (Exception ex)
                    {
                        la = laws;
                    }
                }
                if (la.Views > 0) { mostUsedLaw.Add(new EachZone { Zone = la.Zone, Law = la }); }
            }


            long min = 0;
            string mi = "";
            foreach (List<Laws> zone in all)
            {
                long minn = 0;
                string mii = "";
                foreach (Laws laws in zone)
                {
                    minn=minn+ laws.Views;
                    mii = laws.Zone; 
                }
                if (minn > min)
                {
                    min= minn;
                    mi = mii;
                }
               
            }
            mostUsedZone = mi;


            return new Analatics { Zones = zones, Total_laws = total_laws, Users = users, MostUsedLaw = mostUsedLaw, MostUsedZone = mostUsedZone };


        }
    }
}
