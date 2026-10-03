# Museum visitor upgrade

Scene: Assets/Scenes/MuseumOfSound.unity. Unity 6000.3.22f1, Meta XR 207, URP 17.3. No package versions or project rendering settings changed.

## Visitor controls

Push the RIGHT thumbstick forward and aim the right controller at a numbered gold floor station. A green beam means the station is selected; an orange beam means no valid destination. Release the stick to teleport and face that exhibit. Grip cancels; pushing the stick backward also cancels. A right-hand grab prevents starting teleport aim. LEFT stick walks; RIGHT stick sideways snap-turns. Walking and snap-turning pause during teleport aim.

There is one station in front of each of the five exhibits. Teleport placement compensates for the room-scale head offset, preserves tracked head height, and rejects occupied landing capsules.

| Exhibit | Interaction |
| --- | --- |
| Piano | Touch the eight white keys with either controller tip. |
| Drums | Grip the two drumsticks and strike the drum heads or cymbal. |
| Guitar | Grip the guitar; sweep the other controller across the strings. Trigger also strums. A/B or X/Y selects chords. |
| Violin | Hold the violin and bow in separate hands and bow across the strings. A/B or X/Y selects notes. |
| Flute | Grip the flute and hold trigger to play; release trigger to stop. A/B or X/Y selects notes. |

Dropped objects return to their display after 15 seconds or immediately if below the floor.

## Text changes

All text remains attached to museum objects, outside the XR rig hierarchy. The previous labels used the legacy font material. The headset test log confirms SinglePassInstanced rendering, making an unsupported legacy shader a likely explanation for the reported one-eye/head-motion artifact. The replacement URP font shader explicitly initializes the instance and stereo eye, transforms geometry with the appropriate eye matrix, and carries stereo state to the fragment stage. Font atlas rebuilds update the shared material. It does not billboard or follow the camera.

Five fixed plaques now explain sound production and basic instrument history. Interaction instructions were moved above the models and wrapped to prevent obstruction.

## Validation evidence

- Unity Editor imported and compiled the changed runtime/editor/test scripts successfully. No new errors appeared in Console; existing Unity AI NoSubscription and AgentBridge port errors remain.
- Play Mode test MuseumInteractionTests.ExhibitAudioGrabPhysicsAndReset: **1 passed, 0 failed, 0 skipped**, duration 1.5432 seconds, 2026-10-03 03:37:09–03:37:11 UTC. XML: Docs/AI/MuseumTestResults.xml.
- All six objects: Meta grab selection, transformed movement, release, home reset, Rigidbody/interactable references.
- Piano sound, repeated-contact debounce, wrong/inactive tool rejection; guitar held-only strum and trigger/chord changes; violin held-only bow sound; flute trigger/note changes and release-to-stop.
- Physical drumstick trigger contact generated snare playback.
- All five teleport landing positions and facing directions passed; null and blocked destinations were rejected. Labels remained fixed after moving and rotating the XR rig.
- Scene validation: **0 missing scripts, 0 broken prefabs, 0 total issues**.
- Visual previews inspected for all five exhibit plaques, then instruction placement refined. Gold station preview inspected.
- The test logged Meta Link Quest 3, OpenXR, 72 Hz and SinglePassInstanced. Inputs were simulated; this does not establish actual thumbstick/grip mappings, physical reach, comfort, or both-eye appearance. OpenXR action-set/haptics warnings appeared during test scene initialization. No player/Android build was requested or produced.
- MuseumOfSound restored in Edit mode after tests. Pre-upgrade scene snapshot: Docs/AI/MuseumBeforeVisitorUpgrade.unity.

## Headset check

Enter Play in MuseumOfSound. Look at a title and history plaque with each eye separately, then turn and move your head: both eyes should see the same stationary sign. Push RIGHT stick forward, aim at each gold station and release. Confirm the green beam, exhibit-facing arrival, cancellation with grip, and station reach. Pick up both sticks and all other portable instruments, exercise the controls above, and verify sounds and dropped-object recovery. These are the remaining hardware acceptance checks.

## History sources

Descriptions are original paraphrases of Yamaha's Musical Instrument Guide:

- Piano: https://www.yamaha.com/en/musical_instrument_guide/piano/structure/
- Drums: https://www.yamaha.com/en/musical_instrument_guide/drums/structure/
- Guitar: https://www.yamaha.com/en/musical_instrument_guide/classical_guitar/structure/
- Violin: https://www.yamaha.com/en/musical_instrument_guide/violin/structure/
- Flute: https://www.yamaha.com/en/musical_instrument_guide/flute/structure/

## Main changed files

Assets/Museum/Scripts/MuseumTeleportStation.cs and MuseumTeleportation.cs define the destinations and controller aiming/placement. MuseumLocomotion.cs prevents walking/turning conflicts during aim. MuseumWorldText.cs and Shaders/WorldTextStereo.shader render fixed text in stereo. MuseumInstrument.cs exposes the same playing-input logic to controller input and integration tests. Editor/MuseumVisitorUpgrade.cs installs/refines the scene assets. MuseumTestReport.cs retains reporting across domain reloads; MuseumInteractionTests.cs covers the interactive scene.
