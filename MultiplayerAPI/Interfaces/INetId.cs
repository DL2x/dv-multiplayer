
namespace MPAPI.Interfaces;

public interface INetIdProvider
{
    bool TryGetNetId<T>(T obj, out ushort netId) where T : class;
    bool TryGetObject<T>(ushort netId, out T obj) where T : class;
}
