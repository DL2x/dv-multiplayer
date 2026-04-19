package net.dl2x.dvlobby.service;

public class ServerEntryTooLargeException extends RuntimeException {
  public ServerEntryTooLargeException(int limitBytes) {
    super("normalized server entry exceeds " + limitBytes + " bytes");
  }
}
