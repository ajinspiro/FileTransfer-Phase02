using Common;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

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
        PayloadMetadata metadataObj = JsonSerializer.Deserialize<PayloadMetadata>(metadata) ?? throw new Exception();

        byte[] networkBuffer = new byte[metadataObj.FileSize];
        await channel.ReadAsync(networkBuffer, 0, (int)metadataObj.FileSize);

        string filePath = $@"{Constants.FullPathOfFolderToWhichServerWritesFile}\{metadataObj.FileName}";
        using FileStream fileStream = new(filePath, FileMode.Create);
        await fileStream.WriteAsync(networkBuffer, 0, (int)metadataObj.FileSize);
        await fileStream.FlushAsync();
    }
}
