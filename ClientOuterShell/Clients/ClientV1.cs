using Common;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace ClientOuterShell.Clients;

/*
 * This is phase 1 code reimplemented for reference purposes
 */
public class ClientV1 : IClient
{
    public Task Send(ClientOptions options)
    {
        using TcpClient tcpClient = new(options.IPAddress.ToString(), options.Port);
        using NetworkStream channel = tcpClient.GetStream();

        using FileStream fileStream = new(options.FilePath, FileMode.Open, FileAccess.Read);
        string fileName = Path.GetFileName(options.FilePath);
        PayloadMetadata payloadMetadata = new() { FileName = fileName, FileSize = fileStream.Length };
        string metadata = JsonSerializer.Serialize(payloadMetadata);

        using BinaryWriter channelWriter = new(channel);
        channelWriter.Write(metadata);
        using BinaryReader fileReader = new(fileStream);

        for (int i = 0; i < fileStream.Length; i++)
        {
            byte bite = fileReader.ReadByte();
            channelWriter.Write(bite);
        }
        return Task.CompletedTask;
    }
}
