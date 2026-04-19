package net.dl2x.dvlobby.api.dto;

import com.fasterxml.jackson.annotation.JsonProperty;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

public record RemoveServerRequest(
    @NotBlank @Size(max = 64)
    @JsonProperty("game_server_id") String gameServerId,

    @NotBlank @Size(max = 128)
    @JsonProperty("private_key") String privateKey
) {}
