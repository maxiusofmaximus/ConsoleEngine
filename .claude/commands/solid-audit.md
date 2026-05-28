Perform a DRY / SOLID / KISS audit on the file at: $ARGUMENTS

If no path is given, ask the user which file to audit.

Steps:
1. Read the file.
2. For each principle below, check if any violation exists:

   **SRP** — Does this class/module do more than one thing? Could it be split?
   **OCP** — Is there a `switch` or `if/else` chain that would need to grow when new
             types are added? Should it be replaced with polymorphism or a strategy?
   **LSP** — If this class inherits or implements an interface, does every override
             preserve the base contract? Are there `NotImplementedException` stubs?
   **ISP** — Are interfaces used here too broad? Are callers forced to depend on
             methods they don't use?
   **DIP** — Does a high-level class depend directly on a concrete low-level class
             instead of an abstraction? (e.g. `new ConcreteService()` inline)
   **DRY** — Is there logic duplicated elsewhere in the codebase?
   **KISS** — Is there unnecessary abstraction, indirection, or premature generalization?

3. For each violation found, output:
   - Principle violated
   - Line number(s)
   - One-sentence explanation
   - Suggested fix (in one sentence — no code, just direction)

4. If no violations are found, say so clearly.

Do not make any changes. Audit only.
