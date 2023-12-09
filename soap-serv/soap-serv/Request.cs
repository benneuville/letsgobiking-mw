using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace soap_serv
{
    public class Request
    {
        public static string Get(string url)
        {
            string response = "";
            using (var client = new System.Net.Http.HttpClient())
            {
                client.BaseAddress = new Uri(url);
                client.DefaultRequestHeaders.Accept.Clear();
                System.Net.Http.HttpResponseMessage responseMessage = client.GetAsync(url).Result;
                if (responseMessage.IsSuccessStatusCode)
                {
                    response = responseMessage.Content.ReadAsStringAsync().Result;
                }
                else throw new Exception("Error while getting response from " + url);
            }
            return response;
        }
    }

}
