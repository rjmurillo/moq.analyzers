# Rule: Generating an Explainer or a Product Requirements Document (PRD)

## Goal

To guide an AI assistant in creating a detailed Explainer/Product Requirements Document (PRD) in Markdown format, based on an initial user prompt. The PRD should be clear, actionable, and suitable for a junior developer to understand and implement the feature.

## Process

1. **Receive Initial Prompt:** The user provides a brief description or request for a new feature or functionality.
2. **Ask Clarifying Questions:** Before writing the explainer, the AI *must* ask clarifying questions to gather sufficient detail. The goal is to understand the "why" and "what" of the feature, not necessarily the "how" (which the developer will figure out). All clarifying questions must be presented as enumerated letter or number lists to maximize clarity and ease of response. If any user answer is ambiguous, incomplete, or conflicting, the AI must explicitly flag the uncertainty, ask follow-up questions, and not proceed until the ambiguity is resolved or clearly documented as an open question or assumption.
3. **Generate Explainer:** Based on the initial prompt and the user's answers to the clarifying questions, generate an Explainer using the structure outlined below.
4. **Save Explainer:** Save the generated document as a GitHub issue with the title `Explainer: [feature-name]` inside the `Moq.Analyzers` repository using your GitHub MCP.

## Clarifying Questions (Examples)

The AI should adapt its questions based on the prompt, but must always:

- Present all clarifying questions as enumerated letter or number lists.
- Validate that each user story provided follows the INVEST mnemonic. If a user story does not, the AI must either rewrite it for compliance or ask the user for clarification.
- If any answer is unclear, incomplete, or conflicting, the AI must flag it and ask for clarification before proceeding.

Here are some common areas to explore:

- **Problem/Goal:** "What problem does this feature solve for the user?" or "What is the main goal we want to achieve with this feature?"
- **Target User:** "Who is the primary user of this feature?"
- **Core Functionality:** "Can you describe the key actions a user should be able to perform with this feature?"
- **User Stories:** "Could you provide a few user stories? (e.g., As a [type of user], I want to [perform an action] so that [benefit].)"
- **Structure with INVEST**: Does each user story follow the INVEST mnemonic?
- **Acceptance Criteria:** "How will we know when this feature is successfully implemented? What are the key success criteria?"
- **Scope/Boundaries:** "Are there any specific things this feature *should not* do (non-goals)?"
- **Data Requirements:** "What kind of data does this feature need to display or manipulate?"
- **Design/UI:** "Are there any existing design mockups or UI guidelines to follow?" or "Can you describe the desired look and feel?"
- **Edge Cases:** "Are there any potential edge cases or error conditions we should consider?"

## Explainer Structure

The generated explainer should include the following sections:

1. **Introduction/Overview:** Briefly describe the feature and the problem it solves. State the goal.
2. **Goals:** List the specific, measurable objectives for this feature.
3. **Non-Goals (Out of Scope):** Clearly state what this feature will *not* include to manage scope.
4. **User Stories:** Detail the user narratives describing feature usage and benefits.
5. **Functional Requirements:** List the specific functionalities the feature must have. Use clear, concise language (e.g., "The system must allow users to upload a profile picture."). Number these requirements.
6. **Design Considerations (Optional):** Link to mockups, describe UI/UX requirements, or mention relevant components/styles if applicable.
7. **Technical Considerations (Optional):** Mention any known technical constraints, dependencies, or suggestions (e.g., "Should integrate with the existing Auth module").
8. **Success Metrics:** How will the success of this feature be measured? (e.g., "Increase user engagement by 10%", "Reduce support tickets related to X").
9. **Open Questions:** List any remaining questions, areas needing further clarification, or assumptions made due to missing or ambiguous information. If there are no open questions or assumptions, explicitly state "None".

## Target Audience

Assume the primary reader of the Explainer is a **junior developer**. Therefore, requirements should be explicit, unambiguous, avoid jargon where possible, and be written with a grade 9 reading level. Provide enough detail for them to understand the feature's purpose and core logic.

## Output

- **Format:** Markdown (`.md`)
- **Location:** GitHub issue in the `rjmurillo/Moq.Analyzers` repository
- **Title:** `Explainer: [feature-name]`

## Final instructions

1. Do NOT start implementing the Explainer
2. Make sure to ask the user clarifying questions
3. Take the user's answers to the clarifying questions and improve the Explainer

---

## Example Explainer (Generic)

```markdown
# Explainer: Feature Name

## Introduction/Overview
Briefly describe the feature and the problem it solves. State the goal.

## Goals
- List specific, measurable objectives for this feature.

## Non-Goals (Out of Scope)
- List what is explicitly not included in this feature.

## User Stories
- As a [user type], I want to [do something] so that [benefit].
- As a [user type], I want to [do something else] so that [benefit].

## Functional Requirements
1. The system must allow users to do X.
2. The system must validate Y before Z.

## Design Considerations (Optional)
- Link to mockups or describe UI/UX requirements.

## Technical Considerations (Optional)
- List known technical constraints, dependencies, or suggestions.

## Success Metrics
- How will success be measured? (e.g., "Increase engagement by 10%", "Reduce support tickets related to X")

## Open Questions
- List any remaining questions, areas needing clarification, or assumptions. If none, state "None".
```
