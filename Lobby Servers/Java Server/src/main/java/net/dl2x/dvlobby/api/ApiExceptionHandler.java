package net.dl2x.dvlobby.api;

import com.fasterxml.jackson.databind.exc.InvalidFormatException;
import jakarta.servlet.ServletException;
import net.dl2x.dvlobby.api.dto.ErrorResponse;
import net.dl2x.dvlobby.service.InvalidPrivateKeyException;
import net.dl2x.dvlobby.service.InvalidServerUpdateException;
import net.dl2x.dvlobby.service.RateLimitExceededException;
import net.dl2x.dvlobby.service.ServerCapacityReachedException;
import net.dl2x.dvlobby.service.ServerEntryTooLargeException;
import net.dl2x.dvlobby.service.ServerNotFoundException;
import net.dl2x.dvlobby.service.ServerProbeFailedException;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.http.converter.HttpMessageNotReadableException;
import org.springframework.validation.FieldError;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;

import java.util.stream.Collectors;

@RestControllerAdvice
public class ApiExceptionHandler {

  @ExceptionHandler(MethodArgumentNotValidException.class)
  public ResponseEntity<ErrorResponse> handleValidation(MethodArgumentNotValidException e) {
    String msg = e.getBindingResult().getFieldErrors().stream()
        .map(this::formatFieldError)
        .collect(Collectors.joining("; "));
    return ResponseEntity.badRequest().body(new ErrorResponse(msg));
  }

  @ExceptionHandler(HttpMessageNotReadableException.class)
  public ResponseEntity<ErrorResponse> handleBadJson(HttpMessageNotReadableException e) {
    if (e.getCause() instanceof InvalidFormatException ife && ife.getTargetType() == HostingType.class) {
      return ResponseEntity.badRequest().body(new ErrorResponse("hosting_type: unsupported value"));
    }
    return ResponseEntity.badRequest().body(new ErrorResponse("invalid JSON"));
  }

  @ExceptionHandler(RateLimitExceededException.class)
  public ResponseEntity<ErrorResponse> handleRateLimited(RateLimitExceededException e) {
    return ResponseEntity.status(HttpStatus.TOO_MANY_REQUESTS)
        .header(HttpHeaders.RETRY_AFTER, Long.toString(e.retryAfterSeconds()))
        .body(new ErrorResponse(e.getMessage()));
  }

  @ExceptionHandler(InvalidPrivateKeyException.class)
  public ResponseEntity<ErrorResponse> handleUnauthorized(InvalidPrivateKeyException e) {
    return ResponseEntity.status(HttpStatus.FORBIDDEN).body(new ErrorResponse(e.getMessage()));
  }

  @ExceptionHandler(ServerNotFoundException.class)
  public ResponseEntity<ErrorResponse> handleNotFound(ServerNotFoundException e) {
    return ResponseEntity.status(HttpStatus.NOT_FOUND).body(new ErrorResponse(e.getMessage()));
  }

  @ExceptionHandler(ServerCapacityReachedException.class)
  public ResponseEntity<ErrorResponse> handleCapacity(ServerCapacityReachedException e) {
    return ResponseEntity.status(HttpStatus.CONFLICT).body(new ErrorResponse(e.getMessage()));
  }

  @ExceptionHandler(ServerEntryTooLargeException.class)
  public ResponseEntity<ErrorResponse> handleEntryTooLarge(ServerEntryTooLargeException e) {
    return ResponseEntity.status(HttpStatus.PAYLOAD_TOO_LARGE).body(new ErrorResponse(e.getMessage()));
  }

  @ExceptionHandler({InvalidServerUpdateException.class, ServerProbeFailedException.class, IllegalArgumentException.class})
  public ResponseEntity<ErrorResponse> handleBadRequest(RuntimeException e) {
    return ResponseEntity.badRequest().body(new ErrorResponse(e.getMessage()));
  }

  @ExceptionHandler(ServletException.class)
  public ResponseEntity<ErrorResponse> handleServletException(ServletException e) {
    return ResponseEntity.badRequest().body(new ErrorResponse(e.getMessage()));
  }

  @ExceptionHandler(Exception.class)
  public ResponseEntity<ErrorResponse> handleGeneric(Exception e) {
    return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body(new ErrorResponse("internal server error"));
  }

  private String formatFieldError(FieldError fe) {
    return toJsonFieldPath(fe.getField()) + ": " + fe.getDefaultMessage();
  }

  private String toJsonFieldPath(String fieldPath) {
    if (fieldPath == null || fieldPath.isBlank()) {
      return fieldPath;
    }

    return fieldPath
        .replace("requiredMods", "required_mods")
        .replace("onlinePlayers", "online_players")
        .replace("gameServerId", "game_server_id")
        .replace("privateKey", "private_key")
        .replace("hostingType", "hosting_type")
        .replace("privateServer", "private")
        .replace("serverName", "server_name")
        .replace("passwordProtected", "password_protected")
        .replace("gameMode", "game_mode")
        .replace("timePassed", "time_passed")
        .replace("currentPlayers", "current_players")
        .replace("maxPlayers", "max_players")
        .replace("gameVersion", "game_version")
        .replace("multiplayerVersion", "multiplayer_version")
        .replace("serverInfo", "server_info");
  }
}
