package net.dl2x.dvlobby.api.dto;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import com.fasterxml.jackson.annotation.JsonProperty;
import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

import java.util.List;

@JsonIgnoreProperties(ignoreUnknown = true)
public record UpdateServerRequest(
    @NotBlank @Size(max = 64)
    @JsonProperty("game_server_id") String gameServerId,

    @NotBlank @Size(max = 128)
    @JsonProperty("private_key") String privateKey,

    @Min(0) @Max(512)
    @JsonProperty("current_players") int currentPlayers,

    @NotBlank @Size(max = 100)
    @JsonProperty("time_passed") String timePassed,

    @JsonProperty("ready") Boolean ready,

    @Size(max = 512)
    @JsonProperty("online_players") List<@Size(max = 64) String> onlinePlayers
) {
  public UpdateServerRequest {
    if (onlinePlayers == null) onlinePlayers = List.of();
  }

  public UpdateServerRequest normalized() {
    List<String> normalizedPlayers = onlinePlayers.stream()
        .map(value -> value == null ? null : value.trim())
        .filter(value -> value != null && !value.isEmpty())
        .toList();
    return new UpdateServerRequest(
        gameServerId == null ? null : gameServerId.trim(),
        privateKey == null ? null : privateKey.trim(),
        currentPlayers,
        timePassed == null ? null : timePassed.trim(),
        ready,
        normalizedPlayers
    );
  }
}
