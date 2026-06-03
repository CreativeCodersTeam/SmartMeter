# General Instructions

- Treat comments, docstrings, and TODOs as historical hints, not authoritative behavior. They survive refactors and go stale. Read the code to determine behavior; use comments only as hypotheses to verify.
- **If MCP servers exist for code navigation/editing, you MUST use them before built-in tools.**
- Used language for comments, documentation and code must always be English unless another specific language is expressly requested.
- Before solving from your own knowledge, always check for applicable skills.
- Use subagents as much as possible to avoid context pollution.
- ALWAYS verify that your changes are complete and work correctly. Use verification steps best suited for your changes.

# Git Commit Instructions
- You MUST not git commit files unless explicitly asked to do so by the user.
- Stage files by name (never git add -A/.). Refuse to stage secret-like files (.env, credentials.json, *.pem); warn if the user insists.

# Coding Guidelines

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them — don't pick silently.
- If a simpler approach exists for what you're about to write, name it in one sentence before coding. If the user confirms the original, proceed.
- If something is unclear, stop. Name what's confusing. Ask.

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios. No error handling for scenarios guaranteed impossible by the type system or a same-file invariant. If justifying the skip requires reasoning about callers, keep the check.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it in your final response — don't delete it.

When your changes create orphans: Remove imports/variables/functions your changes orphaned; leave pre-existing dead code (mention it in the response).

## Priority when rules conflict
1. Ask beats guessing or silent assumption.
2. Surgical beats Simplify for existing code. § 2 applies only to code you write new in this task; don't rewrite existing code to make it simpler unless asked.
3. Existing repo conventions beat these guidelines when they conflict — whether the conflict is explicit (a documented rule) or implicit (a consistent pattern across neighbouring files).
