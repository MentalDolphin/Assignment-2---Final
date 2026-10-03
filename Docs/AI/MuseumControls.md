# Museum controls and implementation

Open Assets/Scenes/MuseumOfSound.unity. Uses the existing Meta XR OVR rig and Interaction SDK packages; no new packages installed.

- Left thumbstick: slow head-relative walking; right thumbstick: 30-degree snap turns.
- Grip near an instrument/tool to pick it up. Release grip to drop it. Dropped objects return to their initial display after 15 seconds; objects below the floor return immediately.
- Piano: touch any of eight white keys with either controller's silver tip. Black keys are decorative in this prototype.
- Drums: pick up the two sticks and strike their tips against snare, tom, bass pad, or cymbal.
- Guitar: grip the guitar, sweep the other controller tip across the sound hole/string area. Trigger on the holding controller also strums. A/B (right) or X/Y (left) cycles four chords.
- Violin: grip the violin with one hand and the bow with the other. Move bow hair across the bridge/string area to play. A/B or X/Y cycles four pitches on the holding violin controller.
- Flute: grip, then hold the holding controller's index trigger to play. A/B or X/Y cycles four pitches. Releasing the trigger stops sound. Breath is simulated; no microphone input is needed.
- Nearby instruction text appears within 3.2 metres of each station.

Audio is original synthesized prototype audio rather than recordings of real instruments. There are 24 mono clips with spatial attenuation. Models use Unity primitive meshes and shared URP materials.

Runtime components reside in Assets/Museum/Scripts. Editor construction and test helpers reside in Assets/Museum/Editor. The instrument installer refuses to overwrite an existing collection. Physics collider refinement is a separate menu action.

Tests: MuseumInteractionTests.ExhibitAudioGrabPhysicsAndReset in the Unity PlayMode Test Runner. Covers six grab-ready objects, 14 playing surfaces, inactive/wrong-tool filtering, note playback, repeated-contact cooldown, simulated Meta hover/select/move/release, held-instrument rules, actual drumstick trigger contact, and dropped-object recovery. Hardware input and headset rendering still require a connected VR device.

Historical information panels, polished instrument assets/samples, and a final headset build remain later assignment work.

Validation status: initial Unity compilation and scene validation succeeded. Simulated tests verified key playback/filtering/debounce, Meta hover/select/move/release, held guitar/violin playback, and physical drumstick trigger contact in earlier runs. Subsequent test refinement removed Rigidbody interpolation for transform-driven grabbing and corrected the drop simulation to update both Rigidbody and Transform. Saved scene has all six tools/instruments kinematic with interpolation disabled. The latest complete test result could not be retrieved because the Unity MCP connection repeatedly disconnected during reload; do not treat the previous XML report as a passing run. Real controller input, final reset check, and headset rendering remain unverified. Re-run the PlayMode test after Unity is responsive.
