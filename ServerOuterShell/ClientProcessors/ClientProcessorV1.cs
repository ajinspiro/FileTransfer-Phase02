using Common;
using System.Net.Sockets;
using System.Text.Json;

namespace ServerOuterShell.ClientProcessors;

/*
 * This is phase 1 code reimplemented for reference purposes
 */
public class ClientProcessorV1 : IClientProcessor
{
    public Task Receive(TcpClient clientConnection)
    {
        using NetworkStream channel = clientConnection.GetStream();
        using BinaryReader channelReader = new(channel);

        string metadata = channelReader.ReadString();
        PayloadMetadata? metadataPayload = JsonSerializer.Deserialize<PayloadMetadata>(metadata);
        if (metadataPayload is null)
        {
            throw new Exception();
        }

        string pathToOutputFile = $"{Constants.FullPathOfFolderToWhichServerWritesFile}/{metadataPayload.FileName}";
        using FileStream fileStream = new(pathToOutputFile, FileMode.Create);
        using BinaryWriter fileWriter = new(fileStream);
        for (int i = 0; i < metadataPayload.FileSize; i++)
        {
            byte bite = channelReader.ReadByte();
            fileWriter.Write(bite);
        }
        fileWriter.Flush();
        clientConnection.Dispose();
        return Task.CompletedTask;
    }
}