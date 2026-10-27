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
