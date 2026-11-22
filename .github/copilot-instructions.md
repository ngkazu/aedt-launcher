# Copilot instructions for aedt-launcher

## Code comments

- Write code comments in **English**.
- Add comments only where clarification is needed. Do not write comments that restate the code.

## Commit messages

- Write commit messages in **English**.
- Use a short summary on the first line, followed by a body explaining the reason for the change when needed.

## Versioning and releases

- Use semantic version tags in the form `vMAJOR.MINOR.PATCH` (e.g. `v0.1.2`).
- Keep the tag in sync with `<Version>` in `aedt-launcher/aedt-launcher.csproj` (e.g. `0.1.2`).
- Create a release tag only on a commit that includes the matching `<Version>` change.
- Pushing a `v*` tag runs `.github/workflows/release.yml`, which builds the win-x64 version and attaches `aedt-launcher-<tag>.zip` to the GitHub Release. Do not attach the zip manually.

## Issues

- Write issue titles in **English**.
- Write the issue body in **Japanese**, and include a Japanese title in the body.
- Use the templates in `.github/ISSUE_TEMPLATE/`: `bug_report.yml` for bugs and `feature_request.yml` for enhancements.
- Apply only labels that already exist in the repository. Do not create new labels.
- When an issue came from `memo/improvement-items.md`, record the item number in the issue body.
- Create an issue only for bugs or work that needs tracking. Keep small notes and ideas in code comments or the memo.
- Close an issue through a commit or PR that includes `Fixes #<number>` (or `Closes #<number>`) in its message.
