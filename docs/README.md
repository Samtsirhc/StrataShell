# Engineering documentation

This directory is the source of truth for the project. Documents are grouped
by purpose so research, decisions, implementation, and QA evidence remain
auditable over a long-running development cycle.

## Structure

- `project/`: goal, scope, live status, milestones, and work log.
- `requirements/`: user requirements and release acceptance matrix.
- `research/`: software survey, platform constraints, architecture research,
  source URLs, and hands-on evaluation notes.
- `architecture/`: architecture decision records and subsystem designs.
- `qa/`: test strategy, test cases, visual baselines, performance results,
  and runtime evidence.
- `release/`: packaging, migration, publishing, and release checklists.

Publication-safe screenshots live in `images/`; reproducible results and
measurements are recorded in `qa/VALIDATION_REPORT.md` and linked from the
acceptance matrix. Machine-local raw artifacts remain intentionally untracked.
