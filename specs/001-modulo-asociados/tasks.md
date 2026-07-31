# Tasks: Módulo Asociados (Feature 001)

## Review Workload Forecast

| Field | Value |
|---|---|
| Estimated changed lines | ~1,800 total; largest single work unit ~700 (PR2 — Data layer) |
| 400-line budget risk | High (budget for this session is 800; a single-PR delivery would run ~2.25x it) |
| Chained PRs recommended | Yes |
| Suggested split | PR1 → PR2 → PR3 → PR4 |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

Decision needed before apply: Resolved — single PR, `size:exception` accepted by maintainer (see apply-progress.md).
Chained PRs recommended: Yes (not taken — maintainer chose single-PR exception)
Chain strategy: size:exception (single PR, ~1,800 lines vs 800-line budget)
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|---|---|---|---|---|---|
| 1 | Buildable skeleton connects to a copied `.mdb` (Config+Models+test project) | PR 1 | `vstest.console.exe CoopSig.Tests.dll` | Manual run on Windows PC against a copied `.mdb`; verify `CrearConexion()` opens | Whole PR removable, no consumers yet |
| 2 | CRUD data layer, no UI (Repositories+Validaciones+BackupService) | PR 2 | `vstest.console.exe CoopSig.Tests.dll /Tests:AsociadoRepositoryTests,ValidacionesTests` | Console harness calling repository/backup methods against copied `.mdb` | Classes unused until Forms wired; revertible independently |
| 3 | HU-1 usable end-to-end (search/list) | PR 3 | N/A — WinForms UI has no unit-test surface | Run app on office PC; search by Documento/Apellido, verify <1s and grid columns | Forms removable; repositories unaffected |
| 4 | HU-2/3/4/5 usable end-to-end (detail form) | PR 4 | N/A — WinForms UI has no unit-test surface | Run app on office PC; create/edit/baja/reactivar, verify keyboard flow | Only detail form removable; search screen keeps working standalone |

## Phase 1: Foundation (PR 1)

- [x] 1.1 Create `CoopSig.sln` + `CoopSig.csproj`, .NET Framework 4.8, platform x64 only. [IV, VIII]
- [x] 1.2 Add `App.config` with configurable `.mdb` path connection string. [VIII]
- [x] 1.3 Create `/Config/ConexionManager.cs`: resolve ACE.OLEDB.16.0 then 12.0 once, cache, expose `CrearConexion()`. [IV]
- [x] 1.4 Create `/Models/Asociado.cs`, `Servicio.cs`, `Cargo.cs`; `Activo` computed from `FechaBaja`. [Mapeo]
- [x] 1.5 Create `CoopSig.Tests` (MSTest, .NET FW 4.8) — first test runner in the repo.

## Phase 2: Data Layer (PR 2)

- [x] 2.1 `/Data/AsociadoRepository.cs`: `Buscar(texto, incluirBajas)` — numeric→Documento prefix, alpha→Apellido/Nombre, parameterized OleDb only. [HU-1; I, VI]
- [x] 2.2 Add `ObtenerPorDocumento`, `Insertar`, `Actualizar`. [HU-2, HU-3; I]
- [x] 2.3 Add `DarDeBaja(doc)` (writes `FechaBaja`) and `Reactivar(doc)` (clears it) — never `DELETE`. [HU-4; II]
- [x] 2.4 Add `ExisteDocumento(doc)`. [HU-3; VI]
- [x] 2.5 `/Data/CatalogoRepository.cs`: reads Servicio/Cargo, unions catalog with distinct values already used in Asociados. [Mapeo]
- [x] 2.6 `/Utils/Validaciones.cs`: Documento uniqueness, optional CUIL/Digito coherence (skip if empty). [HU-3; VI]
- [x] 2.7 `/Utils/BackupService.cs`: copy `.mdb` to `/Backups/base_AAAAMMDD_HHmmss.mdb` before `FrmPrincipal` opens; keep last 30; failure is non-blocking with clear message. [HU-6; VII]

## Phase 3: UI — Search & Navigation (PR 3)

- [x] 3.1 `/Forms/FrmPrincipal.cs`: menu entry point, runs `BackupService` on startup, shows current `.mdb` name. [HU-6; V]
- [x] 3.2 `/Forms/FrmAsociados.cs`: load padrón in memory, auto-focused search field, 300ms debounce, live filter. [HU-1; V]
- [x] 3.3 Grid columns (Apellido, Nombre, Documento, Servicio, Estado) + "incluir bajas" toggle (default off). [HU-1, HU-4]
- [x] 3.4 Keyboard-only flow: open ficha from grid via Enter. [HU-5; V]

## Phase 4: UI — Detail Form (PR 4)

- [x] 4.1 `/Forms/FrmAsociadoDetalle.cs`: all Asociado fields; required (Apellido, Nombre, Documento, Servicio) vs optional marked. [HU-2, HU-3]
- [x] 4.2 Bind Servicio to catalog combo (no free text); `FechaIngreso` defaults to today, editable. [HU-3]
- [x] 4.3 Enter-to-advance / Escape-to-cancel, tab order matches visual layout. [HU-5; V]
- [x] 4.4 Guardar: validate via `Validaciones`, block duplicate Documento naming the existing owner, persist, refresh grid. [HU-2, HU-3; VI]
- [x] 4.5 Dar de baja / Reactivar actions from ficha and grid context menu. [HU-4; II]
- [x] 4.6 Plain-language error messages, no codes/jargon. [V]

## Phase 5: Verification

- [x] 5.1 Unit tests for `Validaciones` (uniqueness, CUIL coherence, empty-CUIL allowed). [HU-3]
- [x] 5.2 Unit tests for `AsociadoRepository.Buscar` classification, against a copied test `.mdb`, never the original. [HU-1]
- [ ] 5.3 Manual acceptance pass on office PC (x64, Office 2016) against HU-1..HU-6 before merge. [All HU; IV]
