# KER Rendezvous Tools v1.0.2

Bugfix release for the `WIND` long-horizon GOOD display.

## Fixed

- GOOD result rows no longer flicker in and out after the search reaches FOUND.
- Ordinary floating-point jitter no longer restarts the long-horizon search.
- The WIND panel now uses a fixed row layout, preventing repeated KER resizing.
- Legitimate target, orbit, launch-site, and expired-window changes still
  restart the search.

## Requirements

- KSP 1.12.5
- Kerbal Engineer Redux 1.1.9.5

Display-only: no steering, throttle, staging, SAS, or time-warp control.

License: GPL-3.0-or-later.
