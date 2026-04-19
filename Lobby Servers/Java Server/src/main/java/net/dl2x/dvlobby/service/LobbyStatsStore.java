package net.dl2x.dvlobby.service;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.springframework.stereotype.Service;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardOpenOption;

@Service
public class LobbyStatsStore {

  private static final Path STATS_PATH = Path.of("stats.json");

  private final ObjectMapper objectMapper;

  public LobbyStatsStore(ObjectMapper objectMapper) {
    this.objectMapper = objectMapper;
  }

  public synchronized PersistedLobbyStats loadOrCreate() {
    try {
      if (Files.notExists(STATS_PATH)) {
        PersistedLobbyStats defaults = new PersistedLobbyStats();
        save(defaults);
        return defaults;
      }

      try (InputStream in = Files.newInputStream(STATS_PATH)) {
        PersistedLobbyStats loaded = objectMapper.readValue(in, PersistedLobbyStats.class);
        return loaded == null ? new PersistedLobbyStats() : loaded;
      }
    } catch (Exception ignored) {
      PersistedLobbyStats fallback = new PersistedLobbyStats();
      try {
        save(fallback);
      } catch (Exception ignoredAgain) {
        // Ignore secondary failure.
      }
      return fallback;
    }
  }

  public synchronized void save(PersistedLobbyStats stats) {
    try {
      Path parent = STATS_PATH.getParent();
      if (parent != null) {
        Files.createDirectories(parent);
      }
      try (OutputStream out = Files.newOutputStream(
          STATS_PATH,
          StandardOpenOption.CREATE,
          StandardOpenOption.TRUNCATE_EXISTING,
          StandardOpenOption.WRITE)) {
        objectMapper.writerWithDefaultPrettyPrinter().writeValue(out, stats);
      }
    } catch (IOException e) {
      throw new IllegalStateException("failed to persist lobby stats", e);
    }
  }
}
