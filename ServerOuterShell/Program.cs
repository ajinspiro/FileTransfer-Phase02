using Common;
using ServerOuterShell.ClientProcessors;
using System.Net;
using System.Net.Sockets;

using TcpListener listener = new(IPAddress.Parse("127.0.0.1"), 13000);
listener.Start();
IClientProcessor clientProcessor = new ClientProcessorV2_2();
while (true)
{
    TcpClient clientConnection = await listener.AcceptTcpClientAsync();
    await Task.Run(() => clientProcessor.Receive(clientConnection));
}
