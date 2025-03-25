using System.Net;

namespace Common
{
    public class ClientOptions(IPAddress ipAddress, Port port)
    {
        private readonly IPAddress _ipAddress = ipAddress;
        private readonly Port _port = port;

        public IPAddress IPAddress => _ipAddress;
        public Port Port => _port;
    }
}
