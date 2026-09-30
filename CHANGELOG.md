# Changelog

## [1.1.1](https://github.com/purcilas/ClearSkies/releases/tag/v1.1.1) — 2026-09-30

### Fixed

- Fixed a serious manual-folder cleanup defect: earlier versions could recursively delete unrelated files in a selected folder, including an MSFS package root or a saved Documents path.
- Manual cleanup now targets only `ROLLINGCACHE.CCC`. Package-root selections resolve to `LocalCache` when that file exists.
- Invalid saved manual paths are ignored in interactive and scheduled cleanup.
- Cleanup rejects unregistered or modified targets and skips symbolic links and directory junctions.
- Automatic rolling-cache cleanup preserves `MANUALCACHE.CCC` and other unrelated `.ccc` files.
- Locked or inaccessible files are skipped instead of being queued for deletion after reboot.

### Added

- Visible cache paths on cache cards and in the cleanup confirmation.
- An automatic NVIDIA App guidance prompt before interactive NVIDIA DirectX shader-cache cleanup.
- Instructions to use NVIDIA App's **Clear cache**, followed by **Compile now**, and an option to open NVIDIA App from its standard installation location.
- Fourteen safety regression checks using temporary fixtures.

### Upgrade notes

- Updating is strongly recommended before using manual-folder cleanup. Close ClearSkies and replace the old executable with the v1.1.1 download.
- This update does not cancel deletion requests already queued by an older version.
- MSFS rolling-cache size is not automatically restored after deletion; restore your preferred size in the simulator if it resets.
- NVIDIA App features depend on the installed app and driver versions. ClearSkies does not invoke NVIDIA's internal cleanup or compilation automatically. Scheduled cleanup remains noninteractive.

## Earlier releases

- [v1.1.0](https://github.com/purcilas/ClearSkies/releases/tag/v1.1.0)
- [v1.0.0](https://github.com/purcilas/ClearSkies/releases/tag/v1.0.0)
