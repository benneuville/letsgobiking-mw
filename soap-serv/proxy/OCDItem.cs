using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace proxycache
{
    class OCDItem
    {

        string json;
        string contrat;
        private static string OCDkey = "3b468aae7c184bdea7b240ea301d2814";
        private static string OCDkeybackup = "4bd5b96da6bf40eebc28429db33e2089";
        public static int OCDremaining = 0;

        /** Constructeur
         ** 
         ** @param address : adresse à géocoder
         ** 
         **/
        public OCDItem(string address)
        {

            string url = "https://api.opencagedata.com/geocode/v1/json?key=";
            this.contrat = address;
            json = requete(url, address).Result;
        }

        /** Requete HTTP
         ** 
         ** @param request : requete HTTP
         ** @return string : reponse HTTP
         ** 
         **/

        public static async Task<string> requete(string request, string address)
        {
            try
            {
                checkBackup();
                HttpClient client = new HttpClient();
                string rep = await client.GetStringAsync(request + OCDkey + "&q=" + address);
                OCDremaining = JsonDocument.Parse(rep).RootElement.GetProperty("rate").GetProperty("remaining").GetInt32();
                return rep;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public string getJson()
        {
            return json;
        }

        private static void checkBackup()
        {
            if (OCDremaining < 100)
            {
                string url = "https://api.opencagedata.com/geocode/v1/json?key=" + OCDkey;
                HttpClient client = new HttpClient();
                var rep = client.GetAsync(url).Result;
                if(rep.StatusCode == HttpStatusCode.PaymentRequired)
                {
                    var tmp = OCDkey;
                    OCDkey = OCDkeybackup;
                    OCDkeybackup = tmp;
                }
                else
                {
                    OCDremaining = JsonDocument.Parse(rep.Content.ReadAsStringAsync().Result).RootElement.GetProperty("rate").GetProperty("remaining").GetInt32();
                }
            }
        }

    }
}