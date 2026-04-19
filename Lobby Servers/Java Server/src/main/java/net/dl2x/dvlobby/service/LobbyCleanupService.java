package net.dl2x.dvlobby.service;

import net.dl2x.dvlobby.config.LobbyProperties;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Service;

@Service
public class LobbyCleanupService {

  private static final Logger LOGGER = LoggerFactory.getLogger(LobbyCleanupService.class);

  private final LobbyService lobbyService;
  private final LobbyProperties properties;

  public LobbyCleanupService(LobbyService lobbyService, LobbyProperties properties) {
    this.lobbyService = lobbyService;
    this.properties = properties;
  }

  @Scheduled(fixedDelayString = "#{${lobby.cleanup-interval-seconds:60} * 1000}")
  public void cleanupStaleServers() {
    int removed = lobbyService.cleanupStaleServers();
    if (removed > 0) {
      LOGGER.info("Removed {} stale lobby entries", removed);
    }
  }

  public LobbyProperties properties() {
    return properties;
  }
}
