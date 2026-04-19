package net.dl2x.dvlobby.api.dto;

import com.fasterxml.jackson.annotation.JsonProperty;
import net.dl2x.dvlobby.api.HostingType;

import java.time.Instant;
import java.util.List;

public record PublicServerDto(
    String address,
    String ipv4,
    String ipv6,
    int port,
    @JsonProperty("hosting_type") HostingType hostingType,
    @JsonProperty("server_name") String serverName,
    @JsonProperty("password_protected") boolean passwordProtected,
    @JsonProperty("game_mode") int gameMode,
    int difficulty,
    @JsonProperty("time_passed") String timePassed,
    @JsonProperty("current_players") int currentPlayers,
    @JsonProperty("max_players") int maxPlayers,
    @JsonProperty("required_mods") List<ModDto> requiredMods,
    @JsonProperty("game_version") String gameVersion,
    @JsonProperty("multiplayer_version") String multiplayerVersion,
    @JsonProperty("server_info") String serverInfo,
    @JsonProperty("game_server_id") String gameServerId,
    @JsonProperty("start_time") Instant startTime,
    boolean ready,
    @JsonProperty("online_players") List<String> onlinePlayers
) {}
