package net.dl2x.dvlobby.api.dto;

import com.fasterxml.jackson.annotation.JsonAlias;
import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import com.fasterxml.jackson.annotation.JsonProperty;
import jakarta.validation.Valid;
import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;
import net.dl2x.dvlobby.api.HostingType;

import java.util.ArrayList;
import java.util.List;

@JsonIgnoreProperties(ignoreUnknown = true)
public record AddServerRequest(
    @NotBlank
    @Size(max = 300)
    String address,

    @Min(0) @Max(65535)
    int port,

    @NotNull
    @JsonProperty("hosting_type")
    HostingType hostingType,

    @JsonProperty("private")
    boolean privateServer,

    @NotBlank @Size(max = 64)
    @JsonProperty("server_name")
    String serverName,

    @JsonProperty("password_protected")
    boolean passwordProtected,

    @Min(0) @Max(2)
    @JsonProperty("game_mode")
    int gameMode,

    @Min(0) @Max(3)
    int difficulty,

    @NotBlank @Size(max = 100)
    @JsonProperty("time_passed")
    String timePassed,

    @Min(0) @Max(512)
    @JsonProperty("current_players")
    int currentPlayers,

    @Min(1) @Max(512)
    @JsonProperty("max_players")
    int maxPlayers,

    @Valid
    @JsonProperty("required_mods")
    List<ModDto> requiredMods,

    @Valid
    @JsonAlias("mods")
    List<ModDto> mods,

    @NotBlank @Size(max = 50)
    @JsonProperty("game_version")
    String gameVersion,

    @Size(max = 50)
    @JsonProperty("multiplayer_version")
    String multiplayerVersion,

    @Size(max = 500)
    @JsonProperty("server_info")
    String serverInfo,

    @Size(max = 512)
    @JsonProperty("online_players")
    List<@Size(max = 64) String> onlinePlayers
) {
  public AddServerRequest {
    if (requiredMods == null) requiredMods = List.of();
    if (mods == null) mods = List.of();
    if (onlinePlayers == null) onlinePlayers = List.of();
  }

  public List<ModDto> effectiveMods() {
    if (!requiredMods.isEmpty()) {
      return List.copyOf(requiredMods);
    }
    if (!mods.isEmpty()) {
      return List.copyOf(mods);
    }
    return List.of();
  }

  public int effectiveCurrentPlayers() {
    return !onlinePlayers.isEmpty() ? onlinePlayers.size() : currentPlayers;
  }

  public void validateBusinessRules() {
    if (effectiveCurrentPlayers() > maxPlayers) {
      throw new IllegalArgumentException("current_players must not exceed max_players");
    }

    if (onlinePlayers.size() > maxPlayers) {
      throw new IllegalArgumentException("online_players must not exceed max_players");
    }

    if ((hostingType == HostingType.IP || hostingType == HostingType.BOTH || hostingType == HostingType.DEDICATED) && port <= 0) {
      throw new IllegalArgumentException("port must be set for ip, dedicated and both hosting types");
    }

    if (effectiveMods().size() > 64) {
      throw new IllegalArgumentException("Too many mods in request");
    }
  }

  public AddServerRequest normalized() {
    List<ModDto> normalizedMods = new ArrayList<>(effectiveMods());
    List<String> normalizedPlayers = onlinePlayers.stream()
        .map(AddServerRequest::trimToNull)
        .filter(value -> value != null)
        .toList();

    return new AddServerRequest(
        trimToNull(address),
        port,
        hostingType,
        privateServer,
        serverName == null ? null : serverName.trim(),
        passwordProtected,
        gameMode,
        difficulty,
        timePassed == null ? null : timePassed.trim(),
        currentPlayers,
        maxPlayers,
        normalizedMods,
        List.of(),
        gameVersion == null ? null : gameVersion.trim(),
        trimToNull(multiplayerVersion),
        trimToNull(serverInfo),
        normalizedPlayers
    );
  }

  private static String trimToNull(String value) {
    if (value == null) return null;
    String trimmed = value.trim();
    return trimmed.isEmpty() ? null : trimmed;
  }
}
