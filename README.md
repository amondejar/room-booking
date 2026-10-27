# RoomBooking

A meeting room booking API in ASP.NET Core, built **spec-first** with [SpecTrace](https://github.com/amondejar/spec-trace).
Every rule of the system is a requirement in `specs/`, every requirement is implemented by code marked with
`[Implements]`, and every acceptance criterion has a test marked with `[Verifies]`. The compiler checks all of it.

```text
Active requirements: 9 · Implemented: 9/9 · With tests: 9/9 · Acceptance criteria with tests: 25/25
```

## Run it

Requires the .NET 8 SDK.

```bash
dotnet tool restore                          # SpecTrace CLI (dotnet spectrace), pinned in .config/dotnet-tools.json
dotnet build                                 # also checks traceability
dotnet test
dotnet run --project src/RoomBooking.Api     # http://localhost:5080
```

Open `src/RoomBooking.Api/RoomBooking.http` in Visual Studio, Rider or VS Code (REST Client) to try every endpoint.

| Endpoint | Requirement |
| --- | --- |
| `GET /rooms?minCapacity=8` | RM-001 List rooms |
| `GET /rooms/{id}` | RM-002 Room details |
| `GET /rooms/{id}/availability?date=2030-01-07` | RM-003 Room availability for a day |
| `POST /bookings` | BK-001 Create a booking · BK-002 no double booking · BK-003 capacity · BK-004 opening hours |
| `POST /bookings/{id}/cancel` | BK-005 Cancel a booking |
| `GET /bookings/mine` | BK-006 My upcoming bookings |

The organizer is read from the `X-User` header. It is a stand-in for real authentication: swap it for JWT or cookies
before going to production. Data is kept in memory and resets when the app restarts.

## How the traceability works

```markdown
## BK-002: Prevent double booking
Status: approved

If the requested time range overlaps an active booking of the same room,
then the system shall reject the booking with a conflict.

- AC-1: Given an active booking from 10:00 to 11:00, when a booking from 10:30 to 11:30 ... then 409 Conflict.
- AC-2: Given an active booking from 10:00 to 11:00, when a booking from 11:00 to 12:00 ... then it is created.
```

SpecTrace generates `Requirements.BK_002` and `AcceptanceCriteria.BK_002.AC_1` from the spec. The code and the tests
use those constants:

```csharp
[Implements(Requirements.BK_002)]
private Booking? FindConflict(Room room, DateTime start, DateTime end) => ...

[Fact]
[Verifies(Requirements.BK_002, AcceptanceCriteria.BK_002.AC_1)]
public async Task Overlapping_booking_of_the_same_room_is_a_conflict() { ... }
```

| You do this | The build says |
| --- | --- |
| Approve a requirement and don't implement it | `ST0003` on the spec line |
| Implement it without tests | `ST0004` |
| Leave an acceptance criterion without a test | `ST0005` |
| Mistype an ID (`"BK-02"`) | `ST0001`, with a code fix to the closest ID |
| Change a requirement's text after its code was reviewed | `ST0010` / `ST0011`, naming the code or tests to review |

Locally these are warnings (`.globalconfig`). In CI they are errors.

```bash
dotnet spectrace report        # full requirement → code → test matrix
dotnet spectrace lock --check  # any spec change not yet reviewed?
```

## Changing the spec

1. Edit the requirement in `specs/*.spec.md`.
2. `dotnet build` points at the code and tests that were written against the old text (`ST0010` / `ST0011`).
3. Update them, then record the review: `dotnet spectrace lock --requirement BK-003`.
4. Commit the spec, the code, the tests and `spectrace.lock` together.

## Working with an AI coding agent

The repo is ready for Claude Code and any MCP client:

- `.mcp.json` registers the SpecTrace MCP server (`dotnet spectrace mcp`).
- `.claude/skills/spectrace/SKILL.md` tells the agent how to work: read the requirement and the real code first,
  mark the work with `[Implements]` / `[Verifies]`, and stop when the build has no gaps.

`BK-007: Recurring bookings` is in status `proposed`: it is not enforced yet. To try the full loop, change it to
`Status: approved` (the build now reports `ST0003` and `ST0005`) and ask the agent:

```text
Implement BK-007 with its tests. Follow the spectrace skill.
```

`dotnet spectrace context BK-007` shows what the agent receives: the statement, the criteria, where the code goes and
which files to read first.

## CI

`.github/workflows/ci.yml` builds with the SpecTrace diagnostics as errors, runs the tests and publishes the
traceability report on each pull request (annotations, job summary and a comment), failing on gaps or on unreviewed
spec changes.

## Layout

```text
specs/                         rooms.spec.md, bookings.spec.md: the source of truth
spectrace.json                 SpecTrace root: where the specs and the code are
spectrace.lock                 reviewed version of each requirement
.globalconfig                  SpecTrace severities
src/RoomBooking.Api/
  Domain/                      rules: BookingService, Availability, Booking, Room
  Endpoints/                   HTTP mapping and problem details
  Infrastructure/              in-memory store
tests/RoomBooking.Tests/       xUnit tests against the real app on Kestrel, with a fixed clock
```
