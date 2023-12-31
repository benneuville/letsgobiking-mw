﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace proxycache
{
    /** Cache Générique T
     ** 
     **/
    public class CacheGenerique<T>
    {
        ObjectCache cache = MemoryCache.Default;
        DateTimeOffset dt_default = ObjectCache.InfiniteAbsoluteExpiration;

        /** recupere un objet T dans le cache
         ** 
         ** @param CacheItemName : nom de l'objet dans le cache
         ** @return T : objet T
         ** 
         **/
        public T Get(string CacheItemName)
        {
            if (!cache.Contains(CacheItemName))
            {
                CacheItemPolicy policy = new CacheItemPolicy();
                policy.AbsoluteExpiration = dt_default;
                T obj = (T)Activator.CreateInstance(typeof(T), CacheItemName);
                cache.Set(CacheItemName, obj, policy);
            }
            return (T)cache.Get(CacheItemName);
        }

        /** recupere un objet T dans le cache
         ** 
         ** @param CacheItemName : nom de l'objet dans le cache
         ** @param dt_seconds : temps en secondes avant expiration
         ** @return T : objet T
         ** 
         **/
        public T Get(string CacheItemName, double dt_seconds)
        {
            if (!cache.Contains(CacheItemName))
            {
                CacheItemPolicy policy = new CacheItemPolicy();
                policy.AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(dt_seconds);
                T obj = (T)Activator.CreateInstance(typeof(T), CacheItemName);
                cache.Set(CacheItemName, obj, policy);
            }
            return (T)cache.Get(CacheItemName);
        }

        /** recupere un objet T dans le cache
         ** 
         ** @param CacheItemName : nom de l'objet dans le cache
         ** @param dt : date d'expiration
         ** @return T : objet T
         ** 
         **/
        public T Get(string CacheItemName, DateTimeOffset dt)
        {
            if (!cache.Contains(CacheItemName))
            {
                CacheItemPolicy policy = new CacheItemPolicy();
                policy.AbsoluteExpiration = dt;
                T obj = (T)Activator.CreateInstance(typeof(T), CacheItemName);
                cache.Set(CacheItemName, obj, policy);
            }
            return (T)cache.Get(CacheItemName);
        }
    }
}