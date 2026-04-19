# Derail Valley Lobby Server (Spring Boot)

Spring Boot implementation of the Derail Valley lobby API for Steam, IP, Both, and Dedicated hosting.

## Included behavior

- `hosting_type`: `dedicated`, `steam`, `ip`, `both`
- `steam` servers are fully added, updated, timed out, and removed like all other servers
- `steam` servers are **not** returned by `/list`
- `/list` only returns `ready=true` non-Steam servers
- separate capacity limits for public servers and Steam servers
- add rate limit per remote IP
- ping before insert
- regex text filter for server text and player names
- request body size limits per endpoint
- maximum normalized entry size
- `start_time` is assigned by the API when the add request is accepted
- `ready` starts as `false` and can only move to `true`
- `online_players` is supported as an array of strings
- stale entry cleanup by timeout
- `config.json` is auto-generated on first start if it does not exist
- `stats.json` is auto-generated on first start if it does not exist
- all non-current stats survive restarts through `stats.json`
- request and response logging to `logs/dv-lobby-api.log`
- no test sources in the project

## Compatibility details

The API accepts both old and new mod payload styles for required mods:

- `Id` or `id`
- `Version` or `version`
- `Url`, `url`, `Source`, or `source`

Responses use lowercase JSON names:

- `id`
- `version`
- `url`

Unknown extra JSON fields are ignored, so mod-specific fields like `RuntimeType` and `TransportMode` do not break requests.

## Endpoints

- `GET /` -> status
- `GET /favicon.ico` -> `204 No Content`
- `GET /list` -> public ready server list, excluding Steam
- `GET /stats` -> current and persistent statistics
- `POST /add` and `POST /add_game_server`
- `POST /update` and `POST /update_game_server`
- `POST /remove` and `POST /remove_game_server`

## Ready flow

The intended flow is:

1. Send `add` when the server process starts.
2. The API validates text and probes the host before storing the entry.
3. The entry is stored with `ready=false`.
4. Send `update` with `ready=true` once loading has completed.
5. The server becomes visible in `/list`.

Once `ready` has been set to `true`, the API will not allow it to become `false` again.

## Configuration

On first start the server creates `config.json` next to the JAR if it does not already exist:

```json
{
  "port": 8080,
  "timeout-seconds": 120,
  "cleanup-interval-seconds": 60,
  "public-server-limit": 100,
  "steam-server-limit": 100,
  "blocked-text-regex": []
}
```

`blocked-text-regex` contains Java regular expressions. If any expression matches `server_name`, `server_info`, a player name, or a mod field, the request is rejected.

The server also creates `stats.json` automatically. That file stores:

- `total_servers`
- `max_servers`
- `total_players`
- `max_players`
- `total_time_played_seconds`

## Statistics

`GET /stats` returns:

- `current_servers`
- `total_servers`
- `max_servers`
- `current_players`
- `total_players`
- `max_players`
- `total_time_played_seconds`
- `current_servers_by_type`
- `public_server_limit`
- `steam_server_limit`

Current values are calculated from the live in-memory server list.
Persistent values are loaded from `stats.json` on startup and written back whenever they change.

## Build

```bash
mvn clean package
```

## Run

```bash
java -jar target/dv-lobby-server-0.2.0.jar
```

## Logs

Request logging is written to:

```text
logs/dv-lobby-api.log
```

Each request log entry contains:

- HTTP method
- request path
- remote IP
- request body
- response status
- response body
- duration in milliseconds

## Example add request

```json
{
  "address": "203.0.113.42:7777",
  "port": 7777,
  "hosting_type": "ip",
  "server_name": "Test Server",
  "password_protected": false,
  "game_mode": 0,
  "difficulty": 1,
  "time_passed": "00:15",
  "current_players": 0,
  "max_players": 8,
  "required_mods": [
    {
      "id": "Multiplayer",
      "version": "0.1.13.14",
      "url": "https://www.nexusmods.com/derailvalley/mods/1070"
    }
  ],
  "game_version": "99.0",
  "multiplayer_version": "0.1.0",
  "server_info": "Direct test",
  "online_players": []
}
```

## Example update request

```json
{
  "game_server_id": "abcd1234",
  "private_key": "0123456789abcdef0123456789abcdef",
  "current_players": 2,
  "time_passed": "00:45",
  "ready": true,
  "online_players": ["DriverA", "DriverB"]
}
```
