using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace proxycache
{

    [ServiceContract()]
    internal interface IProxyCache
    {

        [OperationContract()]
        string Get(string key);

        [OperationContract()]
        string GetOCD(string key);
    }
}