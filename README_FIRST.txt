POLYGON RACING VR - ONE RIVAL CAR / v06

INSTALL (one new script)
1. Stop Play Mode in Unity.
2. Extract this ZIP OUTSIDE the Unity project (Desktop is fine).
3. Copy PolygonRival.cs into the actual project folder:
   Assets/PolygonRacing/Runtime/
   Put it beside PolygonDriver.cs and PolygonWorld.cs.
4. Return to Unity and allow compilation. Press Play.

Do not copy the extracted ZIP folder into Assets. Do not regenerate the circuit. No new packages, scene edits, player script replacements or XR changes are required.

The blue-and-white rival spawns automatically when a scene containing PolygonDriver loads. It reuses the player's visible meshes and creates its own blue material, without copying the camera, XR rig, player script, audio listener, or dashboard text. The player materials and saved cockpit sizes are not modified.

Behavior: starts about 12 metres ahead and in an adjacent lane. It waits for you to begin moving, accelerates up to about 126 km/h, and follows the full circuit. Your existing car can catch it on a clean lap. Wheels spin and front wheels steer. A small gold polygon helmet identifies the other driver. It shifts lanes near the player. This first pass has no car-to-car collision or race-position UI.

Controls: existing controls unchanged. R / right A resets both cars. P / left X pauses/resumes both; focus loss pauses the rival too. B continues to recenter your seated view.

QUEST: Once desktop Play works, Build And Run again. Save as PolygonRacingVR_v06.apk (same app ID). The old installed APK will not gain the rival until you rebuild/install. If you start below the car, sit normally and press B as before.

Removal: stop Play, remove PolygonRival.cs and its generated .meta from the project. The original scene and handling remain usable.

Checks completed here: package contains only one additive script; centerline arc-length interpolation checked at 72 steps/second over multiple laps; seam continuity and lane clearance checked; ZIP CRC checked. Unity compilation, visual layout and Quest performance still require local testing.
