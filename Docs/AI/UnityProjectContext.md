# Unity project context

- Project: Assignment #2, Unity 6000.3.22f1, URP 17.3.0.
- Meta XR Core and Interaction OVR 207; OpenXR 1.18; Input System 1.20.
- Existing SampleScene contains directional light, global volume, and Meta BuildingBlock OVRCameraRig with eye/controller anchors.
- CoplayDev Unity MCP is connected through local HTTP port 8080.
- No first-party gameplay assemblies or locomotion scripts were found in Assets.
- Museum scope: educational sound museum. Initial pass only builds an enclosed hall and five instrument station placeholders. Actual exhibits, UI history panels, proximity events, controller interactions/locomotion, headset validation, and submission README remain future work.
- Editor builder: Assets/Museum/Editor/MuseumShellBuilder.cs; menu Tools/Museum of Sound/Build Basic Shell. Refuses to overwrite existing museum scene or root. Original SampleScene asset is preserved. New scene: Assets/Scenes/MuseumOfSound.unity.
- Use existing Meta XR framework for future interaction work. Do not add a second XR framework without a specific need.

- Interactive pass: five primitive instrument models; Meta OVR controller data sources plus controller GrabInteractor templates under existing rig. Six Grabbable objects, 14 sound surfaces, controller contact tips, 24 original WAV clips. Slow CharacterController locomotion and snap turning. Nearby instructions appear at each station. See MuseumControls.md for controls and validation scope.
