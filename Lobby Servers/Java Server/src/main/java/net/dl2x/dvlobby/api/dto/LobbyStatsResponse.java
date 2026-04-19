package net.dl2x.dvlobby.api.dto;

import com.fasterxml.jackson.annotation.JsonProperty;

import java.util.Map;

public record LobbyStatsResponse(
    @JsonProperty("current_servers") int currentServers,
    @JsonProperty("total_servers") long totalServers,
    @JsonProperty("max_servers") int maxServers,
    @JsonProperty("current_players") int currentPlayers,
    @JsonProperty("total_players") long totalPlayers,
    @JsonProperty("max_players") int maxPlayers,
    @JsonProperty("total_time_played_seconds") long totalTimePlayedSeconds,
    @JsonProperty("current_servers_by_type") Map<String, Integer> currentServersByType,
    @JsonProperty("public_server_limit") int publicServerLimit,
    @JsonProperty("steam_server_limit") int steamServerLimit
) {}
