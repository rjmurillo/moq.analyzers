# Rule: Generating a Task List from an Explainer

## Goal

To guide an AI assistant in creating a detailed, step-by-step task list in Markdown format based on an existing Explainer or Product Requirements Document (PRD). The task list should guide a developer through implementation.

## Output

- **Format:** Markdown (`.md`)
- **Location:** GitHub issues in `rjmurillo/Moq.Analyzers` repository
- **Title:** `Tasks for [explainer-issue-title]` (e.g., `Tasks for Explainer Expand LINQ Coverage`)

Link the Tasks issue to the Explainer issue in GitHub using your MCP.

## Process

1. **Receive Explainer/PRD Reference:** The user points the AI to a specific Explainer or PRD GitHub issue
2. **Analyze Explainer/PRD:** The AI uses GitHub MCP and reads and analyzes the functional requirements, user stories, and other sections of the specified Explainer/PRD.
3. **Assess Current State:** Review the existing codebase to understand existing infrastructre, architectural patterns and conventions. Also, identify any existing components or features that already exist and could be relevant to the Explainer/PRD requirements. Then, identify existing related files, components, and utilities that can be leveraged or need modification.
4. **Phase 1: Generate Parent Tasks:** Based on the Explainer/PRD analysis and current state assessment, create the file and generate the main, high-level tasks required to implement the feature. Use your judgement on how many high-level tasks to use. It's likely to be about 5. Present these tasks to the user in the specified format (without sub-tasks yet). Inform the user: "I have generated the high-level tasks based on the Explainer/PRD. Ready to generate the sub-tasks? Respond with 'Go' to proceed."
5. **Wait for Confirmation:** Pause and wait for the user to respond with "Go".
6. **Phase 2: Generate Sub-Tasks:** Once the user confirms, break down each parent task into smaller, actionable sub-tasks necessary to complete the parent task. Ensure sub-tasks logically follow from the parent task, cover the implementation details implied by the PRD, and consider existing codebase patterns where relevant without being constrained by them.
7. **Identify Relevant Files:** Based on the tasks and Explainer/PRD, identify potential files that will need to be created or modified. List these under the `Relevant Files` section, including corresponding test files if applicable. Identification may be achieved through search and through code coverage analysis.
8. **Generate Final Output:** Combine the parent tasks, sub-tasks, relevant files, and notes into the final Markdown structure.
9. **Save Task List:** Save the generated document into a new set of GitHub issues linked to the parent tasks.

## Output Format

The generated task list _must_ follow this structure. The example below is generic and should be adapted to the specific Explainer/PRD being processed.

### GitHub Issue Structure Guidance

- The parent task list should be created as a GitHub issue (the parent issue), with each high-level task represented as a checklist item that links to a corresponding sub-task issue.
- Each sub-task should be created as its own GitHub issue, with a reference back to the parent issue (e.g., "Parent: #123").
- The parent issue should include a checklist like:
  - [ ] [Sub-task Title 1](https://github.com/org/repo/issues/456)
  - [ ] [Sub-task Title 2](https://github.com/org/repo/issues/457)
- Each sub-task issue should include a link to the parent issue at the top, and may include its own detailed checklist if needed.

#### Example Parent Issue Checklist

```markdown
- [ ] [Implement Core Analyzer Logic](https://github.com/org/repo/issues/456)
- [ ] [Add Test Coverage](https://github.com/org/repo/issues/457)
- [ ] [Update Documentation](https://github.com/org/repo/issues/458)
```

#### Example Sub-task Issue Header

```markdown
Parent: #123

## Sub-task Details

- [ ] Sub-step 1
- [ ] Sub-step 2
```

```markdown
## Relevant Files

- `src/Feature/FeatureAnalyzer.cs` - Main analyzer implementation for the feature described in the explainer/PRD.
- `tests/Feature/FeatureAnalyzerTests.cs` - Unit tests for the analyzer logic.
- `docs/rules/FeatureRule.md` - Documentation for the new or updated analyzer rule.

### Notes

When editing files, follow the guidance at `.github/instructions/README.md` to determine appropriate instructions for specific files.

## Tasks

- [ ] [Implement Core Analyzer Logic](https://github.com/org/repo/issues/456)
- [ ] [Add Test Coverage](https://github.com/org/repo/issues/457)
- [ ] [Update Documentation](https://github.com/org/repo/issues/458)
```

## Interaction Model

The process explicitly requires a pause after generating parent tasks to get user confirmation ("Go") before proceeding to generate the detailed sub-tasks. This ensures the high-level plan aligns with user expectations before diving into details.

## Target Audience

Assume the primary reader of the task list is a **junior developer** who will implement the feature with awareness of the existing codebase context.
