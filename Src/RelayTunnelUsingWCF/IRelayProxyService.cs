using System.IO;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace RelayTunnelUsingWCF
{
    [ServiceContract]
    public interface IRelayProxyService
    {
        [OperationContract]
        [WebInvoke(Method = "*", UriTemplate = "*")]
        Stream ProxyRequest(Stream requestBody);
    }
}
