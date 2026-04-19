package net.dl2x.dvlobby.service;

public class ServerNotFoundException extends RuntimeException {
  public ServerNotFoundException(String id) {
    super("server not found: " + id);
  }
}
