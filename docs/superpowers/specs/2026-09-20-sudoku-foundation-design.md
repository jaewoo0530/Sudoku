# Sudoku Foundation — Design

Date: 2026-09-20
Status: Approved for implementation

## Purpose

Build the foundational playable Sudoku game loop in the existing Unity
project (`SampleScene`, URP 2D template, UGUI + new Input System already
installed). Scope is the core rules and interaction model requested by the
user — not menus, save/load, timers, scoring, or difficulty selection UI.

## Requirements (source of truth, from user request)

1. Standard Sudoku rules (9x9 grid, 3x3 boxes, digits 1-9, no repeats in
   row/column/box).
2. Left-click a cell to select it. While selected, the player can either
   type a digit 1-9 on the keyboard, or left-click one of the 9 digits
   shown in a popup that appears on the selected cell. Any keyboard input
   other than 1-9 is ignored.
3. Left-clicking a digit into a cell that already has a value overwrites
   it.
4. Cells pre-filled as puzzle clues ("givens") cannot be clicked,
   selected, or modified in any way.
5. Right-clicking a digit in the popup (instead of left-clicking) does not
   set the cell's value — it adds that digit as a small pencil mark
   (candidate note) inside the cell. Multiple marks can accumulate.
6. Re-clicking a cell that currently holds pencil marks reopens the
   popup, and the player can again choose to enter a value or add more
   marks. Entering a value (keyboard or left-click) clears all existing
   marks and fills the value. Right-clicking a new digit adds it to the
   existing marks.

## Edge case decided during design review

Values and pencil marks are mutually exclusive per cell — a cell is either
"filled" (has a value, no marks) or "in notes mode" (value = 0, zero or
more marks), never both.

Consequence for an already-filled, non-given cell: if the player reopens
its popup and **right-clicks** a digit, the existing value is cleared
first and the cell switches to notes mode with that digit as its first
mark (rather than ignoring the right-click or leaving the old value in
place).

## Approach

**UI**: UGUI (Canvas/Button/Text), built in the existing `SampleScene`.
Chosen over custom SpriteRenderer/OnMouseDown handling because
`IPointerClickHandler`/`eventData.button` gives reliable, low-effort
left/right click discrimination, and `com.unity.ugui` is already a
project dependency.

**Puzzle generation**: randomized backtracking fill to produce a complete
valid solution grid, then randomly remove cell values one at a time,
re-checking after each removal (via the solver, counting up to 2
solutions) that the puzzle still has a unique solution — reverting the
removal if not — until a target givens count is reached. This is required
because "New Game" must produce a fresh, always-uniquely-solvable puzzle
rather than one fixed hardcoded board.

**Difficulty**: single fixed "medium" preset for this foundation:
**32 givens** (~40% of cells filled). Exposed as a constant so it can
become a difficulty parameter later without redesign.

## Components

### `Assets/Scripts/Core/` (plain C#, no MonoBehaviour, no UnityEngine UI deps)

- `Cell` (struct or small class): `int Value` (0 = empty), `bool IsGiven`,
  `int PencilMask` (bit `n` set = digit `n` is marked, bits 1-9 used).
- `SudokuBoard`: 9x9 array of `Cell`, plus row/col/box validity helpers
  (`CanPlace(row, col, digit)`).
- `SudokuSolver`: backtracking solver. `int CountSolutions(board, limit)`
  (stops early once `limit` reached — used with `limit = 2` for
  uniqueness checks) and `bool TrySolve(board, out solved)`.
- `SudokuGenerator`: `GeneratedPuzzle Generate(int targetGivens)` →
  produces `{ int[,] Givens, int[,] Solution }` using the fill-then-dig
  algorithm above.

### `Assets/Scripts/Game/`

- `SudokuGameController` (MonoBehaviour): owns the current `SudokuBoard`
  and the solution grid (kept for potential future "check" features, not
  used yet). Public API:
  - `NewGame()` — generates a fresh puzzle, resets selection, raises
    `OnBoardReset`.
  - `SelectCell(row, col)` — no-ops if the cell `IsGiven`; otherwise sets
    `SelectedCell` and raises `OnSelectionChanged`.
  - `SetValue(int digit)` — applies to `SelectedCell` if any and it is
    not given; validates `1 <= digit <= 9`; overwrites `Value`, clears
    `PencilMask`; raises `OnCellChanged(row, col)`.
  - `TogglePencilMark(int digit)` — applies to `SelectedCell` if any and
    not given; if `Value != 0`, first clears `Value` (per the edge-case
    rule above), then toggles the bit in `PencilMask`; raises
    `OnCellChanged(row, col)`.
  - Events: `OnBoardReset`, `OnSelectionChanged(int row, int col)`,
    `OnCellChanged(int row, int col)`.
- `SudokuInput` (MonoBehaviour): each frame, checks the new Input
  System's `Keyboard.current` for digit keys 1-9 only (`Keyboard.current`
  numeric row, e.g. `digit1Key`..`digit9Key`); on press, if a cell is
  selected, calls `controller.SetValue(digit)`. All other keys are
  ignored — never reach the controller.

### `Assets/Scripts/UI/`

- `CellView` (MonoBehaviour on a per-cell prefab instance): displays
  either the large value digit or a static 3x3 layout of small pencil
  digits (dim/hidden if not marked). Implements `IPointerClickHandler`;
  on left click calls `controller.SelectCell(row, col)` (which also
  triggers the popup to appear via the selection event). Given cells have
  their `Image`/raycast target disabled so no click event fires at all.
- `BoardView` (MonoBehaviour): instantiates the 9x9 `CellView` grid under
  a `GridLayoutGroup` (or manually positioned `RectTransform`s) at
  `Start`, drawing extra 2px separator `Image`s every 3 rows/cols for the
  3x3 box borders. Subscribes to controller events to refresh affected
  cells and to show/hide/reposition the number picker.
- `NumberPickerView` (MonoBehaviour on a popup prefab): a 3x3 grid of 9
  digit buttons. Positioned over the currently selected cell when shown.
  Each button implements `IPointerClickHandler`: left click →
  `controller.SetValue(digit)` and hide the popup; right click →
  `controller.TogglePencilMark(digit)` and keep the popup open. The popup
  hides when the player clicks anywhere else (another cell or empty
  background).
- `NewGameButtonView`: a single UGUI `Button` wired to
  `controller.NewGame()`.

## Data flow

Left-click editable cell → `CellView` → `GameController.SelectCell` →
`OnSelectionChanged` → `BoardView` shows `NumberPickerView` at that cell →
player either types a digit (`SudokuInput` → `SetValue`) or clicks a
popup digit (`NumberPickerView` → `SetValue`/`TogglePencilMark`) →
`GameController` mutates `SudokuBoard` → `OnCellChanged` → `BoardView`/
`CellView` re-render that cell.

## Visual style

Functional, minimal UGUI styling only: white cell background, black
digit/mark text, thin grid lines, thicker lines every 3 cells for boxes,
a distinct background color/outline for the selected cell and for given
(non-editable) cells. No custom art assets.

## Testing

- **EditMode tests** (`com.unity.test-framework`, already installed) for
  `SudokuSolver` and `SudokuGenerator`: a generated full solution grid is
  a valid complete Sudoku; a generated puzzle has exactly one solution;
  the puzzle's givens count matches the requested target; repeat across
  multiple generations to catch flakiness.
- **Manual/Play Mode verification** via Unity MCP tools after
  implementation: enter Play mode, click cells, enter digits via keyboard
  and via popup, right-click to add pencil marks, confirm given cells
  reject all interaction, confirm overwrite behavior, confirm the
  value/notes-mode exclusivity edge case, and confirm `New Game` produces
  a fresh valid board.

## Out of scope (explicitly not built now)

- Difficulty selection UI (only one fixed medium preset).
- Win/completion detection or celebration UI.
- Undo/redo, timers, scoring, save/load.
- Mobile/touch input.
