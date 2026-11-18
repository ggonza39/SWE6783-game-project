# Campaign menu and stamina retest

## Current behavior

- The menu shows Level 1 Facility Breach and Level 2 Crimson Depths side by side.
- A fresh profile starts at Level 1. New Game always starts at Level 1, even when the editor was opened on Level 2.
- Continue explicitly names and highlights its saved level. Level 2 also shows the secured checkpoint.
- Level 2 is locked until Level 1 is cleared in the selected difficulty.
- Easy and Hard keep separate saves. The selected mode, hover, and pressed button states are distinct.
- A completed campaign offers New Game and Replay Level 2. It does not claim to continue an unfinished campaign.
- New Game asks before erasing campaign progress and mission records in both difficulties. Cancel and Enter on that dialog retain the save. Display and control settings are retained.
- Level 1 has 10 seconds of sprint and Level 2 has 20 seconds, equal in Easy and Hard. There is no recharge during an attempt.
- Death and Replay restore full health and the level's full sprint reserve. Checkpoints retain spent sprint during a live attempt.
- Schema 5 adds the extra 10 seconds once to older Level 2 saves. It preserves checkpoints, records, settings, and already-spent seconds. Reloading does not add it again.

## Unity acceptance

1. Close the old editor, apply the updater, then open src in Unity 6000.2.2f1 and Assets/Scenes/UXFacilityExpanded.unity.
2. Confirm there are no Console errors. Check the two cards and the selected difficulty.
3. Hover and press the action buttons and both difficulty buttons. Confirm the selected difficulty stays marked after release.
4. With existing progress, click New Game then Cancel. Confirm the same level and checkpoint are still highlighted.
5. Confirm New Game when ready to erase the old campaign. Verify Level 1 is marked as the start and Level 2 is locked.
6. Complete Level 1 and confirm it automatically enters Level 2. Verify 10 seconds in Level 1 and 20 seconds on entering Level 2.
7. Spend sprint in Level 2, secure a checkpoint, and return to the menu. Verify Continue Level 2, the correct checkpoint, and the preserved remaining reserve.
8. Die and Replay. Verify 100 health, 20 seconds, and the same secured checkpoint.
9. Switch difficulty in the menu and verify that mode's own progress and resume level.
10. Finish Level 2. Verify the campaign ends with New Game and Replay Level 2 available.
11. Test pause, settings, both restart outcomes, camera movement, and the exported executable.

The automated checks compile the real C# against Unity API stand-ins. They cover saves, resource/controller behavior, UI events and bounds, and scene-load requests; they do not replace Unity physics/rendering or an exported-build playtest.

## Publishing and team testing

PRs 24, 25, and 26 were merged into main. The audited main tree matches the reviewed two-level campaign. Its build settings register UXFacilityExpanded first and UXCrimsonDepths second.

The repository's listed public Vercel URL returned HTTP 404 during this check. A successful Vercel source deployment does not establish that Unity exported a new playable build. No compiled WebGL index.html, wasm, or data files were present in the audited tracked tree.

After this local update is playtested and merged:
- In Unity Build Profiles, verify that the active profile includes both campaign scenes with Level 1 first. The Pixel Rebellion Campaign Register Campaign For Builds command registers the shared list.
- Build the intended Windows or WebGL player from the updated source.
- For WebGL, publish the compiled export containing index.html, Build, and TemplateData using the team's deployment setup.
- Test the exact public link from a fresh browser profile. Verify both level cards, both difficulties, the transition, and Continue before sending that link to survey participants.
- Each participant should use their own device/browser profile for a separate anonymous player identifier. New Game clears game progress; it does not create a different participant identifier on a shared browser profile.

