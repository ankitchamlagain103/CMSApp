# Student Management System: Exam, Assessment Configuration, Result Processing & Student Promotion Design

## Executive Summary & Scope

This document provides a comprehensive technical design specification for the **Exam, Assessment Configuration, Result Processing, and Student Promotion** modules of the Student Management System (SMS). 

It builds upon existing core entities within the system:
* **AcademicYear**: Defines academic sessions/terms.
* **AcademicClass**: Represents grade/class levels (e.g., Grade 5, Grade 6).
* **Student**: Represents the permanent personal identity of a student.
* **Enrollment**: Captures the temporal, class-specific snapshot of a student within a specific academic year and section.

This design supersedes previous iterations by integrating granular subject-level assessment configurations (theory/practical split, credit hours, grace marks) with robust exam scheduling, marks recording, result calculation, and immutable promotion workflows.

---

## 1. Assessment Configuration Module

To accommodate diverse curricula, evaluation rules are configured at the **ClassSubject** level. This defines how each subject is evaluated across an entire academic year.

### 1.1 ClassSubject Assessment Configuration Entity

| Column | Data Type | Nullable | Description |
| :--- | :--- | :--- | :--- |
| `CreditHours` | `decimal(4,2)` | No | Academic credit hours assigned to the subject |
| `HasTheory` | `bool` | No | Flag indicating whether theory assessment is enabled |
| `HasPractical` | `bool` | No | Flag indicating whether practical assessment is enabled |
| `TheoryFullMarks` | `decimal(5,2)` | Yes | Maximum marks allocated for theory evaluation |
| `TheoryPassMarks` | `decimal(5,2)` | Yes | Minimum pass marks required in theory |
| `PracticalFullMarks` | `decimal(5,2)` | Yes | Maximum marks allocated for practical evaluation |
| `PracticalPassMarks` | `decimal(5,2)` | Yes | Minimum pass marks required in practical |
| `OverallFullMarks` | `decimal(5,2)` | No | Total full marks (`TheoryFullMarks` + `PracticalFullMarks`) |
| `OverallPassMarks` | `decimal(5,2)` | No | Overall minimum pass marks required across the subject |

### 1.2 Configuration Examples

#### Example A: Pure Theory Subject (e.g., Mathematics)
* **Credit Hours**: 5.0
* **HasTheory**: `true` | **Theory Marks**: 100.00 / 40.00
* **HasPractical**: `false` | **Practical Marks**: N/A
* **Overall**: 100.00 Full / 40.00 Pass

#### Example B: Combined Subject (e.g., Computer Science)
* **Credit Hours**: 4.0
* **HasTheory**: `true` | **Theory Marks**: 75.00 / 27.00
* **HasPractical**: `true` | **Practical Marks**: 25.00 / 10.00
* **Overall**: 100.00 Full / 37.00 Pass

#### Example C: Pure Practical Subject (e.g., Physical Education)
* **Credit Hours**: 2.0
* **HasTheory**: `false` | **Theory Marks**: N/A
* **HasPractical**: `true` | **Practical Marks**: 100.00 / 40.00
* **Overall**: 100.00 Full / 40.00 Pass

### 1.3 Validation Rules & UI Constraints
* **Field Disabling**: Dynamically disable theory inputs when `HasTheory = false`. Disable practical inputs when `HasPractical = false`.
* **Full Marks Consistency**: Ensure `OverallFullMarks = TheoryFullMarks + PracticalFullMarks`.
* **Individual Component Passing**: A student must satisfy the minimum pass mark requirement for *each* enabled component (`TheoryObtainedMarks >= TheoryPassMarks` AND `PracticalObtainedMarks >= PracticalPassMarks`).

---

## 2. Exam Management Module

### 2.1 ExamTerm
Represents a macro examination term/period in an academic year.

| Column | Data Type | Attributes | Description |
| :--- | :--- | :--- | :--- |
| `Id` | `Guid` | Primary Key | Unique term identifier |
| `AcademicYearId` | `Guid` | Foreign Key | Reference to `AcademicYear` |
| `Code` | `varchar(20)` | Unique | Short code (e.g., `TERM-1`, `FINAL`) |
| `Name` | `varchar(100)` | Required | Name (e.g., First Terminal, Mid Term, Final Exam) |
| `Sequence` | `int` | Required | Term ordering index |
| `StartDate` | `date` | Required | Assessment start date |
| `EndDate` | `date` | Required | Assessment end date |
| `PublishResult` | `bool` | Default: `false` | Status indicating if results are visible |
| `Status` | `smallint` | Required | Term lifecycle status (Draft, Active, Completed, Closed) |

### 2.2 Exam
Represents an individual exam component within an `ExamTerm`.

| Column | Data Type | Attributes | Description |
| :--- | :--- | :--- | :--- |
| `Id` | `Guid` | Primary Key | Unique exam identifier |
| `ExamTermId` | `Guid` | Foreign Key | Reference to `ExamTerm` |
| `Name` | `varchar(100)` | Required | Name of the exam (e.g., Written, Quiz, Unit Test) |
| `Description` | `varchar(500)` | Nullable | Additional context or instructions |
| `WeightagePercent` | `decimal(5,2)` | Required | Weighting percentage towards final result calculation |
| `IsFinalExam` | `bool` | Default: `false` | Identifies if this is the major final assessment |

### 2.3 ExamSchedule
Schedules specific subject assessments for class sections.

| Column | Data Type | Attributes | Description |
| :--- | :--- | :--- | :--- |
| `Id` | `Guid` | Primary Key | Unique schedule identifier |
| `ExamId` | `Guid` | Foreign Key | Reference to `Exam` |
| `ClassSubjectId` | `Guid` | Foreign Key | Reference to `ClassSubject` |
| `ClassSectionId` | `Guid` | Foreign Key | Reference to `ClassSection` |
| `ExamDate` | `date` | Required | Date of assessment |
| `StartTime` | `time` | Required | Exam start time |
| `EndTime` | `time` | Required | Exam end time |
| `Room` | `varchar(100)` | Nullable | Exam hall or room location |
| `InvigilatorEmployeeId`| `Guid` | Foreign Key, Nullable| Assigned invigilator/teacher |
| `MaximumMarks` | `decimal(5,2)` | Required | Maximum schedule marks |
| `PassMarks` | `decimal(5,2)` | Required | Minimum required pass marks |

### 2.4 CalendarEvent (Generic Calendar Integration)
Stores events across the application. When an `ExamSchedule` is created, a `CalendarEvent` entry is automatically generated.

| Column | Data Type | Attributes | Description |
| :--- | :--- | :--- | :--- |
| `Id` | `Guid` | Primary Key | Unique event identifier |
| `AcademicYearId` | `Guid` | Foreign Key | Reference to `AcademicYear` |
| `EventType` | `enum` | Required | Event category (`CalendarEventType`) |
| `Title` | `varchar(200)` | Required | Title of the event |
| `Description` | `text` | Nullable | Detailed description |
| `StartDateTime` | `timestamp` | Required | Event start timestamp |
| `EndDateTime` | `timestamp` | Required | Event end timestamp |
| `RelatedEntityType` | `varchar(50)` | Nullable | Target entity name (e.g., `ExamSchedule`) |
| `RelatedEntityId` | `Guid` | Nullable | Primary Key of related record |

---

## 3. Marks Entry & Scoring Module

### 3.1 StudentExamMark Entity
Captures detailed assessment scores per student for a given `ExamSchedule`. Unique constraint: (`ExamScheduleId`, `EnrollmentId`).

| Column | Data Type | Attributes | Description |
| :--- | :--- | :--- | :--- |
| `Id` | `Guid` | Primary Key | Unique entry identifier |
| `ExamScheduleId` | `Guid` | Foreign Key | Reference to `ExamSchedule` |
| `EnrollmentId` | `Guid` | Foreign Key | Reference to student's `Enrollment` |
| `TheoryObtainedMarks`| `decimal(5,2)` | Nullable | Raw theory marks obtained |
| `PracticalObtainedMarks`|`decimal(5,2)`| Nullable | Raw practical marks obtained |
| `InternalMarks` | `decimal(5,2)` | Nullable | Continuous/internal assessment marks |
| `TheoryGraceMarks` | `decimal(5,2)` | Default: `0.00` | Grace marks added to theory |
| `PracticalGraceMarks`| `decimal(5,2)`| Default: `0.00` | Grace marks added to practical |
| `TheoryAbsent` | `bool` | Default: `false` | True if absent for theory component |
| `PracticalAbsent` | `bool` | Default: `false` | True if absent for practical component |
| `TotalMarks` | `decimal(5,2)` | Calculated | Total marks (`Theory` + `Practical` + `Internal` + Grace) |
| `Grade` | `varchar(5)` | Calculated | Letter grade (e.g., A+, B, F) |
| `GradePoint` | `decimal(4,2)` | Calculated | Grade point corresponding to score |
| `Remarks` | `varchar(300)` | Nullable | Teacher/examiner remarks |
| `IsAbsent` | `bool` | Calculated | Flag if student was absent for all components |
| `IsPublished` | `bool` | Default: `false` | Indicates whether marks are finalized and published |

---

## 4. Result Processing Module

### 4.1 GradeScale
Defines the grading schema and grade point equivalencies.

| Column | Data Type | Attributes | Description |
| :--- | :--- | :--- | :--- |
| `Id` | `Guid` | Primary Key | Unique grade scale identifier |
| `Grade` | `varchar(5)` | Required | Letter grade designation (e.g., A+, A, B+, B, C, F) |
| `MinPercent` | `decimal(5,2)` | Required | Lower bound percentage |
| `MaxPercent` | `decimal(5,2)` | Required | Upper bound percentage |
| `GradePoint` | `decimal(4,2)` | Required | Numeric GPA contribution (e.g., 4.00, 3.60) |
| `Remarks` | `varchar(100)` | Nullable | Descriptive remark (e.g., Outstanding, Excellent) |

### 4.2 StudentResult
Stores aggregated final term results.

| Column | Data Type | Attributes | Description |
| :--- | :--- | :--- | :--- |
| `Id` | `Guid` | Primary Key | Unique result record identifier |
| `EnrollmentId` | `Guid` | Foreign Key | Reference to student's `Enrollment` |
| `ExamTermId` | `Guid` | Foreign Key | Reference to `ExamTerm` |
| `TotalMarks` | `decimal(7,2)` | Required | Total obtainable marks across scheduled exams |
| `ObtainedMarks` | `decimal(7,2)` | Required | Total marks achieved by the student |
| `Percentage` | `decimal(5,2)` | Required | Overall calculated percentage |
| `GPA` | `decimal(4,2)` | Required | Grade Point Average calculated on Credit Hours |
| `Rank` | `int` | Nullable | Class section position/rank |
| `ResultStatus` | `enum` | Required | Status (`ResultStatus` enum) |
| `PublishedDate` | `timestamp` | Nullable | Official result publication date |

### 4.3 End-to-End Result Processing Workflow

```
[1. Create Exam Term] ➔ [2. Create Exam] ➔ [3. Generate Exam Schedule]
                                                        │
                                                        ▼
[6. Lock Marks] ◄───── [5. Teachers Enter Marks] ◄───── [4. Auto-Create Calendar Events]
       │
       ▼
[7. Calculate Component Totals] ➔ [8. Calculate Percentage & GPA] ➔ [9. Apply Grade Scale]
                                                                             │
                                                                             ▼
[13. Notify Parents/Portal] ◄── [12. Publish Results] ◄── [11. Generate Student Result Records]
```

#### Detailed Processing Steps:
1. **Create Exam Term**: Admin initializes the term (e.g., "Final Term 2026").
2. **Create Exam**: Specific exam records are defined with designated weightages.
3. **Generate Exam Schedule**: Subjects are mapped to dates, times, rooms, and invigilators.
4. **Auto-create Calendar Events**: System triggers event generation for student/teacher calendars.
5. **Teacher Marks Entry**: Instructors input theory, practical, internal, and grace marks or mark absences.
6. **Lock Marks**: Once entry window closes, administrator locks entries to prevent edits.
7. **Calculate Totals**: System computes total marks for each subject assessment considering component validation.
8. **Calculate Percentage**: Aggregate total marks obtained divided by total max marks.
9. **Apply GradeScale**: Map raw marks/percentages to letter grades and grade points.
10. **Calculate GPA**: Compute weighted Grade Point Average using subject `CreditHours`.
11. **Generate StudentResult**: Persist summary row in `StudentResult` with status (`Pass`, `Fail`, `Compartment`, `Withheld`).
12. **Publish Results**: Update `PublishResult` status to true.
13. **Notify Stakeholders**: Send automated push notifications to student and parent portals.

---

## 5. Promotion and Student Transfer Module

### 5.1 Core Architecture: Immutability Principles
* **Enrollment as Snapshot**: An `Enrollment` record is an immutable historical snapshot binding a student to a specific `AcademicYear`, `AcademicClass`, `ClassSection`, and roll number.
* **Never Update Existing Enrollment**: Existing enrollments must **never** be edited to reflect class progression or repeating a grade.
* **Always Create New Enrollment**: Any movement (promotion, retention, or section transfer) requires generating a **new** `Enrollment` record.
* **Student Identity Consistency**: The `Student` table holds invariant personal details. Historical transcript generation and analytical reporting always filter via `EnrollmentId`.

### 5.2 StudentPromotion Entity

| Column | Data Type | Attributes | Description |
| :--- | :--- | :--- | :--- |
| `Id` | `Guid` | Primary Key | Unique promotion record identifier |
| `StudentId` | `Guid` | Foreign Key | Reference to `Student` |
| `FromEnrollmentId` | `Guid` | Foreign Key | Reference to previous `Enrollment` |
| `ToEnrollmentId` | `Guid` | Foreign Key | Reference to newly generated `Enrollment` |
| `PromotionDate` | `date` | Required | Effective date of promotion/transfer |
| `PromotionType` | `enum` | Required | Type of transition (`PromotionType` enum) |
| `Remarks` | `varchar(500)` | Nullable | Administrative notes or reason for transfer |

### 5.3 Promotion Decision & Workflow Algorithm

```
                  ┌─────────────────────────────────────────┐
                  │ Evaluate Active Enrollment Results      │
                  └────────────────────┬────────────────────┘
                                       │
                       ┌───────────────┴───────────────┐
                       ▼                               ▼
                 [Result = PASS]                [Result = FAIL]
                       │                               │
                       ▼                               ▼
         ┌───────────────────────────┐   ┌───────────────────────────┐
         │ - Create NEW Enrollment   │   │ - Create NEW Enrollment   │
         │ - Next Academic Year      │   │ - Next Academic Year      │
         │ - Next Academic Class     │   │ - SAME Academic Class     │
         │ - Assign New Roll Number  │   │ - Assign New Roll Number  │
         │ - Type: PROMOTED          │   │ - Type: RETAINED          │
         └─────────────┬─────────────┘   └─────────────┬─────────────┘
                       │                               │
                       └───────────────┬───────────────┘
                                       │
                                       ▼
                         ┌───────────────────────────┐
                         │ Close Old Enrollment      │
                         │ Create StudentPromotion   │
                         └───────────────────────────┘
```

#### Transfer Handling Workflow:
* When a student transfers mid-year or to a different section:
  1. Set current enrollment status to `Transferred` / Closed.
  2. Create a new `Enrollment` record in the destination class/section.
  3. Log entry in `StudentPromotion` with `PromotionType = Transferred`.
  4. Ensure previous historical marks and schedules remain attached to `FromEnrollmentId`.

---

## 6. Recommended System Enumerations

### 6.1 `ResultStatus`
* `Pass`: Student satisfied all overall and component-level pass marks.
* `Fail`: Student failed to meet minimum pass marks in one or more mandatory components.
* `Compartment`: Eligible for supplementary/re-examination in failed subjects.
* `Withheld`: Results on hold due to administrative, fee, or disciplinary reasons.

### 6.2 `PromotionType`
* `Promoted`: Successfully advanced to the next higher grade level.
* `Retained`: Repeating the same grade level in the subsequent academic year.
* `Transferred`: Moved to a different section, stream, or branch.

### 6.3 `CalendarEventType`
* `Exam`: Exam schedules, tests, or quizzes.
* `Holiday`: Official school holidays and vacations.
* `Result`: Result publication dates.
* `Meeting`: Parent-teacher meetings, board meetings.
* `Sports`: Athletic events and extracurriculars.
* `Notice`: General notices or circulars.

---

## 7. Key Architectural Principles & Notes

1. **Academic Snapshot Integrity**: `Enrollment` represents the exact state of a student during a given academic period. All academic records (attendance, schedules, marks, term results) point to `EnrollmentId`, not directly to `StudentId`.
2. **Reproducibility of Results**: Any published report card or result snapshot is fully reproducible at any point in time by querying stored `StudentExamMark` data alongside the locked `GradeScale` and `AssessmentConfiguration`.
3. **Component-Level Evaluation Rigor**: The decoupling of theory and practical assessment ensures that academic policies (such as requiring separate pass marks for theory and practical labs) are enforced at the database and application levels.
4. **Auditability & Traceability**: Immutable promotion history stored in `StudentPromotion` allows complete tracking of a student's academic journey over multiple years without altering past historical performance data.
