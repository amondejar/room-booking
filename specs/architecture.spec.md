---
spec: ARC
title: Architecture
kind: architecture
---
# Architecture

ASP.NET Core minimal API in one project, `src/RoomBooking.Api`, with three layers by namespace:

- `RoomBooking.Domain`: the booking rules (BookingService, Availability, Booking, Room). It gets what it needs through
  the constructor: the store, a `TimeProvider` and `IOptions<BookingOptions>`. Errors are returned as `Result<T>` with
  an `Error` (Invalid, NotFound, Forbidden, Conflict), never as exceptions or HTTP results.
- `RoomBooking.Endpoints`: maps HTTP to domain calls and back (`Results`, DTOs, `ToProblem()`), reads the user from the
  `X-User` header and contains no business rules.
- `RoomBooking.Infrastructure`: storage (`InMemoryStore`, guarded by its `Sync` lock).

Tests run the real API on Kestrel with a fixed clock (`FixedTimeProvider`): a new rule that depends on the time must be
testable that way.

## ARC-001: The domain does not depend on the web
In: RoomBooking.Domain
Must not use: Microsoft.AspNetCore, RoomBooking.Endpoints

The domain must be usable and testable without HTTP: requests, responses and status codes belong to the endpoints.

## ARC-002: Time is injected
In: RoomBooking.Domain
Must not use: System.DateTime.Now, System.DateTime.UtcNow, System.DateTime.Today, System.DateTimeOffset.Now, System.DateTimeOffset.UtcNow
Use instead: TimeProvider

Rules such as "has not started" or "in the past" are tested with a fixed clock.

## ARC-003: Endpoints go through the domain services
In: RoomBooking.Endpoints
Must not use: RoomBooking.Infrastructure

The endpoints call BookingService; reaching the store directly would skip the booking rules.
