using Common;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClientOuterShell.Clients;

/*
 * With this version, I'm kicking off an attempt to learn how the network stack works when
 * I send a HTTP(S) request from a process. The end goal is to connect to wikipedia using 
 * raw socket through TLS, fetch the HTML of the page of legendary keralite mathematician 
 * Nīlakaṇṭha Somayāji (at https://en.wikipedia.org/wiki/Nilakantha_Somayaji) and dump it 
 * in console. This is an ambitious dream for an application developer like me, hence I 
 * need to create smaller intermediate goals. No clients in version 3 will be having 
 * ClientProcessors since I will be using servers in internet.
 * */

/* This version (3.1), will be the first step. I will be connecting to http://httpforever.com 
 * through a socket using STREAM and I chose this site as a first step because it doesn't 
 * implement TLS security. 
 * */

/* Limitation/Bug: The buffer is hard coded to be 3KB. The uncompressed response is larger than 
 * 3KB. This means the response will be cut off in the middle.
 */
public class ClientV3_1 : IClient
{
    public async Task Send(ClientOptions _)
    {
        Socket clientSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        // no need to bind the socket because its a client
        await clientSocket.ConnectAsync("httpforever.com", 80);
        string httpRequest =
@"GET / HTTP/1.1
Host: httpforever.com
User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:137.0) Gecko/20100101 Firefox/137.0
Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8
Accept-Language: en-US,en;q=0.5

";
        byte[] httpRequestBytes = Encoding.ASCII.GetBytes(httpRequest);
        int bytesSent = await clientSocket.SendAsync(httpRequestBytes);
        if (bytesSent != httpRequestBytes.Length)
        {
            throw new Exception("Some data were not sent.");
        }
        byte[] response = new byte[3 * 1024];
        int bytesRead = await clientSocket.ReceiveAsync(response);
        string str = Encoding.ASCII.GetString(response);
        Console.WriteLine(str);
        await clientSocket.DisconnectAsync(false);
    }
}

/* In this version I will retrieve the full response sent by the server. I will be doing it 
 * by reading the response in chunks of 64KB using a loop. If less than 64KB data was read,
 * it means it was the last chunk.
 */
public class ClientV3_2 : IClient
{
    public async Task Send(ClientOptions options)
    {
        Socket clientSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        // no need to bind the socket because its a client
        await clientSocket.ConnectAsync("httpforever.com", 80);
        string httpRequest =
@"GET / HTTP/1.1
Host: httpforever.com
User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:137.0) Gecko/20100101 Firefox/137.0
Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8
Accept-Language: en-US,en;q=0.5

";
        byte[] httpRequestBytes = Encoding.ASCII.GetBytes(httpRequest);
        int bytesSent = await clientSocket.SendAsync(httpRequestBytes);
        if (bytesSent != httpRequestBytes.Length)
        {
            throw new Exception("Some data were not sent.");
        }
        string metaLine = await GetMetaLine(clientSocket); // Get the first line that contains response status code
        Console.WriteLine(metaLine);
        Console.WriteLine();
        HeaderList headerList = await ParseHeaders(clientSocket);
        var contentLength = int.Parse(headerList["Content-Length"] ?? throw new Exception());
        Console.WriteLine(contentLength);

        await clientSocket.DisconnectAsync(false);
    }

    async Task<HeaderList> ParseHeaders(Socket clientSocket)
    {
        string headerString = await GetEntireHeaderListAsString(clientSocket);
        HeaderList headerList = new(headerString);
        return headerList;
    }

    private async Task<string> GetMetaLine(Socket clientSocket)
    {
        StringBuilder stringBuilder = new();
        byte[] buffer = new byte[1];
        int bytesRead = 0;
        do
        {
            bytesRead = await clientSocket.ReceiveAsync(buffer);
            stringBuilder.Append((char)buffer[0]);
        }
        while (bytesRead > 0 && !stringBuilder.ToString().EndsWith("\r\n"));
        string str = stringBuilder.ToString();
        return str.Substring(0, str.Length - 2);
    }

    async Task<string> GetEntireHeaderListAsString(Socket clientSocket)
    {
        // HTTP headers and body are separated by \r\n\r\n (double CRLF).
        // We will check for its occurance to separate HTTP headers from body.
        byte[] buffer = new byte[1];
        StringBuilder responseBuilder = new();
        bool[] isDoubleCRLF = [false, false, false, false];
        while (true)
        {
            int bytesRead = await clientSocket.ReceiveAsync(buffer);
            char character = (char)buffer[0];
            if (character == '\n')
            {
                if (isDoubleCRLF[0] && isDoubleCRLF[1] && isDoubleCRLF[2])
                {
                    isDoubleCRLF[3] = true;
                }
                else if (isDoubleCRLF[0])
                {
                    isDoubleCRLF[1] = true;
                }
                else // reset all flags
                {
                    isDoubleCRLF = [false, false, false, false];
                }
            }
            else if (character == '\r')
            {
                isDoubleCRLF[0] = true;
                if (isDoubleCRLF[0] && isDoubleCRLF[1])
                {
                    isDoubleCRLF[2] = true;
                }
            }
            else // reset all flags
            {
                isDoubleCRLF = [false, false, false, false];
            }
            responseBuilder.Append(character);

            if (isDoubleCRLF.All(x => x))
            {
                break;
            }
        }
        return responseBuilder.ToString();
    }

    class HeaderList : Dictionary<string, string?>
    {
        public HeaderList(string headersAsString)
        {
            headersAsString = headersAsString.Substring(0, headersAsString.Length - 4); // length-4 because we dont need \r\n\r\n
            var individualHeaders = headersAsString.Split("\r\n");
            individualHeaders.Select(x => x.Split(":")).ToList().ForEach(x =>
            {
                this[x[0]] = x[1].Trim(); // setting the dictionary
            });
        }

        public override string ToString()
        {
            string str = string.Empty;
            foreach (var item in this)
            {
                str += $"{item.Key} => {item.Value}\r\n";
            }
            return str;
        }
    }
}