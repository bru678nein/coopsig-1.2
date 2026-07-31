# Apply Progress — 001-modulo-asociados

**Mode**: Standard (no strict TDD — this batch introduced the repo's first test runner, MSTest for .NET Framework 4.8, per tasks.md 1.5).
**Delivery**: single PR, `size:exception` accepted by the user (~1,800 forecasted lines vs 800-line review budget). Chain strategy in tasks.md updated from `pending` to `size:exception` accordingly.

## Status

44/45 tasks complete (Phases 1–5 done; only 5.3 "Manual acceptance pass on office PC" is intentionally left unchecked).

## Environment limitation (read before reviewing/building)

This implementation was written on a Mac with no Windows, no Visual Studio, no
`dotnet`/`msbuild`/`mono` toolchain, and no Microsoft Access driver available
(`which dotnet msbuild mono` all resolved to nothing). As a result:

- **No compilation was performed.** Every `.cs`/`.csproj`/`.sln`/`.config` file
  was hand-written and manually reviewed line-by-line for C# syntax and
  WinForms API correctness, but none of it has been run through a compiler.
- **No tests were executed.** `vstest.console.exe` is not available here.
  Tests for 5.1/5.2 are written and believed correct (see manual trace of the
  CUIL check-digit test data below), but are unverified until built on Windows.
- **5.3 (manual acceptance) is unchecked** as instructed — it requires the
  office PC with Office 2016 x64 and cannot run here.
- **First build on the Windows/VS2022 machine is the real gate.** Expect to
  fix at minor compile issues (e.g. a missed `using`, a HintPath that doesn't
  match the exact MSTest NuGet package version VS restores) before green.

Recommended next step: open `CoopSig.sln` in VS2022 on the Windows build
machine, let NuGet restore `CoopSig.Tests`' packages (MSTest.TestFramework /
MSTest.TestAdapter 2.2.10), build in `x64`, then run `sdd-verify`.

## Files Changed (all created — from-scratch implementation)

| File | Lines | What it does |
|---|---|---|
| `CoopSig.sln` | — | Solution, x64-only configs (Debug\|x64, Release\|x64 — no AnyCPU) |
| `CoopSig/CoopSig.csproj` | — | WinForms app project, .NET Framework 4.8, PlatformTarget x64, no NuGet |
| `CoopSig/App.config` | — | `RutaBaseDatos`, `CarpetaBackups`, `CantidadBackupsAConservar` app settings |
| `CoopSig/Properties/AssemblyInfo.cs` | — | Standard assembly metadata |
| `CoopSig/Program.cs` | 54 | Entry point; runs `BackupService` before `FrmPrincipal` opens, non-blocking failure |
| `CoopSig/Config/AppSettings.cs` | 58 | Reads `App.config`; the only thing that differs between the two office installs |
| `CoopSig/Config/ConexionManager.cs` | 87 | Resolves ACE.OLEDB.16.0→12.0 once via `OleDbEnumerator.GetElements()`, caches, exposes `CrearConexion()` |
| `CoopSig/Models/Asociado.cs` | 54 | `Activo` computed from `FechaBaja`; `IdentificadorFiscal` reconstructs CUIL+Documento+Digito |
| `CoopSig/Models/Servicio.cs` | 17 | Catalog entry model |
| `CoopSig/Models/Cargo.cs` | 16 | Catalog entry model |
| `CoopSig/Data/AsociadoRepository.cs` | 263 | `Buscar`, `ObtenerPorDocumento`, `Insertar`, `Actualizar`, `DarDeBaja`, `Reactivar`, `ExisteDocumento`, plus `EsBusquedaNumerica`/`Coincide` classification helpers — all parameterized OleDb, no DELETE |
| `CoopSig/Data/CatalogoRepository.cs` | 67 | `ObtenerServicios`/`ObtenerCargos`, UNION of catalog table with distinct values already used in Asociados |
| `CoopSig/Utils/Validaciones.cs` | 111 | Documento-duplicate lookup, CUIL/Digito coherence (mod-11 algorithm, skipped if empty) |
| `CoopSig/Utils/BackupService.cs` | 97 | Copies `.mdb` to `/Backups/base_AAAAMMDD_HHmmss.mdb`, keeps last N (default 30), never blocks startup |
| `CoopSig/Forms/FrmPrincipal.cs` | 110 | Menu entry point; shows current `.mdb` name (HU-6); Asociados button enabled, Bonos/Anticipos disabled placeholders (out of scope) |
| `CoopSig/Forms/FrmAsociados.cs` | 326 | Search/list screen: padrón loaded in memory once, 300ms debounce, live client-side filter, grid with context menu, keyboard-only flow |
| `CoopSig/Forms/FrmAsociadoDetalle.cs` | 459 | Alta/edición form: Enter-to-advance, Escape-to-cancel, Servicio/Cargo combos (no free text), duplicate-Documento guard, Baja/Reactivar |
| `CoopSig.Tests/CoopSig.Tests.csproj` | — | MSTest v2 (NuGet, dev-only) test project referencing `CoopSig.csproj` |
| `CoopSig.Tests/packages.config` | — | MSTest.TestFramework / MSTest.TestAdapter 2.2.10 |
| `CoopSig.Tests/App.config` | — | Points `RutaBaseDatos` at `TestData\base_test.mdb` for integration tests |
| `CoopSig.Tests/ValidacionesTests.cs` | 64 | 7 tests: CUIL empty/partial/coherent/incoherent, required-field check |
| `CoopSig.Tests/AsociadoRepositoryTests.cs` | 111 | 7 pure-logic tests (`EsBusquedaNumerica`, `Coincide` — no DB needed) + 2 DB-integration tests that `Assert.Inconclusive` if `TestData\base_test.mdb` is absent |
| `CoopSig.Tests/TestData/LEEME.txt` | — | Explains why no real `.mdb` fixture ships in the repo and what the Windows developer must supply |

## Deviations from Design

1. **`DarDeBaja(long doc)` signature**: plan.md lists `DarDeBaja(long doc)` with no date parameter, so the repository sets `FechaBaja = DateTime.Today` internally rather than accepting a caller-supplied date. This matches plan.md's exact signature literally.
2. **No standalone `ObtenerTodos()` method was added.** Plan.md's Performance section says the padrón is loaded into memory once and filtered client-side; this is achieved by calling the already-specified `Buscar(string.Empty, incluirBajas: true)` rather than adding an extra repository method beyond the ones plan.md explicitly lists.
3. **`Documento` is read-only once a record exists** (`FrmAsociadoDetalle` disables the field in edit mode). Not explicitly stated in spec/plan, but necessary: `Actualizar`/`DarDeBaja`/`Reactivar` all key off `Documento`, so allowing it to change mid-edit would silently orphan the WHERE clause. Documented here per the "note deviations, don't silently freelance" rule.
4. **Confirmation dialog on Baja.** Not required by spec.md's HU-4 acceptance criteria, but added (`MessageBox.Show(..., YesNo)`) since baja is a state-changing action reversible only via a second explicit action; kept it minimal (one Yes/No dialog) to avoid violating Constitution V's "fewer clicks" intent while still avoiding accidental bajas.
5. **Field assumptions beyond plan.md's explicit mapping table.** plan.md's "Mapeo al esquema existente" table only enumerates `Documento`, `CUIL`+`Digito`, `FechaBaja`, `Servicio`, `Cargo`. `Apellido`, `Nombre`, and `FechaIngreso` are confirmed by name elsewhere in spec.md/tasks.md and were used as-is. **No other columns (e.g. Domicilio, Teléfono, Observaciones, FechaNacimiento) were assumed or referenced** — those only appear in the superseded draft `docs/relevamiento-base-actual.md`, which plan.md explicitly supersedes and which itself says "el esquema real de la base existente todavía no fue relevado." This repo has no `.mdb` file and no way to inspect the real schema from a Mac. **Risk**: if the real `Asociados` table uses different exact column names/casing than `Documento, Apellido, Nombre, CUIL, Digito, Servicio, Cargo, FechaIngreso, FechaBaja`, every SQL statement in `AsociadoRepository`/`CatalogoRepository` will fail at runtime with an Access "column not found" error until corrected. This should be the very first thing verified on the Windows machine (plan.md's own "Verificación previa a implementar" step 1–3 says exactly this).
6. **CUIL check-digit test fixture**: `ValidacionesTests.EsDigitoVerificadorValido_CoherenteConocido_EsVerdadero` uses `(cuil: 20, documento: 30123456, digito: 3)`. I hand-computed this via the mod-11 algorithm (weights 5,4,3,2,7,6,5,4,3,2 over "2030123456", sum=96, 96 mod 11=8, 11-8=3) to make sure the "known good" test case is actually correct and not just asserting whatever the code under test happens to produce. Worth a second look on the Windows machine when the test actually runs.

## Issues Found

None beyond what's listed in Deviations above.

## Remaining Tasks

- [ ] 5.3 Manual acceptance pass on office PC (x64, Office 2016) — requires the Windows build machine; cannot run from this Mac session.

## Work Unit Evidence

| Evidence | Value |
|---|---|
| Focused test command and exact result | Not executed — no .NET tooling on this Mac (`dotnet`/`msbuild`/`mono` all absent). Command for the Windows machine: `vstest.console.exe CoopSig.Tests\bin\x64\Debug\CoopSig.Tests.dll`. Tests written: 7 in `ValidacionesTests`, 9 in `AsociadoRepositoryTests` (7 pure-logic + 2 DB-integration guarded by `Assert.Inconclusive` when `TestData\base_test.mdb` is missing). |
| Runtime harness command/scenario and exact result | Not executed — requires Windows + Office 2016 x64 + a copied `.mdb` (per plan.md, never the original). This is task 5.3, explicitly left unchecked. |
| Rollback boundary | Entire `CoopSig/` and `CoopSig.Tests/` trees plus `CoopSig.sln` are new, unreferenced by anything else in the repo (docs/specs only). The whole PR is revertible as a single unit with no partial-state risk. |

## Workload / PR Boundary

- Mode: single PR, `size:exception`
- Current work unit: entire Módulo Asociados (Phases 1–5, HU-1 through HU-6)
- Boundary: from empty repo to a buildable (pending Windows verification) WinForms app covering all in-scope HUs, plus its first test project
- Estimated review budget impact: ~2,155 lines across 23 new files (`.cs`/`.csproj`/`.config`/`.sln`), vs the 800-line session budget — accepted exception per user instruction, not re-litigated here

## Status

44/45 tasks complete. Ready for `sdd-verify`, with the explicit caveat that verification on this Mac cannot include compilation, test execution, or manual acceptance — those require the Windows/VS2022/Office 2016 x64 machine described in plan.md's "Entorno de desarrollo" section.
