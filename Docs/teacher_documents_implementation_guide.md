# CMSApp — Teacher Documents (UI)

**What shipped (2026-07-13)**: uploadable verification documents on teachers — citizenship, ID card, PAN card, passport, driving license, police report, academic certificates, appointment letters. Files (PDF or JPG/JPEG/PNG) are stored server-side; each document row carries a display name, its type (Config catalog), and an optional **validity/expiry date** for documents that expire (driving license, police report).

Fits the teacher profile's tab layout: **Qualifications | Class Assignments | Documents** — each tab is its own existing endpoint set (`/{id}/qualifications`, `/{id}/assignments`, `/{id}/documents`).

> ⚠️ **Migration required**: new table `dbo.teacher_documents` (user-owned migration, same wave as the class/section restructure). Also ensure the API host can write to the configured upload folder.

## Document type catalog (`typeCode 1006`)

Populate the type dropdown from the existing endpoint — options are seeded:

```
GET /api/configs/dropdown/1006
```

Seeded codes: `CITIZENSHIP`, `ID_CARD`, `PAN_CARD`, `PASSPORT`, `DRIVING_LICENSE`, `POLICE_REPORT`, `ACADEMIC_CERTIFICATE`, `APPOINTMENT_LETTER`, `OTHER`. Admins can add more via `POST /api/configs`. Suggested UI hint: show the "Valid until" field prominently for license/report types (the catalog's `additionalValue1` can be set to `"Y"` on types that expire).

## Endpoints — `/api/teachers/{id}/documents`

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

`TEACHER_DOCUMENT_UPLOAD`, `TEACHER_DOCUMENT_LIST`, `TEACHER_DOCUMENT_DOWNLOAD`, `TEACHER_DOCUMENT_DELETE` — under `TEACHER_MANAGEMENT`, grant to other roles via `POST /api/roles/claims`.

## Backend notes

- Files land under the folder from `FileStorage:RootPath` (default `Uploads/` beside the API, subfolder `teacher-documents/{teacherId}/`), stored under generated GUID names — user filenames never touch the filesystem. The DTO never exposes the storage path.
- The storage abstraction (`IFileStorageService` / `LocalFileStorageService`) is reusable — a future `student_documents` table can ride the same service and rules (extensions/size in `DocumentFileRules`).
- Upload limits are a code-level security boundary (10 MB, four extensions), not configuration.
