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
        public static int OCDremaining = 99;

        /** Constructeur
         ** 
         ** @param address : adresse à géocoder
         ** 
         **/
        public OCDItem(string address)
        {
            if (address == "init")
            {
                init();
            }
            else
            {
                string url = "https://api.opencagedata.com/geocode/v1/json?key=";
                this.contrat = address;
                json = requete(url, address).Result;
            }
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

        public static void init()
        {
            string url = "https://api.opencagedata.com/geocode/v1/json?key=" + OCDkey;
            HttpClient client = new HttpClient();
            var rep = client.GetAsync(url).Result;
            if (rep.StatusCode == HttpStatusCode.PaymentRequired)
            {

                url = "https://api.opencagedata.com/geocode/v1/json?key=" + OCDkeybackup;
                client = new HttpClient();
                rep = client.GetAsync(url).Result;
                if (rep.StatusCode == HttpStatusCode.PaymentRequired)
                {
                    throw new Exception("OpenCageData API key limit reached");
                }
                else
                {
                    var tmp = OCDkey;
                    OCDkey = OCDkeybackup;
                    OCDkeybackup = tmp;
                }
            }
            OCDremaining = JsonDocument.Parse(rep.Content.ReadAsStringAsync().Result).RootElement.GetProperty("rate").GetProperty("remaining").GetInt32();

        }

        public static void checkBackup()
        {
            if (OCDremaining < 50)
            {
                string url = "https://api.opencagedata.com/geocode/v1/json?key=" + OCDkeybackup;
                HttpClient client = new HttpClient();
                var rep = client.GetAsync(url).Result;
                if (rep.StatusCode != HttpStatusCode.PaymentRequired)
                {
                    var tmp = OCDkey;
                    OCDkey = OCDkeybackup;
                    OCDkeybackup = tmp;
                }
                else
                {
                    throw new Exception("OpenCageData API key limit reached");
                }
            }
        }

    }
}