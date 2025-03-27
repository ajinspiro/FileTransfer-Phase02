using Common;
using System.Net.Sockets;
using System.Text;

namespace ServerOuterShell.ClientProcessors;

/*
 * V2 - reading bytes manually without using .NET's reader class
 */

public class ClientProcessorV2 : IClientProcessor
{
    public async Task Receive(TcpClient clientConnection)
    {
        using NetworkStream channel = clientConnection.GetStream();
        int sizeOfInt = sizeof(int);
        byte[] metadataLengthInBytes = new byte[sizeOfInt];
        await channel.ReadAsync(metadataLengthInBytes, 0, metadataLengthInBytes.Length);
        int metadataLength = BitConverter.ToInt32(metadataLengthInBytes);
        byte[] metadataBytes = new byte[metadataLength];
        await channel.ReadAsync(metadataBytes, 0, metadataLength);
        string metadata = Encoding.Unicode.GetString(metadataBytes);
        Console.WriteLine(metadata);
    }
}
