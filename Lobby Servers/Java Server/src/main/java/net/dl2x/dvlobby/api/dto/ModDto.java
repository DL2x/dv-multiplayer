package net.dl2x.dvlobby.api.dto;

import com.fasterxml.jackson.annotation.JsonAlias;
import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import com.fasterxml.jackson.annotation.JsonProperty;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

@JsonIgnoreProperties(ignoreUnknown = true)
public record ModDto(
    @NotBlank
    @Size(max = 200)
    @JsonProperty("id")
    @JsonAlias({"Id", "id"})
    String id,

    @NotBlank
    @Size(max = 100)
    @JsonProperty("version")
    @JsonAlias({"Version", "version"})
    String version,

    @Size(max = 200)
    @JsonProperty("url")
    @JsonAlias({"Url", "url", "source", "Source"})
    String url
) {}
