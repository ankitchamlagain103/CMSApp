# CMSApp — Teacher & Student Documents (UI)

**What shipped (2026-07-13)**: uploadable documents on teachers **and students**. Files (PDF or JPG/JPEG/PNG) are stored server-side; each document row carries a display name, its type (Config catalog), and an optional **validity/expiry date** for documents that expire (driving license, police report).

- **Teacher documents** — citizenship, ID card, PAN card, passport, driving license, police report, academic certificates, appointment letters. Tab layout: **Qualifications | Class Assignments | Documents**.
- **Student documents** (same API shape under `/api/students/{id}/documents`) — birth certificate, transfer certificate, character certificate, previous marksheet, citizenship, passport, photo, immunization record, disability card, migration certificate, guardian's citizenship copy. Fits as a **Documents** tab beside Current Class / Guardians / History.

> ⚠️ **Migration required**: new tables `dbo.teacher_documents` and `dbo.student_documents` (user-owned migration, same wave as the class/section restructure). Also ensure the API host can write to the configured upload folder.

## Document type catalogs (two separate dropdowns)

| Audience | `typeCode` | Seeded codes |
|---|---|---|
| Teacher | `1006` | `CITIZENSHIP`, `ID_CARD`, `PAN_CARD`, `PASSPORT`, `DRIVING_LICENSE`, `POLICE_REPORT`, `ACADEMIC_CERTIFICATE`, `APPOINTMENT_LETTER`, `OTHER` |
| Student | `1007` | `BIRTH_CERTIFICATE`, `TRANSFER_CERTIFICATE`, `CHARACTER_CERTIFICATE`, `PREVIOUS_MARKSHEET`, `CITIZENSHIP`, `PASSPORT`, `PHOTO`, `IMMUNIZATION_RECORD`, `DISABILITY_CARD`, `MIGRATION_CERTIFICATE`, `GUARDIAN_CITIZENSHIP`, `OTHER` |

Populate from `GET /api/configs/dropdown/{typeCode}`; admins add more via `POST /api/configs`. Deliberately two catalogs so a student form never offers "Appointment Letter" and a teacher form never offers "Birth Certificate". Suggested UI hint: show the "Valid until" field prominently for expiring types (the catalog's `additionalValue1` can be set to `"Y"` on those).

## Endpoints — `/api/teachers/{id}/documents` and `/api/students/{id}/documents`

Both resources expose the **identical four endpoints** — everything below is written for teachers; substitute `students` (and catalog `1007`, `StudentDocumentDto`) for the student version.

### Upload — `POST /api/teachers/{id}/documents` (**multipart/form-data**, not JSON)

| Form field | Required | Notes |
|---|---|---|
| `file` | ✅ | PDF/JPG/JPEG/PNG only, max **10 MB** |
| `documentTypeCode` | ✅ | Catalog 1006 code |
| `documentName` | ✅ | Display name, ≤150 chars (e.g. "Driving License — B category") |
| `validUntil` | ❌ | Expiry date (`yyyy-MM-dd`) for license/report-type documents |
| `remarks` | ❌ | ≤500 chars |

Failures: `400 VALIDATION_ERROR` (no file / bad extension / >10 MB / unknown type code / missing name), `404` unknown teacher.

Response `data` — `TeacherDocumentDto`:

```json
{
  "id": "…", "teacherId": "…",
  "documentTypeCode": "DRIVING_LICENSE", "documentName": "Driving License — B category",
  "fileName": "license-scan.pdf", "contentType": "application/pdf", "fileSizeBytes": 482133,
  "validUntil": "2028-03-01", "remarks": null,
  "uploadedTs": "2026-07-13T09:15:00+00:00"
}
```

### List — `GET /api/teachers/{id}/documents`

Returns `TeacherDocumentDto[]` (no file bytes). UI tip: flag rows where `validUntil` is past (expired) or within ~30 days (expiring soon).

### Download — `GET /api/teachers/{id}/documents/{documentId}/download`

Streams the **raw file** (correct `Content-Type`, original filename in `Content-Disposition`) — this is the one endpoint that does *not* return the JSON envelope on success; errors (`404`) still do. Open in a new tab or fetch as blob. The token still applies (permission `TEACHER_DOCUMENT_DOWNLOAD`), so plain `<a href>` needs the Authorization header via fetch/blob.

### Delete — `DELETE /api/teachers/{id}/documents/{documentId}`

Hard delete: removes the row **and** the stored file.

## Permissions (seeded to SuperAdmin)

`TEACHER_DOCUMENT_UPLOAD/LIST/DOWNLOAD/DELETE` (under `TEACHER_MANAGEMENT`) and `STUDENT_DOCUMENT_UPLOAD/LIST/DOWNLOAD/DELETE` (under `STUDENT_MANAGEMENT`) — grant to other roles via `POST /api/roles/claims`.

## Backend notes

- Files land under the folder from `FileStorage:RootPath` (default `Uploads/` beside the API, subfolders `teacher-documents/{teacherId}/` and `student-documents/{studentId}/`), stored under generated GUID names — user filenames never touch the filesystem. The DTOs never expose the storage path.
- Both resources share one storage abstraction (`IFileStorageService` / `LocalFileStorageService`) and one rule set (`DocumentFileRules`).
- Upload limits are a code-level security boundary (10 MB, four extensions), not configuration.
