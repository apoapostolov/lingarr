# Subtitle housekeeping

Weekly Hangfire job `SubtitleMaintenanceJob` prepares sidecars so automation
can see them. It is not a nightly full-library read.

## Name standardization

Matches orphan sidecars in the same folder to the Radarr/Sonarr `file_name`,
then renames them to `{file_name}.{lang}[.sdh|.hi|.forced].srt`.

- Same folder only. Different `SxxExx` never mix.
- `eng` becomes `en`, `bul` becomes `bg`.
- Destination exists → skip.
- Clears `media_hash` so the next translation cycle re-evaluates the item.

## Embedded extract

Optional. Only runs when no matching English sidecar exists.

- `ffprobe` headers, then `ffmpeg` extract of one English **text** track.
- Skips PGS / VobSub / DVB image subs.
- Prefers dialogue over signs, and a non-SDH track when both exist.
- Capped by `subtitle_extract_max_per_run` (default 80).
- Stops after three I/O errors so a sick USB DAS does not get hammered.

Requires `ffmpeg` in the image.

## Media servers

After changes, the job asks Jellyfin (`/Library/Media/Updated`) and Plex
(item refresh) to re-read the folder. Missing URL or token → rename/extract
still happen; the existing host refresh cron can catch up.

Settings / env:

| Key / env | Purpose |
|-----------|---------|
| `subtitle_naming_enabled` / `SUBTITLE_NAMING_ENABLED` | Default true |
| `subtitle_extract_enabled` / `SUBTITLE_EXTRACT_ENABLED` | Default false |
| `subtitle_maintenance_schedule` | Default `0 3 * * 0` (Sunday 03:00 UTC) |
| `PLEX_URL`, `PLEX_TOKEN` or `PLEX_TOKEN_FILE` | Optional |
| `JELLYFIN_URL`, `JELLYFIN_API_KEY` or `JELLYFIN_TOKEN_FILE` | Optional |

Trigger from **Settings → Schedule → SubtitleMaintenanceJob → Run**.
