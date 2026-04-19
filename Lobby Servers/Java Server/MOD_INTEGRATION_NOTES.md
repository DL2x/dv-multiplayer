# Mod Integration Notes

## Request schema changes

### Add request

Use `address` as the only host field.
Do not send `ipv4` or `ipv6` anymore.

Add payload fields:

- `address`
- `port`
- `hosting_type`
- `private`
- `server_name`
- `password_protected`
- `game_mode`
- `difficulty`
- `time_passed`
- `current_players` optional when `online_players` is present
- `max_players`
- `required_mods`
- `game_version`
- `multiplayer_version`
- `server_info`
- `online_players`

`current_players` is derived from `online_players.length` when the player array is not empty.

### Update request

Continue sending:

- `game_server_id`
- `private_key`
- `time_passed`
- `ready`
- `online_players`
- `current_players` optional when `online_players` is present

`ready` may only transition from `false` to `true`.

## List semantics

`GET /list` only returns servers that satisfy all of the following:

- `ready == true`
- `hosting_type != steam`
- `private == false`

## Rate limiting

Each endpoint has its own per-IP minimum interval configured in `config.json` under `rate-limit-seconds`.

Example:

```json
{
  "rate-limit-seconds": {
    "list": 1,
    "stats": 2,
    "add": 5,
    "update": 1,
    "remove": 1
  }
}
```

## Compatibility

The API still accepts old mod field names for required mods:

- `Id` / `id`
- `Version` / `version`
- `Url` / `url` / `Source` / `source`
