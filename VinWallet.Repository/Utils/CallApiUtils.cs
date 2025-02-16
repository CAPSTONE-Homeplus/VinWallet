using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using VinWallet.Repository.Constants;

namespace VinWallet.Repository.Utils
{
    public class CallApiUtils
    {
        public static async Task<HttpResponseMessage> CallApiEndpoint(string url, object data)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var httpClient = new HttpClient();
            var response = await httpClient.PostAsync(url, content);
            return response;
        }
        public static async Task<HttpResponseMessage> CallApiEndpoint(string url, object data, string token)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await httpClient.PostAsync(url, content);
            return response;
        }


        public static async Task<HttpResponseMessage> CallApiGetEndpoint(string url, string roomCode)
        {
            using var httpClient = new HttpClient();
            string requestUrl = HomeCleanApiEndPointConstant.Room.RoomByCodeEndpoint.Replace("{room-code}", roomCode);
            var response = await httpClient.GetAsync(requestUrl);
            return response;
        }

        public static async Task<HttpResponseMessage> CallApiEndpoint(string url, string token)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent("");

            // Thiết lập header Content-Type
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await httpClient.SendAsync(request);
            return response;
        }



        public static async Task<Object> GenerateObjectFromResponse<Object>(HttpResponseMessage response)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<Object>(responseString);
            return responseObject;
        }
    }
}
