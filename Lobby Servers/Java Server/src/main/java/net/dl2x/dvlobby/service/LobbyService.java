package net.dl2x.dvlobby.service;

import com.fasterxml.jackson.databind.ObjectMapper;
import net.dl2x.dvlobby.api.HostingType;
import net.dl2x.dvlobby.api.dto.AddServerRequest;
import net.dl2x.dvlobby.api.dto.AddServerResponse;
import net.dl2x.dvlobby.api.dto.LobbyStatsResponse;
import net.dl2x.dvlobby.api.dto.PublicServerDto;
import net.dl2x.dvlobby.api.dto.RemoveServerRequest;
import net.dl2x.dvlobby.api.dto.UpdateServerRequest;
import net.dl2x.dvlobby.config.LobbyProperties;
import net.dl2x.dvlobby.domain.ServerRecord;
import org.springframework.stereotype.Service;

import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.security.SecureRandom;
import java.time.Instant;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.HexFormat;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ConcurrentMap;
import java.util.stream.Collectors;

@Service
public class LobbyService {

  private final LobbyProperties properties;
  private final ServerProbeService probeService;
  private final TextFilterService textFilterService;
  private final ObjectMapper objectMapper;
  private final LobbyStatsStore statsStore;
  private final ConcurrentMap<String, ServerRecord> servers = new ConcurrentHashMap<>();
  private final Object capacityGate = new Object();
  private final Object statsGate = new Object();
  private final SecureRandom secureRandom = new SecureRandom();
  private final PersistedLobbyStats persistedStats;

  public LobbyService(
      LobbyProperties properties,
      ServerProbeService probeService,
      TextFilterService textFilterService,
      ObjectMapper objectMapper,
      LobbyStatsStore statsStore
  ) {
    this.properties = properties;
    this.probeService = probeService;
    this.textFilterService = textFilterService;
    this.objectMapper = objectMapper;
    this.statsStore = statsStore;
    this.persistedStats = statsStore.loadOrCreate();
  }

  public AddServerResponse add(AddServerRequest rawRequest) {
    AddServerRequest request = rawRequest.normalized();
    request.validateBusinessRules();
    textFilterService.validateAddOrThrow(request);


    synchronized (capacityGate) {
      enforceCapacity(request.hostingType());
      probeService.probeOrThrow(request);

      ServerRecord record = buildRecord(request);
      ensureEntryFits(record);
      if (servers.putIfAbsent(record.gameServerId(), record) != null) {
        throw new InvalidServerUpdateException("failed to allocate server id");
      }

      noteServerAdded(record.currentPlayers());
      return new AddServerResponse(record.gameServerId(), record.privateKey());
    }
  }

  public void update(UpdateServerRequest rawRequest) {
    UpdateServerRequest req = rawRequest.normalized();
    textFilterService.validateUpdateOrThrow(req);

    ServerRecord record = servers.get(req.gameServerId());
    if (record == null) throw new ServerNotFoundException(req.gameServerId());
    if (!secureEquals(record.privateKey(), req.privateKey())) throw new InvalidPrivateKeyException();
    int effectiveCurrentPlayers = req.effectiveCurrentPlayers();
    if (effectiveCurrentPlayers > record.maxPlayers()) {
      throw new InvalidServerUpdateException("current_players exceeds max_players");
    }
    if (req.onlinePlayers().size() > record.maxPlayers()) {
      throw new InvalidServerUpdateException("online_players exceeds max_players");
    }

    int oldPlayers = record.currentPlayersSnapshot();
    record.updateState(effectiveCurrentPlayers, req.timePassed().trim(), req.ready(), req.onlinePlayers());
    notePlayerObservation(oldPlayers, effectiveCurrentPlayers);
  }

  public void remove(RemoveServerRequest req) {
    ServerRecord record = servers.get(req.gameServerId());
    if (record == null) throw new ServerNotFoundException(req.gameServerId());
    if (!secureEquals(record.privateKey(), req.privateKey())) throw new InvalidPrivateKeyException();
    if (servers.remove(req.gameServerId(), record)) {
      noteServerRemoved(record.timePassedSnapshot());
    }
  }

  public List<PublicServerDto> list() {
    return servers.values().stream()
        .filter(ServerRecord::ready)
        .filter(record -> record.hostingType() != HostingType.STEAM)
        .filter(record -> !record.privateServer())
        .sorted(Comparator.comparing(ServerRecord::lastUpdateAt).reversed()
            .thenComparing(ServerRecord::serverName, String.CASE_INSENSITIVE_ORDER))
        .map(ServerRecord::toPublicDto)
        .toList();
  }

  public LobbyStatsResponse stats() {
    int currentServers = servers.size();
    int currentPlayers = currentPlayersTotal();
    Map<String, Integer> currentByType = servers.values().stream()
        .collect(Collectors.groupingBy(
            s -> s.hostingType().toJson(),
            Collectors.collectingAndThen(Collectors.counting(), Long::intValue)
        ));

    synchronized (statsGate) {
      return new LobbyStatsResponse(
          currentServers,
          persistedStats.getTotalServers(),
          persistedStats.getMaxServers(),
          currentPlayers,
          persistedStats.getTotalPlayers(),
          persistedStats.getMaxPlayers(),
          persistedStats.getTotalTimePlayedSeconds(),
          currentByType,
          properties.publicServerLimit(),
          properties.steamServerLimit()
      );
    }
  }

  public int cleanupStaleServers() {
    Instant cutoff = Instant.now().minusSeconds(properties.timeoutSeconds());
    List<ServerRecord> staleRecords = new ArrayList<>();

    for (Map.Entry<String, ServerRecord> entry : servers.entrySet()) {
      if (entry.getValue().lastUpdateAt().isBefore(cutoff)) {
        staleRecords.add(entry.getValue());
      }
    }

    int removed = 0;
    for (ServerRecord stale : staleRecords) {
      if (servers.remove(stale.gameServerId(), stale)) {
        noteServerRemoved(stale.timePassedSnapshot());
        removed++;
      }
    }
    return removed;
  }

  private void ensureEntryFits(ServerRecord record) {
    try {
      byte[] payload = objectMapper.writeValueAsBytes(record.toPublicDto());
      if (payload.length > properties.maxStoredEntryBytes()) {
        throw new ServerEntryTooLargeException(properties.maxStoredEntryBytes());
      }
    } catch (ServerEntryTooLargeException e) {
      throw e;
    } catch (Exception e) {
      throw new InvalidServerUpdateException("failed to serialize normalized server entry");
    }
  }

  private ServerRecord buildRecord(AddServerRequest request) {
    return new ServerRecord(
        UUID.randomUUID().toString().replace("-", ""),
        generatePrivateKeyHex16(),
        buildAddress(request),
        request.port(),
        request.hostingType(),
        request.privateServer(),
        request.serverName(),
        request.passwordProtected(),
        request.gameMode(),
        request.difficulty(),
        request.timePassed(),
        request.effectiveCurrentPlayers(),
        request.maxPlayers(),
        request.effectiveMods(),
        request.gameVersion(),
        request.multiplayerVersion(),
        request.serverInfo(),
        Instant.now(),
        false,
        request.onlinePlayers()
    );
  }

  private String buildAddress(AddServerRequest request) {
    return request.address().trim();
  }

  private void enforceCapacity(HostingType hostingType) {
    int bucketSize = countServersByBucket(hostingType);
    if (hostingType == HostingType.STEAM) {
      if (bucketSize >= properties.steamServerLimit()) {
        throw new ServerCapacityReachedException("steam", properties.steamServerLimit());
      }
      return;
    }
    if (bucketSize >= properties.publicServerLimit()) {
      throw new ServerCapacityReachedException("public", properties.publicServerLimit());
    }
  }

  private int countServersByBucket(HostingType hostingType) {
    if (hostingType == HostingType.STEAM) {
      return (int) servers.values().stream().filter(server -> server.hostingType() == HostingType.STEAM).count();
    }
    return (int) servers.values().stream().filter(server -> server.hostingType() != HostingType.STEAM).count();
  }

  private void noteServerAdded(int initialPlayers) {
    synchronized (statsGate) {
      persistedStats.setTotalServers(persistedStats.getTotalServers() + 1L);
      int currentServers = servers.size();
      if (currentServers > persistedStats.getMaxServers()) {
        persistedStats.setMaxServers(currentServers);
      }

      long newTotalPlayers = persistedStats.getTotalPlayers() + Math.max(0, initialPlayers);
      persistedStats.setTotalPlayers(newTotalPlayers);

      int currentPlayers = currentPlayersTotal();
      if (currentPlayers > persistedStats.getMaxPlayers()) {
        persistedStats.setMaxPlayers(currentPlayers);
      }
      statsStore.save(persistedStats);
    }
  }

  private void notePlayerObservation(int oldPlayers, int newPlayers) {
    synchronized (statsGate) {
      int positiveDelta = Math.max(0, newPlayers - oldPlayers);
      if (positiveDelta > 0) {
        persistedStats.setTotalPlayers(persistedStats.getTotalPlayers() + positiveDelta);
      }
      int currentPlayers = currentPlayersTotal();
      if (currentPlayers > persistedStats.getMaxPlayers()) {
        persistedStats.setMaxPlayers(currentPlayers);
      }
      statsStore.save(persistedStats);
    }
  }

  private void noteServerRemoved(String timePassed) {
    synchronized (statsGate) {
      persistedStats.setTotalTimePlayedSeconds(
          persistedStats.getTotalTimePlayedSeconds() + parseTimePassedSeconds(timePassed));
      statsStore.save(persistedStats);
    }
  }

  private int currentPlayersTotal() {
    return servers.values().stream().mapToInt(ServerRecord::currentPlayers).sum();
  }

  private long parseTimePassedSeconds(String value) {
    if (value == null || value.isBlank()) {
      return 0L;
    }

    String trimmed = value.trim();
    if (trimmed.matches("\\d+")) {
      return Long.parseLong(trimmed);
    }

    String lower = trimmed.toLowerCase();
    long total = 0L;
    java.util.regex.Matcher matcher = java.util.regex.Pattern.compile("(\\d+)\\s*([dhms])").matcher(lower);
    boolean matched = false;
    while (matcher.find()) {
      matched = true;
      long amount = Long.parseLong(matcher.group(1));
      switch (matcher.group(2)) {
        case "d" -> total += amount * 86_400L;
        case "h" -> total += amount * 3_600L;
        case "m" -> total += amount * 60L;
        case "s" -> total += amount;
        default -> {
        }
      }
    }
    if (matched) {
      return total;
    }

    String[] parts = trimmed.split(":");
    try {
      if (parts.length == 2) {
        return Long.parseLong(parts[0]) * 60L + Long.parseLong(parts[1]);
      }
      if (parts.length == 3) {
        return Long.parseLong(parts[0]) * 3_600L
            + Long.parseLong(parts[1]) * 60L
            + Long.parseLong(parts[2]);
      }
      if (parts.length == 4) {
        return Long.parseLong(parts[0]) * 86_400L
            + Long.parseLong(parts[1]) * 3_600L
            + Long.parseLong(parts[2]) * 60L
            + Long.parseLong(parts[3]);
      }
    } catch (NumberFormatException ignored) {
      return 0L;
    }
    return 0L;
  }

  private String generatePrivateKeyHex16() {
    byte[] bytes = new byte[16];
    secureRandom.nextBytes(bytes);
    return HexFormat.of().formatHex(bytes);
  }

  private boolean secureEquals(String a, String b) {
    return MessageDigest.isEqual(a.getBytes(StandardCharsets.UTF_8), b.getBytes(StandardCharsets.UTF_8));
  }
}
