using Common;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace ClientOuterShell.Clients;

/*
 * V2 - writing bytes manually without using .NET's writer class
 */

public class ClientV2 : IClient
{
    public async Task Send(ClientOptions options)
    {
        using TcpClient tcpClient = new(options.IPAddress.ToString(), options.Port);
        using NetworkStream channel = tcpClient.GetStream();

        using FileStream fileStream = new(options.FilePath, FileMode.Open, FileAccess.Read);
        string fileName = Path.GetFileName(options.FilePath);
        PayloadMetadata payloadMetadata = new() { FileName = fileName, FileSize = fileStream.Length };
        string metadata = JsonSerializer.Serialize(payloadMetadata);

        // Step-1 Write metadata

        byte[] metadataBytes = Encoding.Unicode.GetBytes(metadata);
        // Step-1.1 Write metadata length
        byte[] metadataLength = BitConverter.GetBytes(metadataBytes.Length);
        channel.Write(metadataLength, 0, metadataLength.Length);

        // Step-1.2 Write metadata
        var metadataBytesInChunks = metadataBytes.Chunk(64 * 1024);
        foreach (var chunk in metadataBytesInChunks)
        {
            await channel.WriteAsync(chunk, 0, chunk.Length, CancellationToken.None);
        }
        await channel.FlushAsync();
    }
}
