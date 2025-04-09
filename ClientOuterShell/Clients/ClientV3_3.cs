using Common;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace ClientOuterShell.Clients;

/* In this version I will connect to wikipedia through TLS and do the same thing. I will be using 
 * course-grained APIs for this version. We will not send Accept-Encoding header because we will 
 * deal with compression in later versions.
 */
public class ClientV3_3 : IClient
{
    public async Task Send(ClientOptions options)
    {
        Socket clientSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        // no need to bind the socket because its a client
        await clientSocket.ConnectAsync("en.wikipedia.org", 443);
        NetworkStream channel = new(clientSocket, false);
        SslStream sslStream = new(channel, true, (sender, cert, chain, error) => true);
        await sslStream.AuthenticateAsClientAsync("en.wikipedia.org");
        string httpRequest =
@"GET /wiki/Nilakantha_Somayaji HTTP/1.1
Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7
Accept-Language: en-US,en;q=0.9
Connection: keep-alive
Cookie: WMF-Last-Access=09-Apr-2025; WMF-Last-Access-Global=09-Apr-2025; GeoIP=IN:TN:Chennai:13.09:80.27:v4; NetworkProbeLimit=0.001; enwikimwuser-sessionId=4dbe0b89db78e4acb677
Host: en.wikipedia.org
Sec-Fetch-Dest: document
Sec-Fetch-Mode: navigate
Sec-Fetch-Site: none
Sec-Fetch-User: ?1
Upgrade-Insecure-Requests: 1
User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/135.0.0.0 Safari/537.36
sec-ch-ua: ""Google Chrome"";v=""135"", ""Not-A.Brand"";v=""8"", ""Chromium"";v=""135""
sec-ch-ua-mobile: ?0
sec-ch-ua-platform: ""Windows""

";
        byte[] httpRequestBytes = Encoding.ASCII.GetBytes(httpRequest);
        await sslStream.WriteAsync(httpRequestBytes);
        string metaLine = await GetMetaLine(sslStream); // Get the first line that contains response status code
        Console.WriteLine(metaLine);
        HeaderList headerList = await ParseHeaders(sslStream);
        var contentLength = int.Parse(headerList["content-length"] ?? throw new Exception());
        Console.WriteLine($"Content length: {contentLength}");
        Console.WriteLine();
        byte[] buffer = new byte[64 * 1024];
        int totalBytesRead = 0, bytesRead;
        StringBuilder responseBodyBuilder = new();
        do
        {
            bytesRead = await sslStream.ReadAsync(buffer);
            totalBytesRead += bytesRead;

            // This line ensures data in buffer from previous loop iteration doesnt pollute current iteration
            ArraySegment<byte> data = totalBytesRead == 64 * 1024 ? new(buffer) : new(buffer, 0, bytesRead);
            responseBodyBuilder.Append(Encoding.ASCII.GetString(data));
        }
        while (totalBytesRead < contentLength);
        Console.WriteLine(responseBodyBuilder.ToString());
        await clientSocket.DisconnectAsync(false);
    }

    async Task<HeaderList> ParseHeaders(SslStream sslStream)
    {
        string headerString = await GetEntireHeaderListAsString(sslStream);
        HeaderList headerList = new(headerString);
        return headerList;
    }

    private static async Task<string> GetMetaLine(SslStream sslStream)
    {
        StringBuilder stringBuilder = new();
        byte[] buffer = new byte[1];
        int bytesRead;
        do
        {
            bytesRead = await sslStream.ReadAsync(buffer);
            stringBuilder.Append((char)buffer[0]);
        }
        while (bytesRead > 0 && !stringBuilder.ToString().EndsWith("\r\n"));
        string str = stringBuilder.ToString();
        return str.Substring(0, str.Length - 2);
    }

    static async Task<string> GetEntireHeaderListAsString(SslStream sslStream)
    {
        // HTTP headers and body are separated by \r\n\r\n (double CRLF).
        // We will check for its occurance to separate HTTP headers from body.
        byte[] buffer = new byte[1];
        StringBuilder responseBuilder = new();
        bool[] isDoubleCRLF = [false, false, false, false];
        while (true)
        {
            int bytesRead = await sslStream.ReadAsync(buffer);
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
                this[x[0].ToLowerInvariant()] = x[1].Trim(); // setting the dictionary
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