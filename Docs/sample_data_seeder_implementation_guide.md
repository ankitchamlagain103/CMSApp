# Sample Data Seeder — Development Dataset Reference

**Audience:** UI team / anyone developing against a locally seeded database.
**Source:** `Infrastructure/Persistence/DataSeeder/SampleDataSeeder.cs`, called last in the `Program.cs` seeder chain. Development/demo data only — remove that call for a production deployment.

This is not an API feature — no new endpoints. It documents what data exists in a freshly seeded development database so screens can be built and tested against realistic content.

## Seeder chain (runs on every startup, in this order)

| Seeder | What it guarantees |
|---|---|
| `IdentitySeeder` | `SuperAdmin`/`Admin`/`User` roles + one login per role (credentials in `appsettings.json` → `Seed:*`) |
| `MenuSeeder` | The full menu/permission catalog (see below) + every endpoint-backed menu granted to SuperAdmin |
| `AppConfigSeeder` | 17 baseline app settings (`GENERAL`/`THEME`/`ANNOUNCEMENT`) |
| `ConfigCatalogSeeder` | Config *types* 1001–1007 + guardian-relationship, teacher-qualification and document-type options |
| `SampleDataSeeder` | Everything below |

All seeders are idempotent create-if-missing (`MenuSeeder` additionally *syncs* — see below). Sample data is never updated or deleted once created, so manual edits survive restarts.

## Menu catalog shape (MenuSeeder)

Three levels, all `MenuFor = ADMIN`:

- `MAIN_MENU` — nav area, no endpoint: `DASHBOARD` (has `Url` `/dashboard/analytics`), `USER_MANAGEMENT`, `CONFIG_MANAGEMENT` ("Master Settings"), `ACADEMIC_MANAGEMENT`, `TEACHER_MANAGEMENT`, `STUDENT_MANAGEMENT`.
- `SUB_MENU` — a feature's **list page**: visible, carries the frontend `Url` *and* the list endpoint (`Controller`/`Action`), so it doubles as the permission for that endpoint. E.g. `STUDENT_LIST` → `/apps/student/list` + `Students/GetStudents`.
- `PERMISSION` — hidden authorization record for every other endpoint, parented under its feature's list `SUB_MENU` (Dashboard's four read permissions hang directly off the `DASHBOARD` main menu).

**The catalog is synced, not just created**: on startup, any drifted row (hierarchy, url, route, visibility) is reset to the definition in `MenuSeeder`, and a soft-deleted catalog row is resurrected. Menu changes belong in that file, not in hand edits.

## Sample dataset (SampleDataSeeder)

### Config catalog options

- **Grade (1001):** `NURSERY`, `LKG`, `UKG`, `ONE` … `TWELVE` (15 options).
- **Section (1002):** `A`, `B`, `C`.
- **Subject (1003):** 36 options — the union of the Nepali school curriculum (core subjects, pre-primary activities, secondary optionals, +2 faculty subjects like `PHYSICS`, `ACCOUNTANCY`, `SOCIOLOGY`, `HOTEL_MANAGEMENT`).

### Academic structure (current academic year)

- Uses the year flagged `IsCurrent` (falls back to the latest year; creates `AY2083` if none exist).
- **15 classes** — one per grade, Nursery through Twelve.
- **Sections A and B** per class, capacity 30 each.
- **Class-subject mapping** per the common Nepali curriculum, class-wide rows:
  - Nursery/LKG/UKG — English, Nepali, Maths + pre-primary activities (Rhymes, Drawing, Music, …)
  - 1–2: + Our Surroundings, Creative Arts, HPE · 3–5: + Science & Tech, Social Studies
  - 6–8: Science & Tech, Social Studies & Human Values, HPCA, Local Subject
  - 9–10: core six **mandatory** + `COMPUTER_SCIENCE`/`OPT_MATHS`/`OPT_ACCOUNT` as **optional**
  - 11: English, Nepali, Social Studies & Life Skills mandatory + 11 faculty subjects optional · 12: English mandatory + the same faculty optionals

### Teachers — 20 records

- `EmployeeNo` `EMP2026001`–`EMP2026020`, Nepali names, `@cmsapp.local` emails, joining dates spread 2015–2024.
- One qualification each (Masters/Bachelors/PhD mix, TU/KU/PU/Purbanchal institutions).
- Every class-wide subject row has a teacher assigned round-robin; each class's section A also gets a **class teacher**.

### Students — 100 records, with guardians and enrollments

- `AdmissionNo` `ADM2026101`–`ADM2026200`, alternating male/female, DOB consistent with grade age (Nursery ≈ 3–4y … Twelve ≈ 17–18y).
- **50 families**: two consecutive students share a surname and their **father + mother guardian records** (guardian emails `father.<surname><nn>@cmsapp.local`); father is the primary guardian.
- Every student is **enrolled** in their grade's section A or B for the current year, with sequential per-section roll numbers (existing rows respected; section capacity honored).
- **Electives:** grade 9/10 students pick one optional subject; grade 11/12 students get a 3-subject faculty set (Science / Management / Humanities, rotating).

## Typical UI uses

- Log in as the seeded `superadmin` and every list page has multiple pages of data (students list, teacher list, guardian search, enrollment per section).
- Student detail shows `CurrentEnrollment` (with subjects + teacher names) and guardians; teacher detail shows qualifications and `ServiceHistory`-style assignments.
- Class detail per grade shows the full curriculum, with mandatory/optional split for grades 9–12.

## Failure behavior

The whole seeder chain runs inside the startup try/catch: on an unmigrated or unreachable database it logs a warning and the app starts anyway. A partially deleted sample dataset is re-completed on the next boot (missing records recreated by natural key); soft-deleted records keep their keys reserved and are *not* resurrected (except menu catalog rows, which are).
