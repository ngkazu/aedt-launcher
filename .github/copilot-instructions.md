# Copilot instructions for aedt-launcher

## Code comments

- Write code comments in **English**.
- Add comments only where clarification is needed. Do not write comments that restate the code.

## Commit messages

- Write commit messages in **English**.
- Use a short summary on the first line, followed by a body explaining the reason for the change when needed.
- End with the Co-authored-by trailer:
  `Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>`

## Versioning and releases

- Use semantic version tags in the form `vMAJOR.MINOR.PATCH` (e.g. `v0.1.2`).
- Keep the tag in sync with `<Version>` in `aedt-launcher/aedt-launcher.csproj` (e.g. `0.1.2`).
- Create a release tag only on a commit that includes the matching `<Version>` change.
- Pushing a `v*` tag runs `.github/workflows/release.yml`, which builds the win-x64 version and attaches `aedt-launcher-<tag>.zip` to the GitHub Release. Do not attach the zip manually.
