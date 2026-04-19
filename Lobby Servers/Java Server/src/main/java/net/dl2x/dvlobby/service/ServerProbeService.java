package net.dl2x.dvlobby.service;

import net.dl2x.dvlobby.api.dto.AddServerRequest;
import net.dl2x.dvlobby.config.LobbyProperties;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

import java.io.IOException;
import java.net.InetAddress;
import java.net.UnknownHostException;
import java.util.LinkedHashSet;
import java.util.Set;

@Service
public class ServerProbeService {

  private static final Logger LOGGER = LoggerFactory.getLogger(ServerProbeService.class);

  private final LobbyProperties properties;

  public ServerProbeService(LobbyProperties properties) {
    this.properties = properties;
  }

  public void probeOrThrow(AddServerRequest request) {
    Set<String> candidates = extractCandidates(request);
    if (candidates.isEmpty()) {
      throw new ServerProbeFailedException("No reachable host candidate found in request");
    }

    for (String candidate : candidates) {
      if (tryReachable(candidate)) {
        LOGGER.info("Server probe succeeded for hostingType={} candidate={}", request.hostingType(), candidate);
        return;
      }

      LOGGER.warn("Server probe failed for hostingType={} candidate={}", request.hostingType(), candidate);
    }

    throw new ServerProbeFailedException("Server host did not answer ping before registration");
  }

  private Set<String> extractCandidates(AddServerRequest request) {
    LinkedHashSet<String> out = new LinkedHashSet<>();
    maybeAdd(out, request.ipv4());
    maybeAdd(out, request.ipv6());

    if (request.address() != null && !request.address().isBlank()) {
      String address = request.address().trim();
      if (address.startsWith("[") && address.contains("]:")) {
        int end = address.indexOf(']');
        if (end > 1) {
          out.add(address.substring(1, end));
          return out;
        }
      }

      long colonCount = address.chars().filter(ch -> ch == ':').count();
      if (colonCount == 1) {
        int idx = address.lastIndexOf(':');
        if (idx > 0) {
          out.add(address.substring(0, idx));
          return out;
        }
      }

      out.add(address);
    }

    return out;
  }

  private void maybeAdd(Set<String> out, String value) {
    if (value != null && !value.isBlank()) {
      out.add(value.trim());
    }
  }

  private boolean tryReachable(String host) {
    try {
      InetAddress[] addresses = InetAddress.getAllByName(host);
      for (InetAddress address : addresses) {
        if (address.isReachable(properties.pingTimeoutMs())) {
          return true;
        }
      }
      return false;
    } catch (UnknownHostException e) {
      return false;
    } catch (IOException e) {
      return false;
    }
  }
}
