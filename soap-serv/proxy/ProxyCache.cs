using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace proxycache
{

    /** Interface du proxy cache
     ** 
     ** cache pour les requetes JCDecaux
     ** cache pour les requetes OpenCageData
     ** 
     **/
    public class ProxyCache : IProxyCache

    {
        private static readonly CacheGenerique<JCDecauxItem> cacheGenerique = new CacheGenerique<JCDecauxItem>();
        private static readonly CacheGenerique<OCDItem> cacheGeneriqueOCD = new CacheGenerique<OCDItem>();


        public string Get(string key)
        {
            return cacheGenerique.Get(key, 4000).getJson();
        }

        public string GetOCD(string key)
        {
            return cacheGeneriqueOCD.Get(key, 4000).getJson();
        }

    }
}