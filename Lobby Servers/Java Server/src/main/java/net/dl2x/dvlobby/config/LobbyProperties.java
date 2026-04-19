package net.dl2x.dvlobby.config;

import org.springframework.boot.context.properties.ConfigurationProperties;

import java.util.List;

@ConfigurationProperties(prefix = "lobby")
public record LobbyProperties(
    int timeoutSeconds,
    int cleanupIntervalSeconds,
    int publicServerLimit,
    int steamServerLimit,
    int addRateLimitWindowSeconds,
    int addRateLimitRequestsPerWindow,
    int pingTimeoutMs,
    int maxAddRequestBodyBytes,
    int maxUpdateRequestBodyBytes,
    int maxRemoveRequestBodyBytes,
    int maxStoredEntryBytes,
    List<String> blockedTextRegex
) {
  public LobbyProperties {
    if (timeoutSeconds <= 0) timeoutSeconds = 120;
    if (cleanupIntervalSeconds <= 0) cleanupIntervalSeconds = 60;
    if (publicServerLimit <= 0) publicServerLimit = 100;
    if (steamServerLimit <= 0) steamServerLimit = 100;
    if (addRateLimitWindowSeconds <= 0) addRateLimitWindowSeconds = 60;
    if (addRateLimitRequestsPerWindow <= 0) addRateLimitRequestsPerWindow = 5;
    if (pingTimeoutMs <= 0) pingTimeoutMs = 1500;
    if (maxAddRequestBodyBytes <= 0) maxAddRequestBodyBytes = 8192;
    if (maxUpdateRequestBodyBytes <= 0) maxUpdateRequestBodyBytes = 4096;
    if (maxRemoveRequestBodyBytes <= 0) maxRemoveRequestBodyBytes = 1024;
    if (maxStoredEntryBytes <= 0) maxStoredEntryBytes = 6144;
    if (blockedTextRegex == null) blockedTextRegex = List.of();
  }
}
