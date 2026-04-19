package net.dl2x.dvlobby.service;

import net.dl2x.dvlobby.api.dto.AddServerRequest;
import net.dl2x.dvlobby.api.dto.ModDto;
import net.dl2x.dvlobby.api.dto.UpdateServerRequest;
import net.dl2x.dvlobby.config.LobbyProperties;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.List;
import java.util.regex.Pattern;
import java.util.regex.PatternSyntaxException;

@Service
public class TextFilterService {

  private final List<Pattern> blockedPatterns;

  public TextFilterService(LobbyProperties properties) {
    List<Pattern> compiled = new ArrayList<>();
    for (String expression : properties.blockedTextRegex()) {
      if (expression == null || expression.isBlank()) {
        continue;
      }
      try {
        compiled.add(Pattern.compile(expression, Pattern.CASE_INSENSITIVE | Pattern.UNICODE_CASE));
      } catch (PatternSyntaxException ignored) {
        // Ignore invalid admin patterns instead of preventing startup.
      }
    }
    this.blockedPatterns = List.copyOf(compiled);
  }

  public void validateAddOrThrow(AddServerRequest request) {
    rejectIfBlocked(request.serverName(), "server_name");
    rejectIfBlocked(request.serverInfo(), "server_info");
    for (String player : request.onlinePlayers()) {
      rejectIfBlocked(player, "online_players");
    }
    for (ModDto mod : request.effectiveMods()) {
      if (mod == null) {
        continue;
      }
      rejectIfBlocked(mod.id(), "required_mods.id");
      rejectIfBlocked(mod.version(), "required_mods.version");
      rejectIfBlocked(mod.source(), "required_mods.source");
    }
  }

  public void validateUpdateOrThrow(UpdateServerRequest request) {
    for (String player : request.onlinePlayers()) {
      rejectIfBlocked(player, "online_players");
    }
  }

  private void rejectIfBlocked(String value, String field) {
    if (value == null || value.isBlank()) {
      return;
    }
    for (Pattern pattern : blockedPatterns) {
      if (pattern.matcher(value).find()) {
        throw new InvalidServerUpdateException("blocked text detected in " + field);
      }
    }
  }
}
