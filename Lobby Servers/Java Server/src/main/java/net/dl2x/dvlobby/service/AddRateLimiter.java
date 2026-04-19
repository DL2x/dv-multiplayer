package net.dl2x.dvlobby.service;

import jakarta.servlet.http.HttpServletRequest;
import net.dl2x.dvlobby.config.LobbyProperties;
import org.springframework.stereotype.Service;

import java.time.Duration;
import java.time.Instant;
import java.util.ArrayDeque;
import java.util.Deque;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

@Service
public class AddRateLimiter {

  private final LobbyProperties properties;
  private final Map<String, Deque<Instant>> attempts = new ConcurrentHashMap<>();

  public AddRateLimiter(LobbyProperties properties) {
    this.properties = properties;
  }

  public void consumeOrThrow(HttpServletRequest request) {
    String key = resolveRemoteIdentity(request);
    Instant now = Instant.now();
    Duration window = Duration.ofSeconds(properties.addRateLimitWindowSeconds());
    Deque<Instant> queue = attempts.computeIfAbsent(key, ignored -> new ArrayDeque<>());

    synchronized (queue) {
      while (!queue.isEmpty() && Duration.between(queue.peekFirst(), now).compareTo(window) > 0) {
        queue.removeFirst();
      }

      if (queue.size() >= properties.addRateLimitRequestsPerWindow()) {
        long retryAfter = Math.max(0L, window.minus(Duration.between(queue.peekFirst(), now)).toSeconds());
        throw new RateLimitExceededException("Too many add requests from this IP", retryAfter);
      }

      queue.addLast(now);
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
