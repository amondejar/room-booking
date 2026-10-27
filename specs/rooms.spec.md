---
spec: RM
title: Meeting rooms
---
# Meeting rooms

The office has a fixed set of meeting rooms. Clients can browse them and check when a room is free
before booking it. Times are in the office's local time; opening hours are configurable
(`Booking:OpensAt` / `Booking:ClosesAt`, 08:00–20:00 by default).

## RM-001: List rooms
Status: approved

When a client requests the list of rooms, the system shall return every room with its id, name and capacity,
ordered by name.

- AC-1: Given the three seeded rooms, when a client requests the rooms, then all three are returned ordered by name.
- AC-2: Given a minimum capacity of 8, when a client requests the rooms, then only rooms with a capacity of at least 8 are returned.

## RM-002: Room details
Status: approved

When a client requests a room by its id, the system shall return its details.

- AC-1: Given an existing room id, when a client requests it, then its id, name and capacity are returned.
- AC-2: Given an unknown room id, when a client requests it, then the response is 404 Not Found.

## RM-003: Room availability for a day
Status: approved

When a client requests the availability of a room for a date, the system shall return the free time slots
within opening hours that do not overlap any active booking of that room.

- AC-1: Given a room without bookings that day, when its availability is requested, then a single slot from opening to closing time is returned.
- AC-2: Given a booking from 10:00 to 11:00, when the availability is requested, then the slots 08:00–10:00 and 11:00–20:00 are returned.
- AC-3: Given a cancelled booking from 10:00 to 11:00, when the availability is requested, then that time is reported as free.

## RM-004: Room equipment
Status: proposed

> Source: https://github.com/amondejar/room-booking/issues/4 (no acceptance criteria in the story; room data from its Notes)
> Open questions:
> - When filtering by both projector and video call, must a room have both, or either?
> - What happens with an unknown equipment value in the filter: 400 Bad Request, or ignored?
> - Is the equipment filter combined with the minimum-capacity filter (RM-001 AC-2)? With both, must a room meet both?
> - Is equipment limited to projector and video call, or can more kinds be added? Who maintains it (seed data, configuration)?
> - Should a booking be able to require equipment, or is this only for browsing rooms?
> - Domain note: rooms have no equipment today (`Room` is id, name and capacity only). This extends what RM-001 and RM-002 return without changing their text.

> Proposed by an agent on 2026-10-27. Review it and change the status to approved to enforce it.

When a client requests rooms, the system shall report for each room whether it has a projector and whether it has video-call equipment, and when the client filters by a piece of equipment, the system shall return only the rooms that have it.

- AC-1: Given the three seeded rooms, when a client requests the rooms, then Cosmos is reported with a projector and video call, Boreal with video call only, and Atlas with no equipment.
- AC-2: Given the room id "cosmos", when a client requests it, then its details include a projector and video call.
- AC-3: Given the three seeded rooms, when a client requests the rooms filtered by projector, then only Cosmos is returned.
- AC-4: Given the three seeded rooms, when a client requests the rooms filtered by video call, then Boreal and Cosmos are returned, ordered by name.
