
namespace Common
{
    public interface IClient
    {
        Task Run(ClientOptions options);
    }
}