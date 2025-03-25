using System.Net;
using System.Net.Sockets;

namespace Common
{
    public class ClientV1 : IClient
    {
        public async Task Run(ClientOptions options)
        {
            IPEndPoint ipEndPoint = new(options.IPAddress, options.Port);
            using TcpClient tcpClient = new(ipEndPoint);
        }
    }
}
