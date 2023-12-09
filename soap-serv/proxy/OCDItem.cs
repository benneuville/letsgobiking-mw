using System;
using System.Collections.Generic;
using System.Linq;
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
        private static string OCDkey = "4bd5b96da6bf40eebc28429db33e2089";
        private static string OCDkeybackup = "3b468aae7c184bdea7b240ea301d2814";
        public static int OCDremaining = 2000;

        public OCDItem(string address)
        {

            string url = "https://api.opencagedata.com/geocode/v1/json?key=" + OCDkey + "&q=";
            this.contrat = address;
            json = requete(url + address).Result;
        }

        public static async Task<string> requete(string request)
        {
            try
            {
                HttpClient client = new HttpClient();
                string rep = await client.GetStringAsync(request);
                OCDremaining = JsonDocument.Parse(rep).RootElement.GetProperty("rate").GetProperty("remaining").GetInt32();
                checkBackup();
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
                var tmp = OCDkey;
                OCDkey = OCDkeybackup;
                OCDkeybackup = tmp;
            }
        }

    }
}