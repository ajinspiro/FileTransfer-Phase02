using Common;
using System.Net.Sockets;

namespace ServerOuterShell.ClientProcessors
{
    public class ClientProcessorV3 : IClientProcessor
    {
        public Task Receive(TcpClient clientConnection)
        {
            throw new NotImplementedException("V3 client calls wikipedia. It doesnt have a listener.");
        }
    }
}
