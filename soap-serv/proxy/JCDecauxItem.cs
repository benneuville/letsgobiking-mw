using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace proxycache
{
    class JCDecauxItem
    {

        string json;
        string contrat;

        public JCDecauxItem(string contrat)
        {
            string baseUrl = "https://api.jcdecaux.com/vls/v3/";
            string JCDkey = "c7cf784389d43afc852484ad19368ad387ea1e4f";
            this.contrat = contrat;
            if (contrat.Equals("all-contract"))
            {
                json = requete(baseUrl+ "contracts?apiKey=" + JCDkey).Result;
            }
            else if (contrat.Equals("all-station"))
            {
                json = requete(baseUrl + "stations?apiKey=" + JCDkey).Result;
            }
            else
            {
                json = requete($"{baseUrl}stations?contract={contrat}&apiKey={JCDkey}").Result;
            }

        }

        public static async Task<string> requete(string request)
        {
            try
            {
                HttpClient client = new HttpClient();
                string rep = await client.GetStringAsync(request);
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

    }
}