package net.dl2x.dvlobby.service;

public class PersistedLobbyStats {
  private long totalServers;
  private int maxServers;
  private long totalPlayers;
  private int maxPlayers;
  private long totalTimePlayedSeconds;

  public long getTotalServers() {
    return totalServers;
  }

  public void setTotalServers(long totalServers) {
    this.totalServers = totalServers;
  }

  public int getMaxServers() {
    return maxServers;
  }

  public void setMaxServers(int maxServers) {
    this.maxServers = maxServers;
  }

  public long getTotalPlayers() {
    return totalPlayers;
  }

  public void setTotalPlayers(long totalPlayers) {
    this.totalPlayers = totalPlayers;
  }

  public int getMaxPlayers() {
    return maxPlayers;
  }

  public void setMaxPlayers(int maxPlayers) {
    this.maxPlayers = maxPlayers;
  }

  public long getTotalTimePlayedSeconds() {
    return totalTimePlayedSeconds;
  }

  public void setTotalTimePlayedSeconds(long totalTimePlayedSeconds) {
    this.totalTimePlayedSeconds = totalTimePlayedSeconds;
  }
}
