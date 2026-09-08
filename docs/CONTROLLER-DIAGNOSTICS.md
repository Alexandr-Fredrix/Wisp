# Controller investigation — 2026-09-08

Local setup: Hollow Knight 1.5.78.11833 / API v77 / Wisp alpha.3, Flydigi APEX5.

## Observations

- Player.log contains 34 activations of Unknown Device and 33 of Xbox 360 Controller in the captured session. The two names repeatedly alternate. It reports both Flydigi APEX5 Wireless and XBOX 360 For Windows connections.
- Steam's controller log uses the gamepad joystick template for app 367520. Earlier entries have xinput=false; subsequent entries have xinput=true. This log alone does not establish which component created the second device.
- Read-only Windows device inventory reports one Xbox 360 controller with status OK. This is enumeration, not a physical latency test.
- Game preferences at inspection: NativeInput=0, XInput=0. These were not changed.
- No Wisp exception was found in the captured logs.

## Code inspection

Inspection of the installed game assembly shows that InputHandler.ControllerActivated calls SetActiveGamepadType, which remaps controller buttons, rebuilds the mappable bindings and starts SetupGamepadUIInputActions. Repeated device switching therefore repeatedly reconfigures input; it is a strong candidate for the reported erratic response.

Wisp only reads InputManager.ActiveDevice; the installed getter returns a field and has no switching side effect. Wisp does not attach controllers, set ActiveDevice, configure Steam Input or change native input preferences. While its guide is open it temporarily changes shared UI navigation/mouse flags, restoring them on close. A physical comparison with Wisp disabled remains necessary to rule out all mod interactions.

The guide's .2-second repeat interval applies only to held menu navigation while open, not gameplay. HUD code unnecessarily queried journal completion during each IMGUI event. It now uses the one-second refresh snapshot and draws only on Repaint, reducing avoidable gameplay work without claiming this fixes the device conflict.

## Pending controlled test

After normal save/exit, compare the same controller in the main menu with Wisp enabled and disabled, keeping Steam/game input settings unchanged. Record new controller-switch counts and physical button response. Then test one input-backend setting at a time with a full restart, preserving the original settings above. Do not alter drivers or main saves to perform this comparison.

References: [Steam Input double-input guidance](https://partner.steamgames.com/doc/features/steam_controller/getting_started_for_devs), [Team Cherry controller troubleshooting](https://www.hollowknight.com/help).
