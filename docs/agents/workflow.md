# Workflow

Last updated: 2026-05-01

## Scope

- Keep project workflow in the closest `AGENTS.md`.
- Keep workstation runbooks outside this repo in `C:\Users\nguco\.agents\docs\`.
- Preserve user changes. Do not revert, overwrite, rename, or reorganize unrelated work unless explicitly asked.
- Keep changes small, cohesive, aligned with local patterns, and no broader than the task requires.

## Before Editing

- Identify the goal, constraints, done criteria, and verification path before making non-trivial changes.
- Inspect relevant local guidance plus `README.md`, project files, and task runners when useful.
- For vague requests, ask one concise clarifying question only when a reasonable default would be risky.
- When a design is under-specified, surface the key decision, recommend a default, and ask for confirmation before committing to the direction.

## During Work

- Prefer existing helpers, framework APIs, parsers, and conventions before adding new abstractions.
- Add abstractions only when they remove real duplication or complexity.
- Clean up temporary artifacts unless they remain useful records.
- If asked to do too much work at once, stop and say so clearly.

## Git

- Check `git status --short` before non-trivial edits and again before commit or push.
- Do not include unrelated changes in commits.
- Do not force-push or run destructive git commands unless explicitly requested.
