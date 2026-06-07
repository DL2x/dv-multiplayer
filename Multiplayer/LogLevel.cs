namespace Multiplayer;

/// <summary>
/// Minimum log verbosity that gets written. Higher value = more verbose.
/// A message of severity S is emitted when (int)S &lt;= (int)Settings.LogLevel.
/// Dedicated servers default to <see cref="Warning"/> to avoid per-tick log spam
/// (which on a headless server is synchronous IO on the network-tick thread).
/// </summary>
public enum LogLevel
{
    Error = 0,
    Warning = 1,
    Info = 2,
    Debug = 3,
}
