using Common;

namespace ClientOuterShell.Clients
{
    // This client will use raw socket to connect to an HTTP server, send GET request, retrieve response and dumps it in console.
    // This client will not have a server implementation. We will be using the wikipedia page of a keralite mathematician Nīlakaṇṭha Somayāji (https://en.wikipedia.org/wiki/Nilakantha_Somayaji) to get the response.
    public class ClientV3 : IClient
    {
        public Task Send(ClientOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
