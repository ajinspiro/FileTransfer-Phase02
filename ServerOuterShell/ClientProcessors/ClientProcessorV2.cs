using Common;
using System.Net.Sockets;

namespace ServerOuterShell.ClientProcessors;

/*
 * V2 - reading bytes manually without using .NET's reader class
 */

public class ClientProcessorV2 : IClientProcessor
{
    public async Task Receive(TcpClient clientConnection)
    {
        using NetworkStream channel = clientConnection.GetStream();
        byte[] metadataLengthInBytes = new byte[sizeof(long)];
        await channel.ReadAsync(metadataLengthInBytes, 0, metadataLengthInBytes.Length);
        long metadataLength = BitConverter.ToInt64(metadataLengthInBytes);
        Console.WriteLine(metadataLength);

    }
}
