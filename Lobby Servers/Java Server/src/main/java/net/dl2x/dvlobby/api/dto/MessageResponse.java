package net.dl2x.dvlobby.api.dto;

import com.fasterxml.jackson.annotation.JsonProperty;

public record MessageResponse(@JsonProperty("message") String message) {}
