package net.dl2x.dvlobby.service;

public class ServerCapacityReachedException extends RuntimeException {
  public ServerCapacityReachedException(String bucket, int maxServers) {
    super(bucket + " server limit reached (" + maxServers + ")");
  }
}
