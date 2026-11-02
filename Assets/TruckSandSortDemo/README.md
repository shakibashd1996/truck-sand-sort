# Truck & Sand Sort - 60-Second Scripted Demo

## Open and run

1. Open `Assets/TruckSandSortDemo/Scenes/TruckSandSort_Demo.unity`.
2. Set the Unity Game view to a 9:16 aspect (recommended preview: 540 x 960).
3. Press Play. No input is required.
4. The deterministic sequence runs for exactly 60 seconds, holds for 3 seconds, then automatically replays from the same initial state.

If the scene is missing or needs to be regenerated, use **Tools > Truck Sand Sort > Build Demo Scene**. Use **Tools > Truck Sand Sort > Set Portrait Game View** to set the standalone-player defaults to 1080 x 1920.

## Timeline

- 00-05: establishing view and ambient conveyor motion
- 05-10: blue-truck selection and unobstructed route
- 10-16: blue sand unload
- 16-23: yellow truck route and unload
- 23-30: red truck route and unload
- 30-36: mixed-color hopper discharge and conveyor transport
- 36-44: matching bucket streams and staged capacity updates
- 44-50: completed red bucket celebration, close, exit, and queue advance
- 50-55: blocked purple truck collision feedback and exact return
- 55-60: continuing sand flow and final level-complete payoff

## Implementation notes

- Unity 6.3 LTS / URP 17.3.
- Source models are preserved. Supplied hopper, conveyor, bucket, cone, plant, and lamp prefabs are instantiated by the runtime presentation.
- Trucks are deliberately assembled from simple rounded toy-like forms so they can be recolored, selected, bounced, routed, and unloaded without destructive edits to the source truck FBX.
- The sand effect combines bounded mesh particles, visible hopper beads, animated conveyor beads, and bucket fill meshes. Blue, yellow, and red remain visually distinct.
- A single deterministic clock triggers all presentation beats. General pathfinding, procedural puzzle generation, granular rigidbody sand, and production game-state architecture are intentionally out of scope.

## Recording

Unity Recorder is not installed in the supplied project. To capture the submission:

1. Install Unity Recorder from Package Manager only if allowed for the submission environment.
2. Create a Game View recording at 1080 x 1920, 30 FPS, for 60 seconds.
3. Start recording on the first rendered frame and stop at 60 seconds, before the 3-second hold/replay interval.

Without Recorder, use a lossless screen capture of the portrait Game view at 30 FPS. The runtime itself does not claim to render an MP4.

## Validation checklist

- Scene starts automatically and replays deterministically.
- Game view uses 9:16 portrait framing.
- No compile errors or missing references in Console.
- All three unloading colors are visible in the hopper and on the conveyor.
- Bucket values change coherently from 0/3 to 3/3.
- Red bucket completes, celebrates, exits, and reveals the queued bucket.
- Purple blocked truck returns to its exact start position.
- Original imported asset files remain unchanged.

## Remaining production work

- Optional Unity Recorder package/preset and a final 1080 x 1920 capture.
- Device-performance pass on the actual target phone.
- Art-directable custom meshes for fully rounded UI panels and truck bodywork if more polish time is available.
- Audio and haptics are intentionally omitted from this visual task demo.
