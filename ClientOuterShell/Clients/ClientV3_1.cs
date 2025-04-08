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
 * 3KB. This means the response logged to the console will be cut off in the middle. Will fix
 * this in 3.2
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
