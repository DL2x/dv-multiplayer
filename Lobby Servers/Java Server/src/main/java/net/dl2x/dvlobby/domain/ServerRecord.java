package net.dl2x.dvlobby.domain;

import net.dl2x.dvlobby.api.HostingType;
import net.dl2x.dvlobby.api.dto.ModDto;
import net.dl2x.dvlobby.api.dto.PublicServerDto;

import java.time.Instant;
import java.util.List;

public class ServerRecord {
  private final String gameServerId;
  private final String privateKey;
  private final String address;
  private final String ipv4;
  private final String ipv6;
  private final int port;
  private final HostingType hostingType;
  private final String serverName;
  private final boolean passwordProtected;
  private final int gameMode;
  private final int difficulty;
  private volatile String timePassed;
  private volatile int currentPlayers;
  private final int maxPlayers;
  private final List<ModDto> requiredMods;
  private final String gameVersion;
  private final String multiplayerVersion;
  private final String serverInfo;
  private final Instant startTime;
  private volatile Instant lastUpdateAt;
  private volatile boolean ready;
  private volatile List<String> onlinePlayers;

  public ServerRecord(
      String gameServerId,
      String privateKey,
      String address,
      String ipv4,
      String ipv6,
      int port,
      HostingType hostingType,
      String serverName,
      boolean passwordProtected,
      int gameMode,
      int difficulty,
      String timePassed,
      int currentPlayers,
      int maxPlayers,
      List<ModDto> requiredMods,
      String gameVersion,
      String multiplayerVersion,
      String serverInfo,
      Instant startTime,
      boolean ready,
      List<String> onlinePlayers
  ) {
    this.gameServerId = gameServerId;
    this.privateKey = privateKey;
    this.address = address;
    this.ipv4 = ipv4;
    this.ipv6 = ipv6;
    this.port = port;
    this.hostingType = hostingType;
    this.serverName = serverName;
    this.passwordProtected = passwordProtected;
    this.gameMode = gameMode;
    this.difficulty = difficulty;
    this.timePassed = timePassed;
    this.currentPlayers = currentPlayers;
    this.maxPlayers = maxPlayers;
    this.requiredMods = List.copyOf(requiredMods);
    this.gameVersion = gameVersion;
    this.multiplayerVersion = multiplayerVersion;
    this.serverInfo = serverInfo;
    this.startTime = startTime;
    this.ready = ready;
    this.onlinePlayers = List.copyOf(onlinePlayers);
    touch();
  }

  public synchronized int currentPlayersSnapshot() {
    return currentPlayers;
  }

  public synchronized void updateState(int currentPlayers, String timePassed, Boolean ready, List<String> onlinePlayers) {
    this.currentPlayers = currentPlayers;
    this.timePassed = timePassed;
    this.onlinePlayers = List.copyOf(onlinePlayers);
    if (Boolean.TRUE.equals(ready)) {
      this.ready = true;
    }
    touch();
  }

  public synchronized String timePassedSnapshot() {
    return timePassed;
  }

  public void touch() {
    this.lastUpdateAt = Instant.now();
  }

  public String gameServerId() { return gameServerId; }
  public String privateKey() { return privateKey; }
  public String address() { return address; }
  public String ipv4() { return ipv4; }
  public String ipv6() { return ipv6; }
  public int port() { return port; }
  public HostingType hostingType() { return hostingType; }
  public String serverName() { return serverName; }
  public boolean passwordProtected() { return passwordProtected; }
  public int gameMode() { return gameMode; }
  public int difficulty() { return difficulty; }
  public String timePassed() { return timePassed; }
  public int currentPlayers() { return currentPlayers; }
  public int maxPlayers() { return maxPlayers; }
  public List<ModDto> requiredMods() { return requiredMods; }
  public String gameVersion() { return gameVersion; }
  public String multiplayerVersion() { return multiplayerVersion; }
  public String serverInfo() { return serverInfo; }
  public Instant startTime() { return startTime; }
  public Instant lastUpdateAt() { return lastUpdateAt; }
  public boolean ready() { return ready; }
  public List<String> onlinePlayers() { return onlinePlayers; }

  public PublicServerDto toPublicDto() {
    return new PublicServerDto(
        address,
        ipv4,
        ipv6,
        port,
        hostingType,
        serverName,
        passwordProtected,
        gameMode,
        difficulty,
        timePassed,
        currentPlayers,
        maxPlayers,
        requiredMods,
        gameVersion,
        multiplayerVersion,
        serverInfo,
        gameServerId,
        startTime,
        ready,
        onlinePlayers
    );
  }
}
