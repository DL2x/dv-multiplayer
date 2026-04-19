# Mod Integration Notes

## `hosting_type`

The API accepts exactly these values:

- `dedicated`
- `steam`
- `ip`
- `both`

## Add behavior

All hosting types can be sent to `/add*`, including `steam`.

Important behavior:

- `steam` is stored and updated like other server types
- `steam` is **never** returned by `/list`
- `ip`, `dedicated`, and `both` appear in `/list` only after `ready=true`
- new entries start with `ready=false`
- the API assigns `start_time` itself when the add request arrives

## Update behavior

Use `/update*` for all stored server types:

- `steam`
- `ip`
- `dedicated`
- `both`

Update payload additions:

- `ready` may be set to `true`
- `ready` cannot be toggled back to `false`
- `online_players` may be updated at any time

Recommended mod flow:

1. Send `/add*` as soon as the server process starts.
2. Wait until the world is fully loaded.
3. Send `/update*` with `ready=true`.
4. Continue sending `/update*` for `current_players`, `time_passed`, and `online_players`.

## Required mod payload compatibility

The API accepts both of these payload styles for required mods:

```json
{
  "id": "Multiplayer",
  "version": "0.1.13.14",
  "url": "https://example.com"
}
```

```json
{
  "Id": "Multiplayer",
  "Version": "0.1.13.14",
  "Url": "https://example.com"
}
```

It also accepts `source` / `Source` as aliases for `url`.

The API always responds with lowercase field names.

## List behavior

`GET /list` only returns:

- non-Steam servers
- servers with `ready=true`

Each list entry includes:

- `start_time`
- `ready`
- `online_players`

## Separate limits

The API enforces two independent limits:

- `public-server-limit` for `ip`, `dedicated`, and `both`
- `steam-server-limit` for `steam`

A full Steam bucket does not consume public slots, and a full public bucket does not consume Steam slots.

## Stats semantics

`GET /stats` exposes live and persistent values.

Live values:

- `current_servers`
- `current_players`
- `current_servers_by_type`

Persistent values stored in `stats.json`:

- `total_servers`
- `max_servers`
- `total_players`
- `max_players`
- `total_time_played_seconds`

The current implementation treats `total_players` as the sum of positive player-count increases over time, plus the initial player count seen on add.
`total_time_played_seconds` is accumulated from the server's last reported `time_passed` value when the entry is removed or cleaned up.

## Validation and rejection rules

Add requests are processed in this order:

1. request schema validation
2. business-rule validation
3. regex text filter validation
4. add rate limit
5. host probe
6. capacity check and insert

Requests can be rejected for:

- invalid `hosting_type`
- invalid sizes or missing required fields
- blocked regex matches in `server_name`, `server_info`, `online_players`, or `required_mods`
- probe failures
- reaching the per-bucket server limit
- oversized normalized entries
- add rate limiting

## Request logging

Every request is logged with:

- method
- path
- remote IP
- request body
- response status
- response body
- elapsed time

By default the log file is:

```text
logs/dv-lobby-api.log
```

## Auto-generated files

If missing, the server creates:

- `config.json`
- `stats.json`
