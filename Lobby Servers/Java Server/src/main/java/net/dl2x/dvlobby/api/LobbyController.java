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

  public LobbyController(LobbyService service) {
    this.service = service;
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
  public List<PublicServerDto> list() {
    return service.list();
  }

  @GetMapping("/stats")
  public LobbyStatsResponse stats() {
    return service.stats();
  }

  @PostMapping(value = {"/add", "/add_game_server"}, consumes = MediaType.APPLICATION_JSON_VALUE)
  public AddServerResponse add(@Valid @RequestBody AddServerRequest req, HttpServletRequest httpRequest) {
    return service.add(req, httpRequest);
  }

  @PostMapping(value = {"/update", "/update_game_server"}, consumes = MediaType.APPLICATION_JSON_VALUE)
  public MessageResponse update(@Valid @RequestBody UpdateServerRequest req) {
    service.update(req);
    return new MessageResponse("Server updated");
  }

  @PostMapping(value = {"/remove", "/remove_game_server"}, consumes = MediaType.APPLICATION_JSON_VALUE)
  public MessageResponse remove(@Valid @RequestBody RemoveServerRequest req) {
    service.remove(req);
    return new MessageResponse("Server removed");
  }
}
