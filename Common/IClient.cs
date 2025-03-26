
namespace Common;

public interface IClient
{
    Task Send(ClientOptions options);
}
