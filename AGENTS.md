# Release requirements

- Always keep the published version at v1.0.0, replacing its portable ZIP only after validation.
- Before publishing any candidate, test the exact ZIP on a newly provisioned Windows VM or a separately verified clean Windows computer. A new folder on the development PC does not satisfy this requirement.
- The clean environment must have no JONSBO vendor installation, Studio settings/themes/cache, Studio scheduled task or legacy Run entry. Test the shipped EXE, first launch, startup status, bundled dependencies, theme import, gallery previews and video preview. Keep logs and screenshots.
- Use the `Fresh Windows install` GitHub Actions workflow and `Publish-Release.ps1` gate. A passing run must match both the candidate ZIP SHA256 and current source commit. If the clean-machine run fails or is unavailable, hold publication and report the reason; do not substitute local results or bypass the gate.
- A hosted Windows Server VM test does not prove Windows 10/11 USB driver installation or physical screen playback. State those limits accurately and use connected-device tests when transport code changes.
- Preserve the untracked `Themes/` folder and all personal `Data/` content. Never include them in release packages.
