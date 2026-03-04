# XEmuera Kernel Update Plan

## Overview

This document describes the plan for updating the XEmuera kernel from **Emuera1824+v15** to **Emuera1824+v24**.

XEmuera is an Android port of the Emuera Windows text-game engine, built with Xamarin.Forms. The Emuera kernel used in XEmuera is a modified (私家改造版) version that includes bug fixes beyond the original Emuera1824 release.

**Original author credit**: [Fegelein](https://github.com/Fegelein21/XEmuera) — the original creator of XEmuera and maintainer of the base project.

---

## Background

The upstream 私家改造版 (custom fix edition) of Emuera has released versions v15 through v24, each containing targeted bug fixes. XEmuera was based on v15 and accumulated XEmuera-specific adaptations (replacing Windows Forms APIs with Xamarin equivalents), but never received the v16–v24 upstream fixes.

The zip file `Emuera1824+v24.7z` contains:
- The v24 binary (`Emuera1824+v24.exe`)
- The v24 source code (`src1824+v24.7z`)
- Changelog (`私家改造版Emuera_readme.txt`)

---

## Update Strategy

### Approach: Cherry-Pick Bug Fixes

Because the XEmuera codebase contains significant platform-specific modifications (replacing `System.Windows.Forms`, `System.Drawing`, etc. with SkiaSharp/Xamarin equivalents), we **cannot** replace files wholesale. Instead, we apply each individual bug fix from v16–v24 to the existing XEmuera code.

### Files to Modify

| File | Bug Fix | Version |
|------|---------|---------|
| `GameData/Function/Creator.Method.cs` | Fix NOSAMES implementation | v16 |
| `GameData/ConstantData.cs` | Add ISASSI/助手 CSV case | v16 |
| `GameData/Expression/OperatorMethod.cs` | Add Int64.MinValue negation warning | v16 |
| `GameData/Variable/VariableEvaluator.cs` | Add ARRAYSORT range check | v19 |
| `GameView/EmueraConsole.cs` | Fix INPUTS "@" single char handling | v20 |
| `GameData/ConstantData.cs` | Add CALLNAME/NICKNAME/MASTERNAME cases in TryKeywordToInteger | v22/v23 |
| `GameData/ConstantData.cs` | Populate relationDic with Mastername | v22/v23 |
| `GameData/Function/Creator.cs` | GETNUMB uses GetnumBMethod | v22 |
| `Sub/LexicalAnalyzer.cs` | Add NumericCheck method | v24 |
| `GameData/Function/Creator.Method.cs` | Use NumericCheck in IsNumericMethod | v24 |

---

## Bug Fix Details (v16–v24)

### v16
- **NOSAMES**: The `NosamesMethod` was completely wrong — it used `Distinct()` on all arguments rather than comparing the first argument against all others.
- **ISASSI/助手**: Reading character CSV files would error if ISASSI or 助手 was used as a key; now those are silently skipped.
- **Integer MinValue**: Added a warning when negating `long.MinValue` since the result doesn't change.

### v17
- **GDRAWSPRITE**: Fixed unexpected display when enlarging a BMP-sourced image. *(Windows-specific graphics, minimal impact on Android.)*

### v18
- **CUSTOMDRAWLINE**: Optimized to avoid redundant computation.

### v19
- **ARRAYSORT**: Fixed missing range validation for the 3rd and 4th arguments (start + count exceeding array length).

### v20
- **INPUTS "@"**: When INPUTS received a single "@" character, it was incorrectly treated as a debug command prefix. Added `str.Length > 1` guard.

### v21
- **Debug Console**: Fixed exception when debug console reached 100 lines. *(Debug-mode only.)*

### v22
- **CALLNAME**: Some processes couldn't correctly resolve CALLNAME — added CALLNAME/NICKNAME/MASTERNAME fall-through cases in `TryKeywordToInteger`.
- **GETNUMB**: Was incorrectly mapped to `GetnumMethod` (same as GETNUM) instead of `GetnumBMethod`.

### v23
- **NICKNAME/MASTERNAME in GETNUM/GETNUMB**: Fixed by also populating `relationDic` with Mastername entries when loading character templates.

### v24
- **ISNUMERIC**: Using `ReadInt64` for the numeric check caused CodeEE for some strings that should simply return false. Replaced with a dedicated `NumericCheck` method in `LexicalAnalyzer`.

---

## Non-Applied Changes

The following upstream changes were **not** applied because they concern Windows-specific features that are either already handled differently in XEmuera or not applicable to Android:

- Windows Forms UI changes (MainWindow, ConfigDialog, DebugDialog, etc.)
- GDI/WinInput/WinmmTimer changes
- `System.Media.SystemSounds` calls (already commented out in XEmuera)
- `System.Windows.Forms.Application.DoEvents()` calls (already replaced with `App.DoEvents()`)
- Settings/Properties files specific to the Windows build
- `GlobalSuppressions.cs` (Windows build suppression rules)

---

## README Update

The README.md is updated from Chinese to English, includes:
- Project description
- Usage instructions
- Download link
- Known issues
- Changelog (v16–v24)
- Credits to the original author (Fegelein)
