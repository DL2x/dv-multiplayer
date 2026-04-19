package net.dl2x.dvlobby.api;

import com.fasterxml.jackson.annotation.JsonCreator;
import com.fasterxml.jackson.annotation.JsonValue;

import java.util.Arrays;

public enum HostingType {
  DEDICATED,
  STEAM,
  IP,
  BOTH;

  @JsonCreator
  public static HostingType fromJson(String value) {
    if (value == null || value.isBlank()) {
      return null;
    }

    return Arrays.stream(values())
        .filter(type -> type.name().equalsIgnoreCase(value.trim()))
        .findFirst()
        .orElseThrow(() -> new IllegalArgumentException("Unsupported hosting_type: " + value));
  }

  @JsonValue
  public String toJson() {
    return name().toLowerCase();
  }
}
