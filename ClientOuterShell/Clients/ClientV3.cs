using Common;
using System.Net.Sockets;
using System.Text;

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
        byte[] response = new byte[1];
        StringBuilder responseBuilder = new();
        bool[] isDoubleCRLF = [false, false, false, false];
        while (true)
        {
            int bytesRead = await clientSocket.ReceiveAsync(response);
            char character = (char)response[0];
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
                Console.WriteLine(responseBuilder.ToString());
                break;
            }
        }
        await clientSocket.DisconnectAsync(false);
    }
}

/*
 * In this version I will be parsing the HTTP response headers and figure out the exact content-length
 * to read the correct amount of bytes. This enables me to get the complete response.
 */