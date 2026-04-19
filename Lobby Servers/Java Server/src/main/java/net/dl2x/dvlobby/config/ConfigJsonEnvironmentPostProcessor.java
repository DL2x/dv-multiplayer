package net.dl2x.dvlobby.config;

import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.env.EnvironmentPostProcessor;
import org.springframework.core.Ordered;
import org.springframework.core.env.ConfigurableEnvironment;
import org.springframework.core.env.MapPropertySource;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardOpenOption;
import java.util.HashMap;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

public class ConfigJsonEnvironmentPostProcessor implements EnvironmentPostProcessor, Ordered {

  private static final String EXTERNAL_CONFIG = "config.json";

  private final ObjectMapper mapper = new ObjectMapper();

  @Override
  public void postProcessEnvironment(ConfigurableEnvironment environment, SpringApplication application) {
    Map<String, Object> root = loadOrCreateExternalConfig();
    if (root == null || root.isEmpty()) {
      return;
    }

    Map<String, Object> props = new HashMap<>();
    map(root, props, "port", "server.port");
    map(root, props, "timeout-seconds", "lobby.timeout-seconds");
    map(root, props, "cleanup-interval-seconds", "lobby.cleanup-interval-seconds");
    map(root, props, "public-server-limit", "lobby.public-server-limit");
    map(root, props, "steam-server-limit", "lobby.steam-server-limit");
    map(root, props, "blocked-text-regex", "lobby.blocked-text-regex");

    environment.getPropertySources().addFirst(new MapPropertySource("dvConfigJson", props));
  }

  private void map(Map<String, Object> source, Map<String, Object> target, String sourceKey, String targetKey) {
    Object value = source.get(sourceKey);
    if (value != null) {
      target.put(targetKey, value);
    }
  }

  private Map<String, Object> loadOrCreateExternalConfig() {
    Path external = Path.of(EXTERNAL_CONFIG);

    try {
      if (Files.notExists(external)) {
        writeDefaultConfig(external);
      }

      if (Files.isRegularFile(external)) {
        try (InputStream in = Files.newInputStream(external)) {
          return mapper.readValue(in, new TypeReference<>() {});
        }
      }
    } catch (Exception ignored) {
      // Keep internal defaults if config.json cannot be created or parsed.
    }

    return null;
  }

  private void writeDefaultConfig(Path path) throws IOException {
    Path parent = path.getParent();
    if (parent != null) {
      Files.createDirectories(parent);
    }

    Map<String, Object> defaults = new LinkedHashMap<>();
    defaults.put("port", 8080);
    defaults.put("timeout-seconds", 120);
    defaults.put("cleanup-interval-seconds", 60);
    defaults.put("public-server-limit", 100);
    defaults.put("steam-server-limit", 100);
    defaults.put("blocked-text-regex", List.of());

    try (OutputStream out = Files.newOutputStream(
        path,
        StandardOpenOption.CREATE_NEW,
        StandardOpenOption.WRITE)) {
      mapper.writerWithDefaultPrettyPrinter().writeValue(out, defaults);
    }
  }

  @Override
  public int getOrder() {
    return Ordered.HIGHEST_PRECEDENCE;
  }
}
