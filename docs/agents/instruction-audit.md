# Instruction Audit

Last updated: 2026-05-01

## Contradictions

No hard contradictions were found in the supplied instructions for this repository.

The only scope mismatch is that some global preferences, such as TypeScript and package-manager defaults, do not apply to this C# Godot/.NET project. They were not copied into the project-local guidance except where useful as negative command guidance.

## Essentials Kept In Root

- One-sentence project description.
- Package manager status.
- SDK and build constraint.
- Note that there is no standalone typecheck command.
- Build command syntax for the non-standard `sts2.dll` requirement.
- Default validation command.
- Links to deeper guidance.

## Suggested `docs/` Structure

```text
docs/
  agents/
    workflow.md
    commands.md
    code-conventions.md
    testing.md
    release-packaging.md
    instruction-audit.md
```

## Flagged For Deletion Or Omission

- "Always strive for concise, simple solutions." Too vague to enforce as a project-specific rule.
- "If a problem can be solved in a simpler way, propose it." Useful preference, but redundant with normal agent behavior and the workflow guidance.
- TypeScript-specific guidance. Not relevant to this repository unless TypeScript is introduced later.
- Package-manager preference for pnpm or bun. Not relevant because this repository has no JavaScript package manager.
- "Use Agent CI only when the repo supports it..." Omitted because this repository has no Agent CI setup in the project files.
- Broad tech-stack preferences such as Tailwind, React, Convex, Clerk, and Vercel. Not relevant to a C# Godot mod.
- General reminders like "write clean code" or "prefer existing patterns" were either omitted or converted into repository-specific guidance.
