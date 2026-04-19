package net.dl2x.dvlobby.service;

public class InvalidPrivateKeyException extends RuntimeException {
  public InvalidPrivateKeyException() {
    super("invalid private_key");
  }
}
