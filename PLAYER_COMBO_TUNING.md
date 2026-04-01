# Player Combo Tuning

This file captures the intended feel for the current 3-hit sword combo.

## Target Feel

- Hit 1: fast opener, light forward step, easy confirm into hit 2
- Hit 2: slightly heavier follow-up, still responsive
- Hit 3: strong finisher, biggest forward push, more commitment

## Current Suggested Runtime Timing

- `Attack1`
  - `AnimationDuration`: `0.90`
  - `LungeStartTime`: `0.08`
  - `LungeEndTime`: `0.24`
  - `ComboBufferOpenTime`: `0.34`
  - `ComboBufferCloseTime`: `0.78`
- `Attack2`
  - `AnimationDuration`: `1.00`
  - `LungeStartTime`: `0.11`
  - `LungeEndTime`: `0.30`
  - `ComboBufferOpenTime`: `0.40`
  - `ComboBufferCloseTime`: `0.86`
- `Attack3`
  - `AnimationDuration`: `1.12`
  - `LungeStartTime`: `0.16`
  - `LungeEndTime`: `0.42`

## Recommended Animation Events

For each sword clip, the event order should be:

1. `PlayCurrentAttackVfx()` slightly before impact
2. `OpenCurrentAttackDamageWindow()` exactly on impact
3. `CloseAttackDamageWindow()` shortly after impact
4. `CompleteCurrentAttackAnimation()` near the true recovery end

If you prefer one event on impact, use:

1. `PlayCurrentAttackVfxAndOpenDamageWindow()`
2. `CloseAttackDamageWindow()`
3. `CompleteCurrentAttackAnimation()`

## Practical Placement Guide

- `Attack1`
  - impact around `0.16 - 0.22`
  - damage window close around `0.28 - 0.34`
  - complete around `0.82 - 0.90`
- `Attack2`
  - impact around `0.22 - 0.30`
  - damage window close around `0.34 - 0.42`
  - complete around `0.92 - 1.00`
- `Attack3`
  - impact around `0.28 - 0.38`
  - damage window close around `0.42 - 0.52`
  - complete around `1.04 - 1.12`

## Notes

- If the combo feels delayed, move `CompleteCurrentAttackAnimation()` earlier.
- If hits feel fake, move `OpenCurrentAttackDamageWindow()` closer to the actual blade contact frame.
- If chaining feels strict, widen the combo window slightly before changing damage or speed.
