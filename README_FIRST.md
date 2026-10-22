# Polygon Racing VR — First Playable / v01

Built for the supplied Unity 6000.6.4f1 Universal 3D project.

## Install

1. Close Unity. Extract this ZIP somewhere temporary.
2. Copy its **Assets** and **Packages** folders into your **Polygon Racing VR project root**, beside ProjectSettings. Merge Assets; replace Packages/manifest.json when asked. Do not put Packages inside Assets. This manifest preserves all packages in your uploaded starter and adds OpenXR 1.16.1 and XR Management 4.5.0.
3. Reopen the project. Let Unity finish resolving packages and compiling. Internet access is needed for the added packages. If Unity asks to enable the Input System, accept (the supplied project already uses it).
4. In the top menu, choose **Polygon Racing > 1 - Create Forest Circuit**. Save any scene you want to keep when prompted. This creates and opens a new scene, saves the generated meshes/materials and a PlayerCar prefab, and adds ForestCircuit to the shared build scene list.
5. To add the second circuit, open the saved ForestCircuit scene and choose **Polygon Racing > 6 - Create Coastal Expressway Layout**. This makes a separate CoastalExpressway scene from the saved forest scene, replaces only its scenery and track profile, and enables both scenes in the shared build list. ForestCircuit remains available. Run this once before selecting Coastal Expressway in Play Mode or building for Quest. To refresh the Coastal scenery later without changing the route, car or handling, open CoastalExpressway and choose **Polygon Racing > 7 - Refresh Coastal Expressway Scenery**.
6. Press Play and click inside the Game view. Choose Race or Time Trial, then select Forest Circuit or Coastal Expressway.

The original SampleScene is left on disk. Re-running Create Forest Circuit regenerates the prototype and replaces its scene and PlayerCar prefab; it does not preserve manual edits to them. It also creates another set of generated mesh assets. Run it once, then edit the scene normally. Back up changes before regenerating.

## Desktop controls

| Control | Action |
| --- | --- |
| W / Up | Accelerate |
| S / Down / Space | Brake |
| A / D or Left / Right | Steer |
| Hold right mouse and move | Look around |
| C | Reset look |
| R | Restart at the start line; restart the current lap timer |
| P | Pause / resume |

## Quest setup and USB install

1. In Unity Hub, make sure this Unity version has **Android Build Support**, including SDK/NDK tools and OpenJDK installed.
2. In **File > Build Profiles**, select Android (add an Android profile if needed) and **Switch Platform**. Let Unity finish importing.
3. Choose **Polygon Racing > 2 - Configure Quest**. It applies Android IL2CPP + ARM64, OpenGLES3, minimum API 29, automatic target API, APK output, a separate application ID `com.bootleg714.polygonracingvr`, and an Android OpenXR loader. It selects the existing Mobile URP asset with 4x MSAA, render scale 1, HDR off and shadows off.
4. Under **Edit > Project Settings > XR Plug-in Management**, select the **Android** tab. Confirm **OpenXR** and **Initialize XR on Startup** are enabled.
5. Under Android **OpenXR**, confirm **Oculus Touch Controller Profile** is in Interaction Profiles and **Meta Quest Support** is enabled in Features. The setup command enables these if Unity has already populated its feature list. If they are missing or unchecked, add/enable them manually. Make sure the Meta Quest feature targets Quest 2 if a device list appears.
6. Open **XR Plug-in Management > Project Validation** for Android. Apply the required fixes. Choose **Polygon Racing > 3 - Check Quest Setup** for an additional local check. Fix any issues it reports.
7. Connect your Quest 2 by USB using your existing Harrier developer/USB debugging setup. Allow USB debugging in the headset if asked. Choose the Quest under **Run Device** in the Android Build Profile.
8. Confirm **Assets/PolygonRacing/Scenes/ForestCircuit.unity** and, if generated, **Assets/PolygonRacing/Scenes/CoastalExpressway.unity** are enabled build scenes. If the Android profile overrides the shared scene list, add both scenes to that profile too.
9. Choose **Build And Run**. Save the APK as `PolygonRacingVR_v01.apk`, outside Assets. This is a local development build; no Meta store upload is required.

Sit in your normal driving position before the app begins. The first tracked head pose centers you in the seat. Press **B** to recenter after adjusting your chair. Head movement remains tracked while driving or paused.

## Quest controls

| Control | Action |
| --- | --- |
| Left thumbstick, horizontal | Steer |
| Right trigger | Accelerate |
| Left trigger | Brake |
| A (right controller) | Restart car and current lap |
| B (right controller) | Recenter seated view |
| X (left controller) | Pause / resume |

On focus loss the car pauses; use X / P to resume. Controller models and virtual hands are not part of this first pass. Steering is via thumbstick; the visible steering wheel animates with your input.

## What is included

- Editable red-and-white open-wheel car made from simple generated meshes, with wedge nose, side pods, wings, exposed suspension and twelve-sided tires.
- Separate front steering pivots, spinning wheels, and moving steering wheel.
- Open cockpit with live speed, lap and time instruments.
- Flat forest circuit: approximately 793 metres, 18 metres wide, alternating curbs, runoff, guardrails, pit garages, start gantry and distant polygon hills.
- Coastal Expressway layout: approximately 3.95 km, a 22-metre-wide flat road, two roughly 670-metre straights, sweeping bends and one slower technical section. Low-poly scenery includes a visible ocean horizon, shoreline and beaches, palms, coastal rocks, distant islands, start/finish grandstands, and bridge and tunnel landmarks.
- The race menu lets you choose either circuit for Race or Time Trial. Opponents, race progress and drafting follow the selected route; Time Trial records are stored separately for each circuit.
- Sequential checkpoints, timed laps, best/last lap display on desktop; best times last for the current Play/app session.
- Arcade handling: approximately 151 km/h maximum, speed-sensitive steering, slower runoff and a simple barrier constraint. This is a kinematic prototype rather than a WheelCollider simulation. No reverse, opponent AI, damage or multiplayer yet.
- Synthesized engine sound; no external audio download required.
- Seated head tracking with before-render pose updates; no scripted camera shake or suspension bounce. The view turns with the car; comfort needs headset testing.
- One-time static scenery batching at startup. Materials use URP Simple Lit. Models and scenery are real 3D objects, not generated images.

## Edit the models

Open ForestCircuit. Expand **Polygon Racing VR - Prototype 01** in the Hierarchy.

- **Player car - editable polygon model** contains the car parts. Edit part positions/scales/materials in Edit Mode. The generated PlayerCar prefab is also in Assets/PolygonRacing/Generated.
- **Circuit - editable scenery** contains the road, trees, curbs and structures.
- The **PolygonDriver** component on the car exposes top speed, acceleration, braking and steering angle. A good first tuning pass is to change only one setting at a time.
- The seated eye reference is in PolygonDriver.Seat. Runtime recentering offsets the tracking space around it. Keep the camera and tracking hierarchy intact.

## First test checklist

Desktop: confirm the model is visible, steering turns the correct way, braking stops the car, R resets it, and a complete forward lap updates the counter. Driving a shortcut must not skip the sequential checkpoints.

Quest: confirm stereo view, correct seating scale, look/lean tracking, controls, readable dashboard and B recenter. Test driving gently first; then report handling, comfort, frame rate if available, and any Console errors. A 72 fps target is set, but actual headset refresh and performance are not verified here.

## Validation and limits

The supplied project settings and package manifest were inspected. The patch was checked for source structure, mesh winding, track continuity, checkpoint order and ZIP integrity. Unity, an Android toolchain and a Quest are not available in this workspace, so Unity compilation, visual appearance, APK building, controller input and hardware performance remain to be verified locally. If there is a compilation error, send the first complete red Console error before changing scripts.

If Polygon Racing is missing from the menu, package import or script compilation has not finished or has an error. If the APK opens flat instead of VR, check Android OpenXR and Initialize XR on Startup. Pink surfaces indicate an inactive/missing URP asset; rerun Configure Quest and check the active quality tier uses Mobile_RPAsset.

## Implementation references

Unity OpenXR: https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.16/manual/index.html
XR loader assignment API: https://docs.unity.cn/Packages/com.unity.xr.management@4.5/api/UnityEditor.XR.Management.Metadata.XRPackageMetadataStore.html
Unity XR input API: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/XR.InputDevice.TryGetFeatureValue.html
