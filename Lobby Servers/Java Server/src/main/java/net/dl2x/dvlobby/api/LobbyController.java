package net.dl2x.dvlobby.api;

import jakarta.servlet.http.HttpServletRequest;
import jakarta.validation.Valid;
import net.dl2x.dvlobby.api.dto.AddServerRequest;
import net.dl2x.dvlobby.api.dto.AddServerResponse;
import net.dl2x.dvlobby.api.dto.LobbyStatsResponse;
import net.dl2x.dvlobby.api.dto.MessageResponse;
import net.dl2x.dvlobby.api.dto.PublicServerDto;
import net.dl2x.dvlobby.api.dto.RemoveServerRequest;
import net.dl2x.dvlobby.api.dto.UpdateServerRequest;
import net.dl2x.dvlobby.service.EndpointRateLimiter;
import net.dl2x.dvlobby.service.LobbyService;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;

@RestController
@RequestMapping(produces = MediaType.APPLICATION_JSON_VALUE)
public class LobbyController {

  private final LobbyService service;
  private final EndpointRateLimiter rateLimiter;

  public LobbyController(LobbyService service, EndpointRateLimiter rateLimiter) {
    this.service = service;
    this.rateLimiter = rateLimiter;
  }

  @GetMapping("/")
  public MessageResponse root() {
    return new MessageResponse("DV Lobby API online");
  }

  @GetMapping("/favicon.ico")
  @ResponseStatus(HttpStatus.NO_CONTENT)
  public void favicon() {
    // Intentionally empty to avoid noisy 400s in browser logs.
  }

  @GetMapping("/list")
  public List<PublicServerDto> list(HttpServletRequest request) {
    rateLimiter.consumeOrThrow("list", request);
    return service.list();
  }

  @GetMapping("/stats")
  public LobbyStatsResponse stats(HttpServletRequest request) {
    rateLimiter.consumeOrThrow("stats", request);
    return service.stats();
  }

  @PostMapping(value = "/add", consumes = MediaType.APPLICATION_JSON_VALUE)
  public AddServerResponse add(@Valid @RequestBody AddServerRequest req, HttpServletRequest httpRequest) {
    rateLimiter.consumeOrThrow("add", httpRequest);
    return service.add(req);
  }

  @PostMapping(value = "/update", consumes = MediaType.APPLICATION_JSON_VALUE)
  public MessageResponse update(@Valid @RequestBody UpdateServerRequest req, HttpServletRequest request) {
    rateLimiter.consumeOrThrow("update", request);
    service.update(req);
    return new MessageResponse("Server updated");
  }

  @PostMapping(value = "/remove", consumes = MediaType.APPLICATION_JSON_VALUE)
  public MessageResponse remove(@Valid @RequestBody RemoveServerRequest req, HttpServletRequest request) {
    rateLimiter.consumeOrThrow("remove", request);
    service.remove(req);
    return new MessageResponse("Server removed");
  }
}
