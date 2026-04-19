package net.dl2x.dvlobby.api.dto;

import com.fasterxml.jackson.annotation.JsonProperty;

public record ErrorResponse(@JsonProperty("error") String error) {}
