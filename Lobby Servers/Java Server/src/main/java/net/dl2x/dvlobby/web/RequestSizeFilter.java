package net.dl2x.dvlobby.web;

import com.fasterxml.jackson.databind.ObjectMapper;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import net.dl2x.dvlobby.api.dto.ErrorResponse;
import net.dl2x.dvlobby.config.LobbyProperties;
import org.springframework.core.Ordered;
import org.springframework.core.annotation.Order;
import org.springframework.http.MediaType;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

import java.io.IOException;

@Component
@Order(Ordered.HIGHEST_PRECEDENCE)
public class RequestSizeFilter extends OncePerRequestFilter {

  private final LobbyProperties properties;
  private final ObjectMapper objectMapper;

  public RequestSizeFilter(LobbyProperties properties, ObjectMapper objectMapper) {
    this.properties = properties;
    this.objectMapper = objectMapper;
  }

  @Override
  protected boolean shouldNotFilter(HttpServletRequest request) {
    return !"POST".equalsIgnoreCase(request.getMethod());
  }

  @Override
  protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response, FilterChain filterChain)
      throws ServletException, IOException {
    int limit = resolveLimit(request.getRequestURI());
    if (limit <= 0) {
      filterChain.doFilter(request, response);
      return;
    }

    long contentLength = request.getContentLengthLong();
    if (contentLength > limit) {
      writePayloadTooLarge(response, limit);
      return;
    }

    byte[] body = request.getInputStream().readNBytes(limit + 1);
    if (body.length > limit) {
      writePayloadTooLarge(response, limit);
      return;
    }

    filterChain.doFilter(new CachedBodyHttpServletRequest(request, body), response);
  }

  private int resolveLimit(String uri) {
    return switch (uri) {
      case "/add", "/add_game_server" -> properties.maxAddRequestBodyBytes();
      case "/update", "/update_game_server" -> properties.maxUpdateRequestBodyBytes();
      case "/remove", "/remove_game_server" -> properties.maxRemoveRequestBodyBytes();
      default -> 0;
    };
  }

  private void writePayloadTooLarge(HttpServletResponse response, int limit) throws IOException {
    response.setStatus(HttpServletResponse.SC_REQUEST_ENTITY_TOO_LARGE);
    response.setContentType(MediaType.APPLICATION_JSON_VALUE);
    objectMapper.writeValue(response.getOutputStream(), new ErrorResponse("Request exceeds size limit of " + limit + " bytes"));
  }
}
