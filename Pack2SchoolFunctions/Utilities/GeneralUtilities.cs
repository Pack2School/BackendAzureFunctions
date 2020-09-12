using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace Pack2SchoolFunction
{
    public static class Utilities
    {
        public static async Task<T> ExtractContent<T>(HttpRequestMessage request)
        {
            string connectionRequestJson = await request.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(connectionRequestJson);
        }
    }
}
