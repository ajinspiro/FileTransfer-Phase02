
using System.Net.Sockets;

namespace Common;

public interface IClientProcessor
{
    Task Receive(TcpClient clientConnection);
}