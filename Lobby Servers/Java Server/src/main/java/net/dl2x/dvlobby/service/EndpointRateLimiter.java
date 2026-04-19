package net.dl2x.dvlobby.service;

import jakarta.servlet.http.HttpServletRequest;
import net.dl2x.dvlobby.config.LobbyProperties;
import org.springframework.stereotype.Service;

import java.time.Instant;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

@Service
public class EndpointRateLimiter {

  private final LobbyProperties properties;
  private final Map<String, Instant> lastSeenByKey = new ConcurrentHashMap<>();

  public EndpointRateLimiter(LobbyProperties properties) {
    this.properties = properties;
  }

  public void consumeOrThrow(String endpointKey, HttpServletRequest request) {
    int limitSeconds = properties.rateLimitSeconds().getOrDefault(endpointKey, 0);
    if (limitSeconds <= 0) {
      return;
    }

    String remote = resolveRemoteIdentity(request);
    String bucket = endpointKey + "|" + remote;
    Instant now = Instant.now();

    Instant previous = lastSeenByKey.compute(bucket, (ignored, oldValue) -> {
      if (oldValue == null || oldValue.plusSeconds(limitSeconds).isBefore(now) || oldValue.plusSeconds(limitSeconds).equals(now)) {
        return now;
      }
      return oldValue;
    });

    if (previous != null && previous != now && previous.plusSeconds(limitSeconds).isAfter(now)) {
      long retryAfter = Math.max(1L, previous.plusSeconds(limitSeconds).getEpochSecond() - now.getEpochSecond());
      throw new RateLimitExceededException("Too many " + endpointKey + " requests from this IP", retryAfter);
    }
  }

  private String resolveRemoteIdentity(HttpServletRequest request) {
    String forwarded = request.getHeader("X-Forwarded-For");
    if (forwarded != null && !forwarded.isBlank()) {
      int comma = forwarded.indexOf(',');
      return (comma >= 0 ? forwarded.substring(0, comma) : forwarded).trim();
    }

    String realIp = request.getHeader("X-Real-IP");
    if (realIp != null && !realIp.isBlank()) {
      return realIp.trim();
    }

    return request.getRemoteAddr() == null ? "unknown" : request.getRemoteAddr();
  }
}
