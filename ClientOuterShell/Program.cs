// Client
using ClientOuterShell.Clients;
using Common;
using System.Net;


// Wait for the server to start (3 seconds)
await Task.Delay(3000);
IClient client = new ClientV3_2(); // to try a different client implementation, change here
await client.Send(new(IPAddress.Parse("127.0.0.1"), 13000, Constants.FullPathOfFileSentByClient));