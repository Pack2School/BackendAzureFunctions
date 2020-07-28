using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using System.Text;
using Pack2SchoolFunction.Templates;
using Pack2SchoolFunction.Tables;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.EventHubs.Processor;
using System.Linq;
using Pack2SchoolFunctions;

namespace Pack2SchoolFunction
{
    public static class Utilities
    {

        public static async Task<T> ExtractContent<T>(HttpRequestMessage request)
        {
            string connectionRequestJson = await request.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(connectionRequestJson);
        }


        public async static Task<string> sendHttpRequest(string baseUri, HttpMethod httpMethod, string data)
        {
            HttpResponseMessage response;
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(httpMethod, baseUri))
            {
                using (var stringContent = new StringContent(data, Encoding.UTF8, "application/json"))
                {
                    if (httpMethod == HttpMethod.Post)
                    {
                        response = await client.PostAsync(baseUri, stringContent);
                    }
                    else
                    {
                        response = await client.PostAsync(baseUri, stringContent);
                    }
                    return await response.Content.ReadAsStringAsync();
                }
            }
        }

        public static T KeyByValue<T, W>(this Dictionary<T, W> dict, W val)
        {
            T key = default;
            foreach (KeyValuePair<T, W> pair in dict)
            {
                if (EqualityComparer<W>.Default.Equals(pair.Value, val))
                {
                    key = pair.Key;
                    break;
                }
            }
            return key;
        }
    }
}
