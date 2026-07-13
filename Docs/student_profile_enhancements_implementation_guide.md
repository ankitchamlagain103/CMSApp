# CMSApp — Student Profile Enhancements (UI)

**What shipped (2026-07-13)**: two additions to the existing student endpoints — no new routes.

1. `GET /api/students/{id}` now returns a **`currentEnrollment`** block: the student's current class/section/year, roll number, and the subjects they are studying.
2. `PUT /api/students/{id}` now accepts a **`guardians`** list with the same entry shape as the create form, so the profile's guardian table can be edited through the normal update call.

## 1. `currentEnrollment` on the student detail

```json
{
  "id": "…", "admissionNo": "ADM2026001", "firstName": "Ankit", "...": "...",
  "guardians": [ { "id": "<linkId>", "guardianFirstName": "Test", "relationshipCode": "FATHER", "isPrimary": true, "...": "..." } ],
  "currentEnrollment": {
    "enrollmentId": "…",
    "academicYearId": "…", "academicYearCode": "AY2026", "academicYearName": "2026-2027",
    "academicClassId": "…", "gradeCode": "LKG",
    "classSectionId": "…", "sectionCode": "SECTION_A",
    "rollNumber": "1", "enrollmentDate": "2026-07-13",
    "subjects": [
      { "classSubjectId": "…", "subjectCode": "NEPALI",  "isMandatory": true,  "displayOrder": 1 },
      { "classSubjectId": "…", "subjectCode": "DANCE",   "isMandatory": false, "displayOrder": 9 }
    ]
  }
}
```

- **Which enrollment is "current"?** The active (`status: 1` Enrolled) one, preferring the academic year flagged `isCurrent`; if none is current, the latest year by start date. `currentEnrollment` is `null` when the student has no active enrollment — show an "Enroll" call-to-action then.
- **`subjects` = subjects studying**: every mandatory subject the student's section sees (class-wide) **plus** the electives chosen on the enrollment (`isMandatory: false` rows). Optional subjects the student did *not* pick are excluded.
- Codes resolve to labels from cached dropdown data (grades 1001, sections 1002, subjects 1003).
- The paged list (`GET /api/students`) does **not** include `currentEnrollment` or `guardians` — detail call only.

Suggested profile layout: header card (photo/name/status) → info card → **Current Class** card (year, grade/section, roll no, subject chips) → Guardians table.

## 2. Guardians on `PUT /api/students/{id}`

Same field set as before plus `guardians`, with **three-way semantics** (mirrors `roleIds` on user update):

| `guardians` value | Effect |
|---|---|
| field omitted / `null` | Guardian links untouched |
| `[]` | Every guardian link removed |
| non-empty list | **Replace-sync**: the list becomes the full set |

Entry shape is identical to the create form — either `{ "guardianId": "…", "relationshipCode": "FATHER", "isPrimary": true }` for an existing guardian, or inline `{ "firstName", "lastName", "phone", …, "relationshipCode", "isPrimary" }` to create one. Rules: at most one `isPrimary: true` (`400`), unknown `guardianId` → `404`, duplicate `guardianId` in the list → `400`; all-or-nothing.

Replace-sync details worth knowing in the UI:

- A listed `guardianId` that's **already linked** keeps its link — `relationshipCode`/`isPrimary` are updated in place.
- A linked guardian **missing from the list** is unlinked (the guardian record itself survives — it may belong to a sibling).
- **Always send `guardianId` for guardians already shown on the profile** — an inline entry always creates a *new* guardian record, so resubmitting an existing guardian inline would duplicate them.
- The update response returns the refreshed `guardians` list (same shape as the detail call).

The standalone link/unlink endpoints (`POST/DELETE /api/students/{id}/guardians…`) still exist and are fine for single-row actions like the profile's delete icon.

No migration needed for either change; no new permissions (rides on `STUDENT_DETAIL`/`STUDENT_UPDATE`).
