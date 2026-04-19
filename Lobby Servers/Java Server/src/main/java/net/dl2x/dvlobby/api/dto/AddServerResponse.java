package net.dl2x.dvlobby.api.dto;

import com.fasterxml.jackson.annotation.JsonProperty;

public record AddServerResponse(
    @JsonProperty("game_server_id") String gameServerId,
    @JsonProperty("private_key") String privateKey
) {}
