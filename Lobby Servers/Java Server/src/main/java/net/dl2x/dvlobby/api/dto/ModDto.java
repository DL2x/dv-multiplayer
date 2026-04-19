package net.dl2x.dvlobby.api.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

public record ModDto(
    @NotBlank @Size(max = 200) String id,
    @NotBlank @Size(max = 100) String version,
    @Size(max = 200) String source
) {}
