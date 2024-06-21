using StackExchange.Redis;
using System.Text.Json;
using last.Models;

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


        public async Task<string> AddLawAsync(Laws laws)
        {
            var existingValue = await _redisDb.StringGetAsync(laws.Zone);

            if (!existingValue.IsNullOrEmpty)
            {
                var lawsList = JsonSerializer.Deserialize<List<Laws>>(existingValue);
                lawsList.Add(laws);
                await _redisDb.StringSetAsync(laws.Zone, JsonSerializer.Serialize(lawsList));
                return "added";
            }
            else
            {
                var newList = new List<Laws> { laws };
                await _redisDb.StringSetAsync(laws.Zone, JsonSerializer.Serialize(newList));
                return "added";
            }

        }


        public async Task<string> AddListOfLaws(List<Laws> laws)
        {
            foreach (var law in laws)
            {
                var existingValue = await _redisDb.StringGetAsync(law.Zone);

                if (!existingValue.IsNullOrEmpty)
                {
                    var lawsList = JsonSerializer.Deserialize<List<Laws>>(existingValue);
                    lawsList.Add(law);
                    await _redisDb.StringSetAsync(law.Zone, JsonSerializer.Serialize(lawsList));
                }
                else
                {
                    var newList = new List<Laws> { law };
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
    }
}
