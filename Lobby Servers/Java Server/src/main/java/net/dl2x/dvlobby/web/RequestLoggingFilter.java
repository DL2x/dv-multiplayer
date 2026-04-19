package net.dl2x.dvlobby.web;

import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.core.Ordered;
import org.springframework.core.annotation.Order;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Component;
import org.springframework.util.StreamUtils;
import org.springframework.web.filter.OncePerRequestFilter;
import org.springframework.web.util.ContentCachingResponseWrapper;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.util.UUID;

@Component
@Order(Ordered.HIGHEST_PRECEDENCE - 10)
public class RequestLoggingFilter extends OncePerRequestFilter {

  private static final Logger LOGGER = LoggerFactory.getLogger(RequestLoggingFilter.class);
  private static final int MAX_LOGGED_BODY_CHARS = 4000;

  @Override
  protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response, FilterChain filterChain)
      throws ServletException, IOException {
    String requestId = UUID.randomUUID().toString().substring(0, 8);
    long startedAt = System.currentTimeMillis();

    byte[] requestBodyBytes = StreamUtils.copyToByteArray(request.getInputStream());
    HttpServletRequest wrappedRequest = new CachedBodyHttpServletRequest(request, requestBodyBytes);
    String requestBody = new String(requestBodyBytes, StandardCharsets.UTF_8);
    String remoteAddress = resolveRemoteAddress(request);

    ContentCachingResponseWrapper wrappedResponse = new ContentCachingResponseWrapper(response);

    LOGGER.info("[{}] -> {} {} from {} body={}", requestId, request.getMethod(), request.getRequestURI(), remoteAddress, truncate(requestBody));

    try {
      filterChain.doFilter(wrappedRequest, wrappedResponse);
    } finally {
      long elapsedMs = System.currentTimeMillis() - startedAt;
      int status = wrappedResponse.getStatus();
      String responseBody = new String(wrappedResponse.getContentAsByteArray(), StandardCharsets.UTF_8);
      String reason = HttpStatus.resolve(status) != null ? HttpStatus.resolve(status).getReasonPhrase() : "Unknown";

      LOGGER.info("[{}] <- {} {} ({} ms) body={}", requestId, status, reason, elapsedMs, truncate(responseBody));
      wrappedResponse.copyBodyToResponse();
    }
  }

  private String resolveRemoteAddress(HttpServletRequest request) {
    String forwardedFor = request.getHeader("X-Forwarded-For");
    if (forwardedFor != null && !forwardedFor.isBlank()) {
      int commaIndex = forwardedFor.indexOf(',');
      return commaIndex > 0 ? forwardedFor.substring(0, commaIndex).trim() : forwardedFor.trim();
    }
    return request.getRemoteAddr();
  }

  private String truncate(String value) {
    if (value == null || value.isBlank()) {
      return "<empty>";
    }

    String normalized = value.replace("\r", "\\r").replace("\n", "\\n");
    if (normalized.length() <= MAX_LOGGED_BODY_CHARS) {
      return normalized;
    }

    return normalized.substring(0, MAX_LOGGED_BODY_CHARS) + "... [truncated]";
  }
}
