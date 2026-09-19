# Sudoku Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the foundational playable Sudoku loop (rules, cell selection, keyboard + popup digit entry, pencil marks, given-cell locking, New Game) in the existing `Assets/Scenes/SampleScene.unity`.

**Architecture:** A pure-C# `Core` layer (board data, solver, generator) with no Unity dependency, tested with EditMode NUnit tests; a thin `Game` layer (`SudokuGameController` MonoBehaviour + `SudokuInput`) that enforces the given-cell/value-vs-marks rules and exposes events; and a `UI` layer that builds the entire 9x9 grid, digit popup, and New Game button at runtime via UGUI C# APIs (no prefabs/manual scene art needed), driven purely by controller events.

**Tech Stack:** Unity 6000.3.8f1, UGUI (`UnityEngine.UI`), new Input System (`UnityEngine.InputSystem`, `Keyboard.current`), `com.unity.test-framework` (EditMode NUnit tests), Unity MCP tools for all script/scene/test operations.

**Spec:** [docs/superpowers/specs/2026-09-20-sudoku-foundation-design.md](../specs/2026-09-20-sudoku-foundation-design.md)

## Global Constraints

- Project's Input System is set to **new Input System only** (`activeInputHandler: 1` in ProjectSettings) — never use `UnityEngine.Input`; keyboard reads go through `Keyboard.current`, and the scene's `EventSystem` must use `InputSystemUIInputModule`, not the legacy `StandaloneInputModule`.
- UI is built with legacy UGUI (`Image`, `Text`, `Button`, `GridLayoutGroup`) — no UI Toolkit/UXML, no TextMeshPro dependency. Use `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` for all `Text` components (Unity 6's built-in font; `"Arial.ttf"` no longer resolves).
- All gameplay UI is built into the existing `Assets/Scenes/SampleScene.unity` — do not create a new scene.
- Difficulty is a single fixed medium preset: **32 givens**. No difficulty selection UI.
- A cell's value and pencil marks are mutually exclusive: setting a value always clears marks; toggling a mark on a filled (non-given) cell clears the value first, then starts notes mode with that mark. This lives in `SudokuBoard`, not the controller.
- Given cells must be fully inert: not selectable, not clickable at all (`Image.raycastTarget = false`), never mutable.
- Visual style is functional/minimal only (flat colors, no custom art assets) — do not spend time on polish beyond what's specified per task.

---

### Task 1: Core data model — `Cell` + `SudokuBoard`, with the EditMode test assembly

**Files:**
- Create: `Assets/Tests/EditMode/Sudoku.Tests.EditMode.asmdef`
- Create: `Assets/Scripts/Core/Cell.cs`
- Create: `Assets/Scripts/Core/SudokuBoard.cs`
- Test: `Assets/Tests/EditMode/SudokuBoardTests.cs`

**Interfaces:**
- Produces: `Sudoku.Core.Cell` — struct with `int Value` (0 = empty), `bool IsGiven`, `int PencilMask`, `bool HasMark(int digit)`, `void SetMark(int digit, bool on)`, `void ToggleMark(int digit)`.
- Produces: `Sudoku.Core.SudokuBoard` — class with `const int Size = 9`, `const int BoxSize = 3`, `Cell GetCell(int row, int col)`, `int GetValue(int row, int col)`, `bool IsGiven(int row, int col)`, `void LoadGivens(int[,] givens)`, `void SetValue(int row, int col, int digit)`, `void ToggleMark(int row, int col, int digit)`.
- Consumes: nothing (first task).

- [ ] **Step 1: Create the EditMode test assembly definition**

Use the `Write` tool (this is not a `.cs` file, so `create_script` doesn't apply) to create `Assets/Tests/EditMode/Sudoku.Tests.EditMode.asmdef`:

```json
{
    "name": "Sudoku.Tests.EditMode",
    "rootNamespace": "",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "Assembly-CSharp"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`"autoReferenced"` must be `false` here. If it's `true`, Unity's predefined `Assembly-CSharp` automatically references this test assembly (predefined assemblies auto-reference every `autoReferenced: true` asmdef), which creates a cycle with this asmdef's own explicit `"Assembly-CSharp"` reference — Unity silently drops the `Assembly-CSharp` reference to break the cycle, and `using Sudoku.Core;` fails with `CS0246` even though `Sudoku.Core` compiles cleanly. This is also why Unity's own "Tests Assembly Folder" template defaults to `autoReferenced: false`.

- [ ] **Step 2: Write the failing test**

Use `mcp__UnityMCP__create_script` with `path: "Assets/Tests/EditMode/SudokuBoardTests.cs"`:

```csharp
using NUnit.Framework;
using Sudoku.Core;

public class SudokuBoardTests
{
    [Test]
    public void LoadGivens_MarksNonZeroCellsAsGiven()
    {
        var board = new SudokuBoard();
        var grid = new int[9, 9];
        grid[0, 0] = 5;

        board.LoadGivens(grid);

        Assert.IsTrue(board.IsGiven(0, 0));
        Assert.AreEqual(5, board.GetValue(0, 0));
        Assert.IsFalse(board.IsGiven(0, 1));
        Assert.AreEqual(0, board.GetValue(0, 1));
    }

    [Test]
    public void SetValue_OverwritesExistingValueAndClearsPencilMask()
    {
        var board = new SudokuBoard();
        board.LoadGivens(new int[9, 9]);
        board.ToggleMark(2, 2, 4);
        board.ToggleMark(2, 2, 7);

        board.SetValue(2, 2, 3);

        Assert.AreEqual(3, board.GetValue(2, 2));
        Assert.IsFalse(board.GetCell(2, 2).HasMark(4));
        Assert.IsFalse(board.GetCell(2, 2).HasMark(7));
    }

    [Test]
    public void ToggleMark_OnFilledCell_ClearsValueAndStartsNotesMode()
    {
        var board = new SudokuBoard();
        board.LoadGivens(new int[9, 9]);
        board.SetValue(4, 4, 9);

        board.ToggleMark(4, 4, 2);

        Assert.AreEqual(0, board.GetValue(4, 4));
        Assert.IsTrue(board.GetCell(4, 4).HasMark(2));
    }

    [Test]
    public void ToggleMark_TwiceWithSameDigit_RemovesMark()
    {
        var board = new SudokuBoard();
        board.LoadGivens(new int[9, 9]);

        board.ToggleMark(0, 0, 6);
        board.ToggleMark(0, 0, 6);

        Assert.IsFalse(board.GetCell(0, 0).HasMark(6));
    }
}
```

- [ ] **Step 3: Refresh Unity and run the test to verify it fails**

Run `mcp__UnityMCP__refresh_unity` with `compile: "request"`, `wait_for_ready: true`. Then run `mcp__UnityMCP__run_tests` with `mode: "EditMode"`, `assembly_names: ["Sudoku.Tests.EditMode"]`, poll with `mcp__UnityMCP__get_test_job` until done.

Expected: compile error (`Sudoku.Core` namespace / `SudokuBoard` type not found) — this is the "red" state since the types don't exist yet.

- [ ] **Step 4: Implement `Cell`**

`mcp__UnityMCP__create_script`, `path: "Assets/Scripts/Core/Cell.cs"`:

```csharp
namespace Sudoku.Core
{
    public struct Cell
    {
        public int Value; // 0 = empty, 1-9 = filled
        public bool IsGiven;
        public int PencilMask; // bit (n-1) set => digit n is marked

        public bool HasMark(int digit) => (PencilMask & (1 << (digit - 1))) != 0;

        public void SetMark(int digit, bool on)
        {
            int bit = 1 << (digit - 1);
            PencilMask = on ? (PencilMask | bit) : (PencilMask & ~bit);
        }

        public void ToggleMark(int digit)
        {
            PencilMask ^= 1 << (digit - 1);
        }
    }
}
```

- [ ] **Step 5: Implement `SudokuBoard`**

`mcp__UnityMCP__create_script`, `path: "Assets/Scripts/Core/SudokuBoard.cs"`:

```csharp
namespace Sudoku.Core
{
    public class SudokuBoard
    {
        public const int Size = 9;
        public const int BoxSize = 3;

        private readonly Cell[,] _cells = new Cell[Size, Size];

        public Cell GetCell(int row, int col) => _cells[row, col];

        public int GetValue(int row, int col) => _cells[row, col].Value;

        public bool IsGiven(int row, int col) => _cells[row, col].IsGiven;

        public void LoadGivens(int[,] givens)
        {
            for (int r = 0; r < Size; r++)
            {
                for (int c = 0; c < Size; c++)
                {
                    int v = givens[r, c];
                    _cells[r, c] = new Cell
                    {
                        Value = v,
                        IsGiven = v != 0,
                        PencilMask = 0
                    };
                }
            }
        }

        public void SetValue(int row, int col, int digit)
        {
            Cell cell = _cells[row, col];
            cell.Value = digit;
            cell.PencilMask = 0;
            _cells[row, col] = cell;
        }

        // If the cell currently holds a value, clears it first so value and
        // marks stay mutually exclusive, then toggles the requested mark.
        public void ToggleMark(int row, int col, int digit)
        {
            Cell cell = _cells[row, col];
            if (cell.Value != 0)
            {
                cell.Value = 0;
            }
            cell.ToggleMark(digit);
            _cells[row, col] = cell;
        }
    }
}
```

- [ ] **Step 6: Refresh Unity and run the test to verify it passes**

`mcp__UnityMCP__refresh_unity` (`compile: "request"`, `wait_for_ready: true`), then `mcp__UnityMCP__run_tests` (`mode: "EditMode"`, `assembly_names: ["Sudoku.Tests.EditMode"]`), poll via `get_test_job`.

Expected: all 4 tests in `SudokuBoardTests` PASS.

- [ ] **Step 7: Commit**

```bash
git add Assets/Tests/EditMode/Sudoku.Tests.EditMode.asmdef Assets/Tests/EditMode/Sudoku.Tests.EditMode.asmdef.meta \
        Assets/Tests/EditMode/SudokuBoardTests.cs Assets/Tests/EditMode/SudokuBoardTests.cs.meta \
        Assets/Scripts/Core/Cell.cs Assets/Scripts/Core/Cell.cs.meta \
        Assets/Scripts/Core/SudokuBoard.cs Assets/Scripts/Core/SudokuBoard.cs.meta
git commit -m "Add SudokuBoard core data model with EditMode tests"
```

---

### Task 2: `SudokuSolver`

**Files:**
- Create: `Assets/Scripts/Core/SudokuSolver.cs`
- Test: `Assets/Tests/EditMode/SudokuSolverTests.cs`

**Interfaces:**
- Consumes: nothing new (operates on plain `int[9,9]` grids, not `SudokuBoard`).
- Produces: `Sudoku.Core.SudokuSolver` — static class with `const int Size = 9`, `static bool IsSafe(int[,] grid, int row, int col, int digit)`, `static int CountSolutions(int[,] grid, int limit)`, `static bool TrySolve(int[,] grid, out int[,] solution)`.

- [ ] **Step 1: Write the failing tests**

`mcp__UnityMCP__create_script`, `path: "Assets/Tests/EditMode/SudokuSolverTests.cs"`:

```csharp
using NUnit.Framework;
using Sudoku.Core;

public class SudokuSolverTests
{
    private static readonly int[,] SolvedGrid =
    {
        {5,3,4,6,7,8,9,1,2},
        {6,7,2,1,9,5,3,4,8},
        {1,9,8,3,4,2,5,6,7},
        {8,5,9,7,6,1,4,2,3},
        {4,2,6,8,5,3,7,9,1},
        {7,1,3,9,2,4,8,5,6},
        {9,6,1,5,3,7,2,8,4},
        {2,8,7,4,1,9,6,3,5},
        {3,4,5,2,8,6,1,7,9}
    };

    [Test]
    public void IsSafe_RejectsDigitAlreadyInRow()
    {
        var grid = (int[,])SolvedGrid.Clone();
        grid[0, 0] = 0;

        Assert.IsFalse(SudokuSolver.IsSafe(grid, 0, 0, 3)); // 3 already in row 0
    }

    [Test]
    public void IsSafe_AllowsValidPlacement()
    {
        var grid = (int[,])SolvedGrid.Clone();
        grid[0, 0] = 0;

        Assert.IsTrue(SudokuSolver.IsSafe(grid, 0, 0, 5)); // matches the known solution
    }

    [Test]
    public void CountSolutions_OnCompleteGrid_ReturnsOne()
    {
        Assert.AreEqual(1, SudokuSolver.CountSolutions(SolvedGrid, 2));
    }

    [Test]
    public void CountSolutions_OnEmptyGrid_ReachesLimit()
    {
        var grid = new int[9, 9];

        Assert.AreEqual(2, SudokuSolver.CountSolutions(grid, 2));
    }

    [Test]
    public void TrySolve_SolvesAPuzzleWithOneCellRemoved()
    {
        var puzzle = (int[,])SolvedGrid.Clone();
        puzzle[0, 0] = 0;

        bool solved = SudokuSolver.TrySolve(puzzle, out int[,] solution);

        Assert.IsTrue(solved);
        Assert.AreEqual(5, solution[0, 0]);
    }
}
```

- [ ] **Step 2: Refresh and run to verify failure**

`refresh_unity` (`compile: "request"`), `run_tests` (`assembly_names: ["Sudoku.Tests.EditMode"]`). Expected: compile error, `SudokuSolver` not found.

- [ ] **Step 3: Implement `SudokuSolver`**

`mcp__UnityMCP__create_script`, `path: "Assets/Scripts/Core/SudokuSolver.cs"`:

```csharp
namespace Sudoku.Core
{
    public static class SudokuSolver
    {
        public const int Size = 9;
        private const int BoxSize = 3;

        public static bool IsSafe(int[,] grid, int row, int col, int digit)
        {
            for (int i = 0; i < Size; i++)
            {
                if (grid[row, i] == digit) return false;
                if (grid[i, col] == digit) return false;
            }

            int boxRow = (row / BoxSize) * BoxSize;
            int boxCol = (col / BoxSize) * BoxSize;
            for (int r = boxRow; r < boxRow + BoxSize; r++)
                for (int c = boxCol; c < boxCol + BoxSize; c++)
                    if (grid[r, c] == digit) return false;

            return true;
        }

        // Counts solutions up to `limit` (stops early once reached). Never mutates `grid`.
        public static int CountSolutions(int[,] grid, int limit)
        {
            int[,] working = (int[,])grid.Clone();
            int count = 0;
            CountSolutionsRecursive(working, ref count, limit);
            return count;
        }

        private static bool CountSolutionsRecursive(int[,] grid, ref int count, int limit)
        {
            if (!FindEmpty(grid, out int row, out int col))
            {
                count++;
                return count >= limit;
            }

            for (int digit = 1; digit <= Size; digit++)
            {
                if (!IsSafe(grid, row, col, digit)) continue;
                grid[row, col] = digit;
                bool stop = CountSolutionsRecursive(grid, ref count, limit);
                grid[row, col] = 0;
                if (stop) return true;
            }

            return false;
        }

        public static bool TrySolve(int[,] grid, out int[,] solution)
        {
            solution = (int[,])grid.Clone();
            return SolveRecursive(solution);
        }

        private static bool SolveRecursive(int[,] grid)
        {
            if (!FindEmpty(grid, out int row, out int col)) return true;

            for (int digit = 1; digit <= Size; digit++)
            {
                if (!IsSafe(grid, row, col, digit)) continue;
                grid[row, col] = digit;
                if (SolveRecursive(grid)) return true;
                grid[row, col] = 0;
            }

            return false;
        }

        private static bool FindEmpty(int[,] grid, out int row, out int col)
        {
            for (row = 0; row < Size; row++)
                for (col = 0; col < Size; col++)
                    if (grid[row, col] == 0) return true;
            row = -1;
            col = -1;
            return false;
        }
    }
}
```

- [ ] **Step 4: Refresh and run to verify pass**

Same as Step 2 but expect all 5 `SudokuSolverTests` to PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Core/SudokuSolver.cs Assets/Scripts/Core/SudokuSolver.cs.meta \
        Assets/Tests/EditMode/SudokuSolverTests.cs Assets/Tests/EditMode/SudokuSolverTests.cs.meta
git commit -m "Add SudokuSolver backtracking solver with tests"
```

---

### Task 3: `SudokuGenerator`

**Files:**
- Create: `Assets/Scripts/Core/SudokuGenerator.cs`
- Test: `Assets/Tests/EditMode/SudokuGeneratorTests.cs`

**Interfaces:**
- Consumes: `Sudoku.Core.SudokuSolver.IsSafe`, `Sudoku.Core.SudokuSolver.CountSolutions` (Task 2).
- Produces: `Sudoku.Core.GeneratedPuzzle` — struct `{ int[,] Givens; int[,] Solution; }`. `Sudoku.Core.SudokuGenerator` — static class with `static GeneratedPuzzle Generate(int targetGivens, System.Random random = null)`.

- [ ] **Step 1: Write the failing tests**

`mcp__UnityMCP__create_script`, `path: "Assets/Tests/EditMode/SudokuGeneratorTests.cs"`:

```csharp
using System;
using NUnit.Framework;
using Sudoku.Core;

public class SudokuGeneratorTests
{
    [Test]
    public void Generate_ProducesACompleteValidSolution()
    {
        var puzzle = SudokuGenerator.Generate(32, new Random(1));

        AssertIsCompleteValidGrid(puzzle.Solution);
    }

    [Test]
    public void Generate_ProducesAPuzzleWithExactlyOneSolution()
    {
        var puzzle = SudokuGenerator.Generate(32, new Random(2));

        Assert.AreEqual(1, SudokuSolver.CountSolutions(puzzle.Givens, 2));
    }

    [Test]
    public void Generate_MatchesRequestedGivensCount()
    {
        var puzzle = SudokuGenerator.Generate(32, new Random(3));

        int count = 0;
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                if (puzzle.Givens[r, c] != 0) count++;

        Assert.AreEqual(32, count);
    }

    [Test]
    public void Generate_GivensAreConsistentWithSolution()
    {
        var puzzle = SudokuGenerator.Generate(32, new Random(4));

        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                if (puzzle.Givens[r, c] != 0)
                    Assert.AreEqual(puzzle.Solution[r, c], puzzle.Givens[r, c]);
    }

    private static void AssertIsCompleteValidGrid(int[,] grid)
    {
        for (int i = 0; i < 9; i++)
        {
            AssertIsPermutationOfOneToNine(RowValues(grid, i));
            AssertIsPermutationOfOneToNine(ColValues(grid, i));
        }

        for (int boxRow = 0; boxRow < 3; boxRow++)
            for (int boxCol = 0; boxCol < 3; boxCol++)
                AssertIsPermutationOfOneToNine(BoxValues(grid, boxRow, boxCol));
    }

    private static void AssertIsPermutationOfOneToNine(int[] values)
    {
        var seen = new bool[10];
        foreach (int v in values)
        {
            Assert.IsTrue(v >= 1 && v <= 9);
            Assert.IsFalse(seen[v], "Duplicate digit found");
            seen[v] = true;
        }
    }

    private static int[] RowValues(int[,] grid, int row)
    {
        var values = new int[9];
        for (int c = 0; c < 9; c++) values[c] = grid[row, c];
        return values;
    }

    private static int[] ColValues(int[,] grid, int col)
    {
        var values = new int[9];
        for (int r = 0; r < 9; r++) values[r] = grid[r, col];
        return values;
    }

    private static int[] BoxValues(int[,] grid, int boxRow, int boxCol)
    {
        var values = new int[9];
        int index = 0;
        for (int r = boxRow * 3; r < boxRow * 3 + 3; r++)
            for (int c = boxCol * 3; c < boxCol * 3 + 3; c++)
                values[index++] = grid[r, c];
        return values;
    }
}
```

- [ ] **Step 2: Refresh and run to verify failure**

`refresh_unity` (`compile: "request"`), `run_tests`. Expected: compile error, `SudokuGenerator`/`GeneratedPuzzle` not found.

- [ ] **Step 3: Implement `SudokuGenerator`**

`mcp__UnityMCP__create_script`, `path: "Assets/Scripts/Core/SudokuGenerator.cs"`:

```csharp
using System;

namespace Sudoku.Core
{
    public struct GeneratedPuzzle
    {
        public int[,] Givens;
        public int[,] Solution;
    }

    public static class SudokuGenerator
    {
        private const int Size = SudokuSolver.Size;

        public static GeneratedPuzzle Generate(int targetGivens, Random random = null)
        {
            random ??= new Random();

            int[,] solution = GenerateFullSolution(random);
            int[,] givens = DigHoles(solution, targetGivens, random);

            return new GeneratedPuzzle { Givens = givens, Solution = solution };
        }

        private static int[,] GenerateFullSolution(Random random)
        {
            var grid = new int[Size, Size];
            FillRecursive(grid, 0, 0, random);
            return grid;
        }

        private static bool FillRecursive(int[,] grid, int row, int col, Random random)
        {
            if (row == Size) return true;

            int nextRow = col == Size - 1 ? row + 1 : row;
            int nextCol = col == Size - 1 ? 0 : col + 1;

            foreach (int digit in ShuffledDigits(random))
            {
                if (!SudokuSolver.IsSafe(grid, row, col, digit)) continue;
                grid[row, col] = digit;
                if (FillRecursive(grid, nextRow, nextCol, random)) return true;
                grid[row, col] = 0;
            }

            return false;
        }

        private static int[] ShuffledDigits(Random random)
        {
            int[] digits = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            for (int i = digits.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (digits[i], digits[j]) = (digits[j], digits[i]);
            }
            return digits;
        }

        private static int[,] DigHoles(int[,] solution, int targetGivens, Random random)
        {
            var puzzle = (int[,])solution.Clone();
            var cells = new (int row, int col)[Size * Size];
            int index = 0;
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    cells[index++] = (r, c);

            for (int i = cells.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (cells[i], cells[j]) = (cells[j], cells[i]);
            }

            int givensCount = Size * Size;

            foreach (var (row, col) in cells)
            {
                if (givensCount <= targetGivens) break;

                int backup = puzzle[row, col];
                puzzle[row, col] = 0;

                if (SudokuSolver.CountSolutions(puzzle, 2) != 1)
                {
                    puzzle[row, col] = backup;
                    continue;
                }

                givensCount--;
            }

            return puzzle;
        }
    }
}
```

- [ ] **Step 4: Refresh and run to verify pass**

Expect all 4 `SudokuGeneratorTests` to PASS. (Note: generation involves backtracking search; a single `Generate` call should still complete well under a second for `targetGivens = 32`.)

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Core/SudokuGenerator.cs Assets/Scripts/Core/SudokuGenerator.cs.meta \
        Assets/Tests/EditMode/SudokuGeneratorTests.cs Assets/Tests/EditMode/SudokuGeneratorTests.cs.meta
git commit -m "Add SudokuGenerator with unique-solution digging and tests"
```

---

### Task 4: Game layer — `SudokuGameController` + `SudokuInput`

**Files:**
- Create: `Assets/Scripts/Game/SudokuGameController.cs`
- Create: `Assets/Scripts/Game/SudokuInput.cs`
- Test: `Assets/Tests/EditMode/SudokuGameControllerTests.cs`

**Interfaces:**
- Consumes: `Sudoku.Core.SudokuBoard`, `Sudoku.Core.SudokuGenerator.Generate` (Tasks 1, 3).
- Produces: `Sudoku.Game.SudokuGameController` — MonoBehaviour with `SudokuBoard Board { get; }`, `int SelectedRow { get; }`, `int SelectedCol { get; }`, `bool HasSelection { get; }`, `event Action OnBoardReset`, `event Action OnSelectionChanged`, `event Action<int,int> OnCellChanged`, `void NewGame()`, `void SelectCell(int row, int col)`, `void ClearSelection()`, `void SetValue(int digit)`, `void TogglePencilMark(int digit)`. `Sudoku.Game.SudokuInput` — MonoBehaviour with `[SerializeField] SudokuGameController controller`.

- [ ] **Step 1: Write the failing tests**

`mcp__UnityMCP__create_script`, `path: "Assets/Tests/EditMode/SudokuGameControllerTests.cs"`:

```csharp
using NUnit.Framework;
using Sudoku.Game;
using UnityEngine;

public class SudokuGameControllerTests
{
    private SudokuGameController _controller;

    [SetUp]
    public void SetUp()
    {
        var go = new GameObject("Controller");
        _controller = go.AddComponent<SudokuGameController>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_controller.gameObject);
    }

    [Test]
    public void SelectCell_OnGivenCell_DoesNotSelect()
    {
        FindGivenCell(out int row, out int col);

        _controller.SelectCell(row, col);

        Assert.IsFalse(_controller.HasSelection);
    }

    [Test]
    public void SelectCell_OnEmptyCell_Selects()
    {
        int row = FindEmptyRow(out int col);

        _controller.SelectCell(row, col);

        Assert.IsTrue(_controller.HasSelection);
        Assert.AreEqual(row, _controller.SelectedRow);
        Assert.AreEqual(col, _controller.SelectedCol);
    }

    [Test]
    public void SetValue_IgnoresOutOfRangeDigits()
    {
        int row = FindEmptyRow(out int col);
        _controller.SelectCell(row, col);

        _controller.SetValue(0);
        _controller.SetValue(10);

        Assert.AreEqual(0, _controller.Board.GetValue(row, col));
    }

    [Test]
    public void SetValue_OnSelectedEmptyCell_SetsValue()
    {
        int row = FindEmptyRow(out int col);
        _controller.SelectCell(row, col);

        _controller.SetValue(7);

        Assert.AreEqual(7, _controller.Board.GetValue(row, col));
    }

    [Test]
    public void TogglePencilMark_OnFilledCell_SwitchesToNotesMode()
    {
        int row = FindEmptyRow(out int col);
        _controller.SelectCell(row, col);
        _controller.SetValue(7);

        _controller.TogglePencilMark(3);

        Assert.AreEqual(0, _controller.Board.GetValue(row, col));
        Assert.IsTrue(_controller.Board.GetCell(row, col).HasMark(3));
    }

    [Test]
    public void NewGame_ReplacesBoardAndClearsSelection()
    {
        int row = FindEmptyRow(out int col);
        _controller.SelectCell(row, col);

        _controller.NewGame();

        Assert.IsFalse(_controller.HasSelection);
        Assert.IsNotNull(_controller.Board);
    }

    private void FindGivenCell(out int row, out int col)
    {
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                if (_controller.Board.IsGiven(r, c)) { row = r; col = c; return; }
        row = -1;
        col = -1;
    }

    private int FindEmptyRow(out int col)
    {
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                if (!_controller.Board.IsGiven(r, c)) { col = c; return r; }
        col = -1;
        return -1;
    }
}
```

- [ ] **Step 2: Refresh and run to verify failure**

`refresh_unity` (`compile: "request"`), `run_tests`. Expected: compile error, `SudokuGameController` not found.

- [ ] **Step 3: Implement `SudokuGameController`**

`mcp__UnityMCP__create_script`, `path: "Assets/Scripts/Game/SudokuGameController.cs"`:

```csharp
using System;
using Sudoku.Core;
using UnityEngine;

namespace Sudoku.Game
{
    public class SudokuGameController : MonoBehaviour
    {
        [SerializeField] private int targetGivens = 32;

        public SudokuBoard Board { get; private set; }
        public int SelectedRow { get; private set; } = -1;
        public int SelectedCol { get; private set; } = -1;
        public bool HasSelection => SelectedRow >= 0;

        public event Action OnBoardReset;
        public event Action OnSelectionChanged;
        public event Action<int, int> OnCellChanged;

        private void Awake()
        {
            NewGame();
        }

        public void NewGame()
        {
            var puzzle = SudokuGenerator.Generate(targetGivens);
            Board = new SudokuBoard();
            Board.LoadGivens(puzzle.Givens);
            SelectedRow = -1;
            SelectedCol = -1;
            OnBoardReset?.Invoke();
        }

        public void SelectCell(int row, int col)
        {
            if (Board.IsGiven(row, col)) return;

            SelectedRow = row;
            SelectedCol = col;
            OnSelectionChanged?.Invoke();
        }

        public void ClearSelection()
        {
            SelectedRow = -1;
            SelectedCol = -1;
            OnSelectionChanged?.Invoke();
        }

        public void SetValue(int digit)
        {
            if (digit < 1 || digit > 9) return;
            if (!HasSelection) return;
            if (Board.IsGiven(SelectedRow, SelectedCol)) return;

            Board.SetValue(SelectedRow, SelectedCol, digit);
            OnCellChanged?.Invoke(SelectedRow, SelectedCol);
        }

        public void TogglePencilMark(int digit)
        {
            if (digit < 1 || digit > 9) return;
            if (!HasSelection) return;
            if (Board.IsGiven(SelectedRow, SelectedCol)) return;

            Board.ToggleMark(SelectedRow, SelectedCol, digit);
            OnCellChanged?.Invoke(SelectedRow, SelectedCol);
        }
    }
}
```

- [ ] **Step 4: Implement `SudokuInput`**

`mcp__UnityMCP__create_script`, `path: "Assets/Scripts/Game/SudokuInput.cs"`:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace Sudoku.Game
{
    public class SudokuInput : MonoBehaviour
    {
        [SerializeField] private SudokuGameController controller;

        private static readonly Key[] DigitKeys =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
            Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
        };

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !controller.HasSelection) return;

            for (int i = 0; i < DigitKeys.Length; i++)
            {
                if (keyboard[DigitKeys[i]].wasPressedThisFrame)
                {
                    controller.SetValue(i + 1);
                    break;
                }
            }
        }
    }
}
```

`SudokuInput` only ever reads the 9 listed keys, so any other keyboard input (letters, symbols, arrow keys, etc.) never reaches `SetValue` — this is how "ignore non 1-9 input" (requirement 2) is satisfied, with no separate validation branch needed.

- [ ] **Step 5: Refresh and run to verify pass**

Expect all 6 `SudokuGameControllerTests` to PASS. (`SudokuInput` has no automated test here — it's a thin `Keyboard.current` poll verified manually in Task 7's Play Mode check, since simulating Input System device events under EditMode NUnit requires `InputTestFixture` machinery that's out of scope for this foundation.)

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Game/SudokuGameController.cs Assets/Scripts/Game/SudokuGameController.cs.meta \
        Assets/Scripts/Game/SudokuInput.cs Assets/Scripts/Game/SudokuInput.cs.meta \
        Assets/Tests/EditMode/SudokuGameControllerTests.cs Assets/Tests/EditMode/SudokuGameControllerTests.cs.meta
git commit -m "Add SudokuGameController and keyboard input with tests"
```

---

### Task 5: UI layer — `CellView`, `PickerButton`, `NumberPickerView`, `BackgroundClickCatcher`, `NewGameButtonView`, `BoardView`

**Files:**
- Create: `Assets/Scripts/UI/CellView.cs`
- Create: `Assets/Scripts/UI/PickerButton.cs`
- Create: `Assets/Scripts/UI/NumberPickerView.cs`
- Create: `Assets/Scripts/UI/BackgroundClickCatcher.cs`
- Create: `Assets/Scripts/UI/NewGameButtonView.cs`
- Create: `Assets/Scripts/UI/BoardView.cs`

**Interfaces:**
- Consumes: `Sudoku.Game.SudokuGameController` (Task 4) — `Board`, `HasSelection`, `SelectedRow`/`SelectedCol`, `SelectCell`, `ClearSelection`, `SetValue`, `TogglePencilMark`, `OnBoardReset`, `OnSelectionChanged`, `OnCellChanged`.
- Produces: `Sudoku.UI.CellView` (`int Row`, `int Col`, `void Initialize(...)`, `void Refresh(bool isSelected)`, implements `IPointerClickHandler`), `Sudoku.UI.PickerButton` (`void Bind(int digit, Action<int, PointerEventData.InputButton> onClick)`, implements `IPointerClickHandler`), `Sudoku.UI.NumberPickerView` (`void Initialize(SudokuGameController controller, PickerButton[] buttons)`, `void ShowAt(Vector2 anchoredPosition)`, `void Hide()`), `Sudoku.UI.BackgroundClickCatcher` (`void Initialize(SudokuGameController controller, BoardView boardView)`), `Sudoku.UI.NewGameButtonView` (`[SerializeField] SudokuGameController controller`), `Sudoku.UI.BoardView` (`[SerializeField] SudokuGameController controller`, `void ShowPickerFor(CellView cell)`, `void HidePicker()`).

This task has no automated tests (it's runtime UI construction) — it's verified in Task 7 via live Play Mode checks. Build and validate each script compiles before moving to the next.

- [ ] **Step 1: Implement `PickerButton`**

`mcp__UnityMCP__create_script`, `path: "Assets/Scripts/UI/PickerButton.cs"`:

```csharp
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Sudoku.UI
{
    public class PickerButton : MonoBehaviour, IPointerClickHandler
    {
        private int _digit;
        private Action<int, PointerEventData.InputButton> _onClick;

        public void Bind(int digit, Action<int, PointerEventData.InputButton> onClick)
        {
            _digit = digit;
            _onClick = onClick;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _onClick?.Invoke(_digit, eventData.button);
        }
    }
}
```

- [ ] **Step 2: Implement `NumberPickerView`**

`mcp__UnityMCP__create_script`, `path: "Assets/Scripts/UI/NumberPickerView.cs"`:

```csharp
using Sudoku.Game;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Sudoku.UI
{
    public class NumberPickerView : MonoBehaviour
    {
        private SudokuGameController _controller;

        public void Initialize(SudokuGameController controller, PickerButton[] buttons)
        {
            _controller = controller;
            for (int i = 0; i < buttons.Length; i++)
            {
                int digit = i + 1;
                buttons[i].Bind(digit, OnDigitClicked);
            }
            gameObject.SetActive(false);
        }

        private void OnDigitClicked(int digit, PointerEventData.InputButton button)
        {
            if (button == PointerEventData.InputButton.Left)
            {
                _controller.SetValue(digit);
                Hide();
            }
            else if (button == PointerEventData.InputButton.Right)
            {
                _controller.TogglePencilMark(digit);
            }
        }

        public void ShowAt(Vector2 anchoredPosition)
        {
            var rect = (RectTransform)transform;
            rect.anchoredPosition = anchoredPosition;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
```

- [ ] **Step 3: Implement `CellView`**

`mcp__UnityMCP__create_script`, `path: "Assets/Scripts/UI/CellView.cs"`:

```csharp
using Sudoku.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sudoku.UI
{
    public class CellView : MonoBehaviour, IPointerClickHandler
    {
        public int Row { get; private set; }
        public int Col { get; private set; }

        private SudokuGameController _controller;
        private BoardView _boardView;
        private Image _background;
        private Text _valueText;
        private Text[] _markTexts;

        private static readonly Color GivenColor = new Color(0.85f, 0.85f, 0.85f);
        private static readonly Color NormalColor = Color.white;
        private static readonly Color SelectedColor = new Color(0.75f, 0.88f, 1f);

        public void Initialize(SudokuGameController controller, BoardView boardView, int row, int col,
            Image background, Text valueText, Text[] markTexts)
        {
            _controller = controller;
            _boardView = boardView;
            Row = row;
            Col = col;
            _background = background;
            _valueText = valueText;
            _markTexts = markTexts;
        }

        public void Refresh(bool isSelected)
        {
            var cell = _controller.Board.GetCell(Row, Col);

            _background.raycastTarget = !cell.IsGiven;
            _background.color = cell.IsGiven ? GivenColor : (isSelected ? SelectedColor : NormalColor);

            if (cell.Value != 0)
            {
                _valueText.text = cell.Value.ToString();
                _valueText.gameObject.SetActive(true);
                foreach (var t in _markTexts) t.gameObject.SetActive(false);
            }
            else
            {
                _valueText.gameObject.SetActive(false);
                for (int digit = 1; digit <= 9; digit++)
                {
                    var t = _markTexts[digit - 1];
                    bool marked = cell.HasMark(digit);
                    t.gameObject.SetActive(marked);
                    if (marked) t.text = digit.ToString();
                }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (_controller.Board.IsGiven(Row, Col)) return;

            _controller.SelectCell(Row, Col);
            _boardView.ShowPickerFor(this);
        }
    }
}
```

- [ ] **Step 4: Implement `BackgroundClickCatcher`**

`mcp__UnityMCP__create_script`, `path: "Assets/Scripts/UI/BackgroundClickCatcher.cs"`:

```csharp
using Sudoku.Game;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Sudoku.UI
{
    public class BackgroundClickCatcher : MonoBehaviour, IPointerClickHandler
    {
        private SudokuGameController _controller;
        private BoardView _boardView;

        public void Initialize(SudokuGameController controller, BoardView boardView)
        {
            _controller = controller;
            _boardView = boardView;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _controller.ClearSelection();
            _boardView.HidePicker();
        }
    }
}
```

- [ ] **Step 5: Implement `NewGameButtonView`**

`mcp__UnityMCP__create_script`, `path: "Assets/Scripts/UI/NewGameButtonView.cs"`:

```csharp
using Sudoku.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.UI
{
    public class NewGameButtonView : MonoBehaviour
    {
        [SerializeField] private SudokuGameController controller;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(controller.NewGame);
        }
    }
}
```

- [ ] **Step 6: Implement `BoardView`**

`mcp__UnityMCP__create_script`, `path: "Assets/Scripts/UI/BoardView.cs"`:

```csharp
using Sudoku.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.UI
{
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private SudokuGameController controller;
        [SerializeField] private float boardSize = 540f;
        [SerializeField] private float boxSpacing = 6f;
        [SerializeField] private float cellSpacing = 1f;

        private readonly CellView[,] _cells = new CellView[9, 9];
        private NumberPickerView _picker;

        private void Awake()
        {
            BuildBoard();
            BuildPicker();
        }

        private void OnEnable()
        {
            controller.OnBoardReset += HandleBoardReset;
            controller.OnSelectionChanged += HandleSelectionChanged;
            controller.OnCellChanged += HandleCellChanged;
        }

        private void OnDisable()
        {
            controller.OnBoardReset -= HandleBoardReset;
            controller.OnSelectionChanged -= HandleSelectionChanged;
            controller.OnCellChanged -= HandleCellChanged;
        }

        private void Start()
        {
            RefreshAll();
        }

        private void HandleBoardReset()
        {
            _picker.Hide();
            RefreshAll();
        }

        private void HandleSelectionChanged()
        {
            RefreshAll();
        }

        private void HandleCellChanged(int row, int col)
        {
            _cells[row, col].Refresh(IsSelected(row, col));
        }

        private bool IsSelected(int row, int col)
        {
            return controller.HasSelection && controller.SelectedRow == row && controller.SelectedCol == col;
        }

        private void RefreshAll()
        {
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    _cells[r, c].Refresh(IsSelected(r, c));
        }

        public void ShowPickerFor(CellView cell)
        {
            _picker.transform.SetParent(cell.transform, false);
            _picker.ShowAt(Vector2.zero);
        }

        public void HidePicker()
        {
            _picker.Hide();
        }

        private void BuildBoard()
        {
            var canvasTransform = GetComponentInParent<Canvas>().transform;

            var boardRoot = CreateUIObject("BoardRoot", canvasTransform);
            boardRoot.sizeDelta = new Vector2(boardSize, boardSize);
            boardRoot.anchorMin = boardRoot.anchorMax = new Vector2(0.5f, 0.5f);
            boardRoot.anchoredPosition = Vector2.zero;

            float boxSize = (boardSize - boxSpacing * 4f) / 3f;
            var outerGrid = boardRoot.gameObject.AddComponent<GridLayoutGroup>();
            outerGrid.cellSize = new Vector2(boxSize, boxSize);
            outerGrid.spacing = new Vector2(boxSpacing, boxSpacing);
            outerGrid.padding = new RectOffset((int)boxSpacing, (int)boxSpacing, (int)boxSpacing, (int)boxSpacing);

            for (int boxRow = 0; boxRow < 3; boxRow++)
            {
                for (int boxCol = 0; boxCol < 3; boxCol++)
                {
                    var boxRect = CreateUIObject($"Box_{boxRow}_{boxCol}", boardRoot);
                    var boxImage = boxRect.gameObject.AddComponent<Image>();
                    boxImage.color = new Color(0.6f, 0.6f, 0.6f);

                    float cellSize = (boxSize - cellSpacing * 4f) / 3f;
                    var innerGrid = boxRect.gameObject.AddComponent<GridLayoutGroup>();
                    innerGrid.cellSize = new Vector2(cellSize, cellSize);
                    innerGrid.spacing = new Vector2(cellSpacing, cellSpacing);
                    innerGrid.padding = new RectOffset((int)cellSpacing, (int)cellSpacing, (int)cellSpacing, (int)cellSpacing);

                    for (int r = 0; r < 3; r++)
                    {
                        for (int c = 0; c < 3; c++)
                        {
                            int row = boxRow * 3 + r;
                            int col = boxCol * 3 + c;
                            _cells[row, col] = BuildCell(boxRect, row, col, cellSize);
                        }
                    }
                }
            }

            BuildBackgroundCatcher(canvasTransform);
        }

        private CellView BuildCell(Transform parent, int row, int col, float cellSizePx)
        {
            var cellRect = CreateUIObject($"Cell_{row}_{col}", parent);
            var background = cellRect.gameObject.AddComponent<Image>();
            background.color = Color.white;

            var valueRect = CreateUIObject("Value", cellRect);
            StretchFull(valueRect);
            var valueText = valueRect.gameObject.AddComponent<Text>();
            ConfigureText(valueText, Mathf.RoundToInt(cellSizePx * 0.55f));

            var marksRect = CreateUIObject("Marks", cellRect);
            StretchFull(marksRect);
            var marksGrid = marksRect.gameObject.AddComponent<GridLayoutGroup>();
            float markCellSize = cellSizePx / 3f;
            marksGrid.cellSize = new Vector2(markCellSize, markCellSize);
            marksGrid.spacing = Vector2.zero;

            var markTexts = new Text[9];
            for (int i = 0; i < 9; i++)
            {
                var markCellRect = CreateUIObject($"Mark_{i + 1}", marksRect);
                var markText = markCellRect.gameObject.AddComponent<Text>();
                ConfigureText(markText, Mathf.RoundToInt(markCellSize * 0.6f));
                markText.gameObject.SetActive(false);
                markTexts[i] = markText;
            }

            var cellView = cellRect.gameObject.AddComponent<CellView>();
            cellView.Initialize(controller, this, row, col, background, valueText, markTexts);
            return cellView;
        }

        private void BuildBackgroundCatcher(Transform canvasTransform)
        {
            var catcherRect = CreateUIObject("BackgroundCatcher", canvasTransform);
            StretchFull(catcherRect);
            var image = catcherRect.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            var catcher = catcherRect.gameObject.AddComponent<BackgroundClickCatcher>();
            catcher.Initialize(controller, this);
            catcherRect.SetAsFirstSibling();
        }

        private void BuildPicker()
        {
            var pickerRect = CreateUIObject("NumberPicker", transform);
            pickerRect.sizeDelta = new Vector2(180, 180);
            pickerRect.anchorMin = pickerRect.anchorMax = new Vector2(0.5f, 0.5f);
            pickerRect.pivot = new Vector2(0.5f, 0.5f);

            var pickerBackground = pickerRect.gameObject.AddComponent<Image>();
            pickerBackground.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

            var grid = pickerRect.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(56, 56);
            grid.spacing = new Vector2(4, 4);
            grid.padding = new RectOffset(4, 4, 4, 4);

            var buttons = new PickerButton[9];
            for (int i = 0; i < 9; i++)
            {
                var buttonRect = CreateUIObject($"Digit_{i + 1}", pickerRect);
                var buttonImage = buttonRect.gameObject.AddComponent<Image>();
                buttonImage.color = Color.white;

                var textRect = CreateUIObject("Label", buttonRect);
                StretchFull(textRect);
                var text = textRect.gameObject.AddComponent<Text>();
                ConfigureText(text, 24);
                text.text = (i + 1).ToString();

                buttons[i] = buttonRect.gameObject.AddComponent<PickerButton>();
            }

            _picker = pickerRect.gameObject.AddComponent<NumberPickerView>();
            _picker.Initialize(controller, buttons);
        }

        private static RectTransform CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ConfigureText(Text text, int fontSize)
        {
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;
            text.raycastTarget = false;
        }
    }
}
```

- [ ] **Step 7: Refresh Unity and check for compile errors**

Run `mcp__UnityMCP__refresh_unity` (`compile: "request"`, `wait_for_ready: true`), then `mcp__UnityMCP__read_console` (or check the refresh result) to confirm there are no compiler errors across the 6 new scripts. Fix any errors before proceeding — do not move to Task 6 with a broken compile.

- [ ] **Step 8: Commit**

```bash
git add Assets/Scripts/UI/CellView.cs Assets/Scripts/UI/CellView.cs.meta \
        Assets/Scripts/UI/PickerButton.cs Assets/Scripts/UI/PickerButton.cs.meta \
        Assets/Scripts/UI/NumberPickerView.cs Assets/Scripts/UI/NumberPickerView.cs.meta \
        Assets/Scripts/UI/BackgroundClickCatcher.cs Assets/Scripts/UI/BackgroundClickCatcher.cs.meta \
        Assets/Scripts/UI/NewGameButtonView.cs Assets/Scripts/UI/NewGameButtonView.cs.meta \
        Assets/Scripts/UI/BoardView.cs Assets/Scripts/UI/BoardView.cs.meta
git commit -m "Add runtime-built UGUI board, picker, and New Game button views"
```

---

### Task 6: Scene wiring in `SampleScene`

**Files:**
- Modify: `Assets/Scenes/SampleScene.unity` (via Unity MCP scene/GameObject tools, then saved)

**Interfaces:**
- Consumes: `Sudoku.Game.SudokuGameController`, `Sudoku.Game.SudokuInput` (Task 4); `Sudoku.UI.BoardView`, `Sudoku.UI.NewGameButtonView` (Task 5).
- Produces: a playable scene — no new code interfaces.

- [ ] **Step 1: Load `SampleScene` and create `GameRoot`**

`mcp__UnityMCP__manage_scene` (`action: "load"`, `scene_path: "Assets/Scenes/SampleScene.unity"`).

`mcp__UnityMCP__manage_gameobject` (`action: "create"`, `name: "GameRoot"`, `components_to_add: ["Sudoku.Game.SudokuGameController", "Sudoku.Game.SudokuInput"]`). If the fully-qualified name fails to resolve, retry with the short names `["SudokuGameController", "SudokuInput"]`.

From the response, record the GameObject's instance ID (`GAME_ROOT_ID`) and the `SudokuGameController` component's instance ID (`CONTROLLER_ID`) — read `mcpforunity://scene/gameobject/{GAME_ROOT_ID}/components` if the create response doesn't already include component IDs.

- [ ] **Step 2: Create the `EventSystem`**

`mcp__UnityMCP__manage_gameobject` (`action: "create"`, `name: "EventSystem"`, `components_to_add: ["UnityEngine.EventSystems.EventSystem", "UnityEngine.InputSystem.UI.InputSystemUIInputModule"]`, or the short names `["EventSystem", "InputSystemUIInputModule"]` if the qualified names don't resolve).

This must be `InputSystemUIInputModule`, not the legacy `StandaloneInputModule` — the project's `activeInputHandler` is set to the new Input System only, so the legacy module cannot process clicks.

- [ ] **Step 3: Create the `Canvas` and attach `BoardView`**

`mcp__UnityMCP__manage_gameobject` (`action: "create"`, `name: "Canvas"`, `components_to_add: ["Canvas", "UnityEngine.UI.CanvasScaler", "UnityEngine.UI.GraphicRaycaster"]`). Record its instance ID as `CANVAS_ID`.

`mcp__UnityMCP__manage_components` (`action: "set_property"`, `target: CANVAS_ID`, `component_type: "CanvasScaler"`, `properties: {"uiScaleMode": "ScaleWithScreenSize", "referenceResolution": {"x": 900, "y": 900}, "matchWidthOrHeight": 0.5}`). If the enum must be numeric, use `1` for `ScaleWithScreenSize`.

`mcp__UnityMCP__manage_components` (`action: "add"`, `target: CANVAS_ID`, `component_type: "Sudoku.UI.BoardView"`).

`mcp__UnityMCP__manage_components` (`action: "set_property"`, `target: CANVAS_ID`, `component_type: "Sudoku.UI.BoardView"`, `property: "controller"`, `value: CONTROLLER_ID`).

- [ ] **Step 4: Create the New Game button**

`mcp__UnityMCP__manage_gameobject` (`action: "create"`, `name: "NewGameButton"`, `parent: "Canvas"`, `components_to_add: ["UnityEngine.UI.Image", "UnityEngine.UI.Button", "Sudoku.UI.NewGameButtonView"]`). Record its instance ID as `BUTTON_ID`.

`mcp__UnityMCP__manage_components` (`action: "set_property"`, `target: BUTTON_ID`, `component_type: "RectTransform"`, `properties: {"sizeDelta": {"x": 160, "y": 50}, "anchoredPosition": {"x": 0, "y": 420}}`).

`mcp__UnityMCP__manage_components` (`action: "set_property"`, `target: BUTTON_ID`, `component_type: "Sudoku.UI.NewGameButtonView"`, `property: "controller"`, `value: CONTROLLER_ID`).

`mcp__UnityMCP__manage_gameobject` (`action: "create"`, `name: "Label"`, `parent: "NewGameButton"`, `components_to_add: ["UnityEngine.UI.Text"]`). Record its instance ID as `LABEL_ID`.

`mcp__UnityMCP__manage_components` (`action: "set_property"`, `target: LABEL_ID`, `component_type: "RectTransform"`, `properties: {"anchorMin": {"x": 0, "y": 0}, "anchorMax": {"x": 1, "y": 1}, "offsetMin": {"x": 0, "y": 0}, "offsetMax": {"x": 0, "y": 0}}`).

`mcp__UnityMCP__manage_components` (`action: "set_property"`, `target: LABEL_ID`, `component_type: "Text"`, `properties: {"text": "New Game", "fontSize": 22, "alignment": "MiddleCenter", "color": {"r": 0, "g": 0, "b": 0, "a": 1}}`).

- [ ] **Step 5: Save the scene**

`mcp__UnityMCP__manage_scene` (`action: "save"`).

- [ ] **Step 6: Commit**

```bash
git add Assets/Scenes/SampleScene.unity
git commit -m "Wire Sudoku controller, input, board view, and New Game button into SampleScene"
```

---

### Task 7: End-to-end verification in Play Mode

**Files:** none (verification only; fix forward in the relevant Task 1-6 file if something is broken, then re-run this task).

- [ ] **Step 1: Enter Play Mode**

`mcp__UnityMCP__manage_editor` (`action: "play"`).

- [ ] **Step 2: Run the integration check**

`mcp__UnityMCP__execute_code` (`action: "execute"`) with:

```csharp
var controller = GameObject.Find("GameRoot").GetComponent<Sudoku.Game.SudokuGameController>();
var results = new System.Collections.Generic.List<string>();

int gr = -1, gc = -1;
for (int r = 0; r < 9 && gr < 0; r++)
    for (int c = 0; c < 9; c++)
        if (controller.Board.IsGiven(r, c)) { gr = r; gc = c; break; }
controller.SelectCell(gr, gc);
results.Add("Given cell blocks selection: " + !controller.HasSelection);

var givenCellGO = GameObject.Find($"Cell_{gr}_{gc}");
var givenImage = givenCellGO.GetComponent<UnityEngine.UI.Image>();
results.Add("Given cell raycast disabled: " + !givenImage.raycastTarget);

int er = -1, ec = -1;
for (int r = 0; r < 9 && er < 0; r++)
    for (int c = 0; c < 9; c++)
        if (!controller.Board.IsGiven(r, c)) { er = r; ec = c; break; }

var cellGO = GameObject.Find($"Cell_{er}_{ec}");
var cellView = cellGO.GetComponent<Sudoku.UI.CellView>();
var boardViewGO = GameObject.Find("Canvas");
var boardView = boardViewGO.GetComponent<Sudoku.UI.BoardView>();

var leftClick = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
    { button = UnityEngine.EventSystems.PointerEventData.InputButton.Left };
cellView.OnPointerClick(leftClick);
results.Add("Click selects empty cell: " + (controller.HasSelection && controller.SelectedRow == er && controller.SelectedCol == ec));

var picker = boardViewGO.GetComponentInChildren<Sudoku.UI.NumberPickerView>(true);
results.Add("Picker shown on cell click: " + picker.gameObject.activeSelf);
results.Add("Picker parented under clicked cell: " + (picker.transform.parent == cellView.transform));

var pickerButtons = picker.GetComponentsInChildren<Sudoku.UI.PickerButton>(true);
var digit5Button = pickerButtons[4];

var rightClick = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
    { button = UnityEngine.EventSystems.PointerEventData.InputButton.Right };
digit5Button.OnPointerClick(rightClick);
results.Add("Right-click on picker digit adds a pencil mark: " + controller.Board.GetCell(er, ec).HasMark(5));

var leftClickDigit = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
    { button = UnityEngine.EventSystems.PointerEventData.InputButton.Left };
digit5Button.OnPointerClick(leftClickDigit);
results.Add("Left-click on picker digit sets value and hides picker: " +
    (controller.Board.GetValue(er, ec) == 5 && !picker.gameObject.activeSelf));

controller.SelectCell(er, ec);
controller.SetValue(9);
results.Add("Re-entering a value overwrites the old one: " + (controller.Board.GetValue(er, ec) == 9));

controller.TogglePencilMark(3);
controller.TogglePencilMark(7);
results.Add("Right-click on a filled cell clears value and starts notes mode: " +
    (controller.Board.GetValue(er, ec) == 0 && controller.Board.GetCell(er, ec).HasMark(3) && controller.Board.GetCell(er, ec).HasMark(7)));

controller.SetValue(4);
results.Add("Entering a value clears existing marks: " +
    (controller.Board.GetValue(er, ec) == 4 && !controller.Board.GetCell(er, ec).HasMark(3) && !controller.Board.GetCell(er, ec).HasMark(7)));

var beforeNewGame = controller.Board;
controller.NewGame();
results.Add("New Game replaces the board: " + (controller.Board != null && controller.Board != beforeNewGame));
results.Add("New Game clears selection: " + !controller.HasSelection);

return string.Join("\n", results);
```

Expected: every line in the returned string ends with `True`. If any line is `False`, treat it as a bug — fix the relevant script from Tasks 4-6, re-run `refresh_unity`, and re-run this check before proceeding.

- [ ] **Step 3: Check the console for errors**

`mcp__UnityMCP__read_console` (`level: "error"`). Expected: no errors logged during the checks above.

- [ ] **Step 4: Exit Play Mode**

`mcp__UnityMCP__manage_editor` (`action: "stop"`).

- [ ] **Step 5: Run the full EditMode suite one more time**

`mcp__UnityMCP__run_tests` (`mode: "EditMode"`, `assembly_names: ["Sudoku.Tests.EditMode"]`), poll via `get_test_job`. Expected: all tests from Tasks 1-4 still PASS.

- [ ] **Step 6: Commit (only if a fix was needed)**

If Step 2 required any fixes, commit them:

```bash
git add -A
git commit -m "Fix issues found during Sudoku foundation end-to-end verification"
```

If no fixes were needed, skip this step — there's nothing to commit.
