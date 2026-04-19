package net.dl2x.dvlobby.service;

import jakarta.servlet.http.HttpServletRequest;
import org.springframework.stereotype.Service;

/**
 * Legacy compatibility wrapper.
 *
 * Older project copies referenced AddRateLimiter. The new server uses
 * EndpointRateLimiter with per-endpoint cooldowns from config.json.
 * This wrapper forwards to the "add" endpoint limit so stale source trees
 * still compile when files are overwritten in place.
 */
@Service
@Deprecated
public class AddRateLimiter {

  private final EndpointRateLimiter endpointRateLimiter;

  public AddRateLimiter(EndpointRateLimiter endpointRateLimiter) {
    this.endpointRateLimiter = endpointRateLimiter;
  }

  public void consumeOrThrow(HttpServletRequest request) {
    endpointRateLimiter.consumeOrThrow("add", request);
  }
}
