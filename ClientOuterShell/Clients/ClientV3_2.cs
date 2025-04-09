using Common;
using System.Net.Sockets;
using System.Text;

namespace ClientOuterShell.Clients;

/* In this version I will log the full response sent by the server to the console. To do this,
 * first I need to parse the headers and find out the value of content-length header. This header
 * tells us how long the response body is. Detecting the end of the header section is a little 
 * bit challenging because headers and body are sent as a single stream of text. But the headers
 * will be separated from the body by a CRLF (\r\n). This means occurance of double CRLF (\r\n\r\n)
 * is the end of headers. Once the content-length value is known, we can read exactly that much to
 * process the body in full.
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
        byte[] buffer = new byte[contentLength];
        await clientSocket.ReceiveAsync(buffer);
        StringBuilder responseBodyBuilder = new();
        responseBodyBuilder.Append(Encoding.ASCII.GetString(buffer));
        Console.WriteLine(responseBodyBuilder.ToString());
        await clientSocket.DisconnectAsync(false);
    }

    async Task<HeaderList> ParseHeaders(Socket clientSocket)
    {
        string headerString = await GetEntireHeaderListAsString(clientSocket);
        HeaderList headerList = new(headerString);
        return headerList;
    }

    private static async Task<string> GetMetaLine(Socket clientSocket)
    {
        StringBuilder stringBuilder = new();
        byte[] buffer = new byte[1];
        int bytesRead;
        do
        {
            bytesRead = await clientSocket.ReceiveAsync(buffer);
            stringBuilder.Append((char)buffer[0]);
        }
        while (bytesRead > 0 && !stringBuilder.ToString().EndsWith("\r\n"));
        string str = stringBuilder.ToString();
        return str.Substring(0, str.Length - 2);
    }

    static async Task<string> GetEntireHeaderListAsString(Socket clientSocket)
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