# Student Management System – Exam Module Design

## Goals

This design revises the Exam Module to support both simple schools and schools requiring advanced examination hall management.

The module is configurable so institutions can either:

- **Disable Seat Arrangement** (simple scheduling only)
- **Enable Seat Arrangement** (automatic hall allocation and seating)

---

# Configuration

| Setting | Description |
|---|---|
| EnableSeatArrangement | Enables examination hall planning and seat allocation. |
| AutoGenerateExamRollNumber | Generates exam roll numbers during seat allocation. |
| AutoAllocateStudents | Automatically assigns students to halls. |
| AllowMultipleClassesPerHall | Allows multiple classes to share a hall. |

When **EnableSeatArrangement = False**

- Create Exam Term
- Create Exam Schedule
- Assign Subject, Date, Time and Invigilator
- Conduct Examination

No seating plan is required.

When **EnableSeatArrangement = True**

The complete seating workflow becomes available.

---

# Exam Term

Represents First Terminal, Mid-Term, Final etc.

---

# Exam Schedule

Represents a scheduled examination for a subject.

## Fields

- Id
- ExamTermId
- SubjectId
- ExamDate
- StartTime
- EndTime
- RoomId
- InvigilatorEmployeeId
- Remarks

## Removed Fields

- Name
- WeightagePercent
- ClassSectionId

Sections are intentionally removed from scheduling because seating is performed at student level.

---

# Room

Existing ClassSection records can be reused as examination rooms (filtered by current Academic Year).

Each room contains:

- Room Name
- Building
- Floor
- Capacity

---

# Hall Arrangement

Created only when Seat Arrangement is enabled.

## ExamHallArrangement

- Id
- ExamScheduleId
- RoomId
- InvigilatorEmployeeId
- HallCapacity
- ReservedSeats
- EffectiveCapacity
- Status

EffectiveCapacity = HallCapacity - ReservedSeats

---

# Applicable Classes

One hall can accommodate students from multiple classes.

## ExamHallArrangementClass

- Id
- ExamHallArrangementId
- AcademicClassId
- StudentLimit (optional)

Example

Hall A (Capacity 40)

- Nursery (15)
- LKG (15)
- UKG (10)

or

Hall B

- Grade 8 (30)
- Grade 9 (20)
- Grade 10 (10)

If StudentLimit is empty, the system allocates students until the hall reaches capacity.

Validation

- No duplicate classes
- Sum of limits cannot exceed hall capacity
- At least one class required

---

# Student Seat Allocation

## ExamSeatAllocation

- Id
- ExamHallArrangementId
- StudentId
- AcademicClassId
- SeatNumber
- ExamRollNumber
- AttendanceStatus

One student may only have one allocation for the same exam.

---

# Student Ordering

Recommended order before allocation:

1. Academic Class
2. First Name
3. Registration Number

This provides deterministic allocation while allowing new admissions to fit naturally.

Alternative strategies may be configurable:

- Alphabetical
- Registration Number
- Random
- Interleaved by Class

---

# Student Registration Number

Generated once during admission.

Recommended format

REG-2026-1001

Characteristics

- Immutable
- Unique
- Never updated

Avoid deriving the identifier from the student's first name because names may change.

---

# Exam Roll Number

Generated separately for each examination.

Example

First Terminal

001
002
003

Final

001
002
003

Stored in ExamSeatAllocation.

---

# Automatic Allocation Logic

1. Create Exam Schedule.
2. Select Room.
3. Assign Invigilator.
4. Enter Hall Capacity.
5. Select one or more applicable classes.
6. Optionally specify StudentLimit for each class.
7. System validates capacity.
8. Fetch eligible students.
9. Sort students.
10. Allocate seats.
11. Generate Exam Roll Numbers.
12. Lock or Publish arrangement.

---

# Recommended UI

Exam Schedule

- Room
- Invigilator
- Hall Capacity
- Reserved Seats

Applicable Classes Grid

- Class
- Student Limit
- Remove

Button

- Add Class

Action Buttons

- Generate Arrangement
- Publish
- Lock

---

# Business Rules

- Multiple classes may occupy the same hall.
- Sections are not considered during seating.
- Hall capacity must be respected.
- StudentLimit cannot exceed effective capacity.
- Duplicate classes are not allowed within the same hall.
- One student cannot be allocated twice for the same examination.
- Locked arrangements cannot be edited.
- Students added after publication may either occupy remaining seats or trigger regeneration based on school configuration.

---

# Overall Workflow

Exam Term
    ↓
Exam Schedule
    ↓
Assign Room & Invigilator
    ↓
(Optional)
Hall Arrangement
    ↓
Select Applicable Classes
    ↓
Generate Seating
    ↓
Generate Exam Roll Numbers
    ↓
Attendance
    ↓
Result Processing

This design supports schools ranging from small institutions that simply schedule exams to larger schools requiring sophisticated hall allocation, seating plans, invigilator assignment, and automatic exam roll number generation without maintaining separate implementations.
