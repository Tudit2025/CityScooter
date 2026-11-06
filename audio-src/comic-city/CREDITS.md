# CityScooter audio credits — v1

Prepared 2026-11-06. All six external source works below are published under
[CC0 1.0 Universal](https://creativecommons.org/publicdomain/zero/1.0/).
Credit is retained for provenance even though CC0 does not require attribution.

| Source / author | Source page | Used for |
|---|---|---|
| City ambience — DCphotofilm | https://freesound.org/people/DCphotofilm/sounds/411604/ | Australian city ambience; recorded in Brisbane, not a Perth field recording |
| SkateRolling.wav — Lanterr | https://freesound.org/people/Lanterr/sounds/688733/ | Wheel contact loop; a skateboard recording adapted for the scooter |
| Car Horn — danlucaz / Daniel Lucas | https://freesound.org/people/danlucaz/sounds/517673/ | Vehicle horn 01 |
| Car Horn Honk.wav — DeVern | https://freesound.org/people/DeVern/sounds/349922/ | Vehicle horn 02 |
| Metal Scrape — magnuswaker | https://freesound.org/people/magnuswaker/sounds/530075/ | Two metal scrape variants and the scrape layer of crashes |
| Hot Roadway — MintoDog | https://opengameart.org/content/hot-roadway | 160 BPM music; 48–96 seconds of the lossless 160 BPM source, edited into a 32-bar loop |

Freesound downloads in this kit are public HQ **lossy MP3 previews**, not the
original WAV recordings named on those pages. Converting them to PCM does not
restore discarded detail. Music was obtained as lossless FLAC. Exact download
URLs, SHA-256 hashes and retrieval dates are in [sources.json](sources.json);
per-source metadata records are in `originals/*-source.txt`. Raw website HTML
is not archived because unrelated scripts can contain third-party credentials.

Edits: trim, high/low-pass filtering, mono conversion where appropriate, DC
removal, fades, loop joins, pitch variation, gain adjustment and PCM16 dithering.
The originals are retained outside Unity's Assets folder for reproducibility.

Wind, frame rattle, brake friction, vehicle-pass layers, landing impacts, jump
whoosh and UI tones were synthesized specifically for this project by
`tools/prepare-comic-audio.py`. Crash clips combine this synthesis with the
credited metal recording. These are offline generated WAV assets, with no
runtime synthesis dependency. No claim is made that these are real vehicle
recordings or a newly composed original music track.

Derivatives and source mappings: `Assets/_Project/Audio/ComicCity/catalog.json`.
Keep this file and `sources.json` with future asset deliveries.
