using Common;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace ServerOuterShell.ClientProcessors;

/*
 * V2.2 - bug fixed. the client was writing in 64kb chunks but the server was reading in full block.
 * fixed by reading in chunks
 */

public class ClientProcessorV2_2 : IClientProcessor
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

        byte[] networkBuffer = new byte[metadataObj.FileSize + 45000], networkBufferTemp = new byte[64 * 1024];
        int totalBytesReadIndex = 0;
        int bytesRead = 0;
        do
        {
            bytesRead = await channel.ReadAsync(networkBufferTemp, 0, 64 * 1024);
            Array.Copy(networkBufferTemp, 0, networkBuffer, totalBytesReadIndex, bytesRead);
            totalBytesReadIndex += bytesRead;
        } while (bytesRead == 64 * 1024);

        string filePath = $@"{Constants.FullPathOfFolderToWhichServerWritesFile}\{metadataObj.FileName}";
        using FileStream fileStream = new(filePath, FileMode.Create);
        await fileStream.WriteAsync(networkBuffer, 0, (int)metadataObj.FileSize);
        await fileStream.FlushAsync();
        await channel.WriteAsync(BitConverter.GetBytes(1));
    }
}
