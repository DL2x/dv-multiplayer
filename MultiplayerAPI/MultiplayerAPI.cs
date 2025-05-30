using MPAPI.Interfaces;

namespace MPAPI;

public static class MultiplayerAPI
{
    private static IMultiplayerAPI _instance;
    private static IServer _server;
    private static IClient _client;

    /// <summary>
    /// Gets whether the Multiplayer mod is available
    /// </summary>
    public static bool IsMultiplayerLoaded => _instance != null;

    /// <summary>
    /// Gets the current API instance (null if Multiplayer mod is not loaded)
    /// </summary>
    public static IMultiplayerAPI Instance => _instance;
    public static IServer Server => _server;
    public static IClient Client => _client;

    /// <summary>
    /// Internal method for the Multiplayer mod to register itself
    /// </summary>
    /// <param name="apiInstance">The API implementation</param>
    internal static void RegisterAPI(IMultiplayerAPI apiInstance)
    {
        _instance = apiInstance;
    }

    /// <summary>
    /// Internal method for the Multiplayer mod to register a client instance
    /// </summary>
    /// <param name="client">The Client implementation</param>
    internal static void RegisterClient(IClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Internal method for the Multiplayer mod to deregister a client instance
    /// </summary>
    internal static void ClearClient()
    {
        _client = null;
    }

    /// <summary>
    /// Internal method for the Multiplayer mod to register a server instance
    /// </summary>
    /// <param name="apiInstance">The API implementation</param>
    internal static void RegisterServer(IServer server)
    {
        _server = server;
    }


    /// <summary>
    /// Internal method for the Multiplayer mod to deregister a server instance
    /// </summary>
    internal static void ClearServer()
    {
        _server = null;
    }
}
