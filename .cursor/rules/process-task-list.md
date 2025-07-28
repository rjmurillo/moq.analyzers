# Task List Management

Guidelines for managing task lists in GitHub issue Markdown files to track progress on completing an Explainer/PRD

## Task Implementation

- **One sub-task at a time:** Do **NOT** start the next sub‑task until you ask the user for permission and they say "yes" or "y"
- **Load in the appropriate rules:** Before beginning any work, load `./.github/copilot-instructions.md` and `./.github/instructions/README.md`
- **Build and Test Failures are STOP conditions**: If at any point a `dotnet build` or `dotnet test` command fails, you MUST stop. Do not proceed with the task list. Your immediate and only priority is to diagnose and fix the failure. Never apply a workaround to simply make a build pass. Investigate the root cause, and if you are unsure, you MUST ask for guidance.
- **Completion protocol:**
  1. When you finish a **sub‑task**, immediately mark it as completed by changing `[ ]` to `[x]`.
  2. If **all** subtasks underneath a parent task are now `[x]`, follow this sequence:
    - **First**: Run the full test suite (e.g., `dotnet test --settings ./build/targets/tests/test.runsettings`)
    - **Only if all tests pass**: Stage changes (`git add .`)
    - **Clean up**: Remove any temporary files and temporary code before committing
    - **Commit**: Use a descriptive commit message that:
      - Uses conventional commit format (`feat:`, `fix:`, `refactor:`, etc.)
      - Summarizes what was accomplished in the parent task
      - Lists key changes and additions
      - References the GitHub issue, Explainer/PRD issue, and Explainer/PRD context
      - **Formats the message as a single-line command using `-m` flags**, e.g.:

        ```text
        git commit -m "feat: add payment validation logic" -m "- Validates card type and expiry" -m "- Adds unit tests for edge cases" -m "Related to #123 in Explainer" -m "Fixes sub-task #456"
        ```

  3. Once all the subtasks are marked completed and changes have been committed, verify with the user the task is completed.
  4. Once the user has indicated the work is verified, push the branch and open a pull request.
    - **Title**: Uses convention commit format
    - **Body**: Be descriptive
      - **Explain**: all changes made, why they were made, and all validation performed
      - **Reference the Task** Use language to indicate the issue is resolved at the end of the description (e.g., `Fixes #456`, `Closes #123`, `Resolves #789` etc. )
- Stop after each sub‑task and wait for the user's go‑ahead.

## Task List Maintenance

1. **Update the task list as you work:**
   - Mark tasks and subtasks as completed (`[x]`) per the protocol above.
   - Add new tasks as they emerge. Use your GitHub MCP to accomplish this.

2. **Maintain the "Relevant Files" section:**
   - List every file created or modified.
   - Give each file a one‑line description of its purpose.

## AI Instructions

When working with task lists, the AI must:

1. Regularly update the task list file after finishing any significant work.
2. Follow the completion protocol:
   - Mark each finished **sub‑task** `[x]`.
   - Mark the **parent task** `[x]` once **all** its subtasks are `[x]`.
3. Add newly discovered tasks.
4. Keep "Relevant Files" accurate and up to date.
5. Before starting work, check which sub‑task is next.
6. After implementing a sub‑task, update the file and then pause for user approval.
