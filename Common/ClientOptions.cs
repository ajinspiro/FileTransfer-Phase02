using System.Net;

namespace Common;

public class ClientOptions(IPAddress ipAddress, Port port, string filePath)
{
    private readonly IPAddress _ipAddress = ipAddress;
    private readonly string _filePath = filePath;
    private readonly Port _port = port;

    public IPAddress IPAddress => _ipAddress;
    public string FilePath => _filePath;
    public Port Port => _port;
}
