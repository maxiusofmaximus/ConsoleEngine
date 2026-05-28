Read `docs/EDITOR_PLAN.md` and find the next item to implement.

Rules:
1. Scan Phase A first. Find the first unchecked `- [ ]` item.
2. If Phase A is fully checked, move to Phase B, then C, then D.
3. Report the item, which module it belongs to, and what "done" looks like technically.

Then look at the relevant source files in `src/ConsoleEngine.Editor/` to understand
the current state and what gaps exist.

Output format:
- **Next task**: one sentence
- **Module**: which EDITOR_PLAN.md module number and name
- **Phase**: A / B / C / D
- **Technical scope**: what files need to change, which classes are involved, what
  new abstractions (if any) are needed
- **SOLID check**: note any design decision to watch out for — where LSP, ISP, or
  DIP could be violated if not careful
- **Definition of done**: how to verify the task is complete

Do not start implementing. Just report.
