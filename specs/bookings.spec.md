---
spec: BK
title: Bookings
---
# Bookings

An organizer books a room for a time range and a number of attendees. Organizers are identified by the
`X-User` header (a demo stand-in for real authentication).

## BK-001: Create a booking
Status: approved

When an organizer requests a booking of a room for a valid time range,
the system shall create the booking in state Active and return it with its id.

- AC-1: Given a free room and a valid time range, when the booking is requested, then the response is 201 Created with the booking in state Active and a Location header.
- AC-2: Given an end time that is not after the start time, when the booking is requested, then it is rejected with 400 Bad Request.
- AC-3: Given an unknown room id, when the booking is requested, then the response is 404 Not Found.
- AC-4: Given a request without the X-User header, when the booking is requested, then the response is 401 Unauthorized.

## BK-002: Prevent double booking
Status: approved

If the requested time range overlaps an active booking of the same room,
then the system shall reject the booking with a conflict.

- AC-1: Given an active booking from 10:00 to 11:00, when a booking from 10:30 to 11:30 of the same room is requested, then the response is 409 Conflict.
- AC-2: Given an active booking from 10:00 to 11:00, when a booking from 11:00 to 12:00 of the same room is requested, then it is created.
- AC-3: Given a cancelled booking from 10:00 to 11:00, when a booking from 10:00 to 11:00 of the same room is requested, then it is created.
- AC-4: Given an active booking from 10:00 to 11:00 in one room, when the same time is booked in another room, then it is created.

## BK-003: Respect room capacity
Status: approved

If the number of attendees is less than one or exceeds the capacity of the room,
then the system shall reject the booking.

- AC-1: Given a room with capacity 4, when a booking for 5 attendees is requested, then it is rejected with 400 Bad Request.
- AC-2: Given a room with capacity 4, when a booking for 4 attendees is requested, then it is created.
- AC-3: Given any room, when a booking for 0 attendees is requested, then it is rejected with 400 Bad Request.
- AC-4: Given a room with capacity 4, when a booking for 1 attendee is requested, then it is created.

## BK-004: Opening hours and duration
Status: approved

The system shall only accept bookings that start in the future, fall within opening hours of a single day
and last at most 4 hours.

- AC-1: Given a time range starting before opening time or ending after closing time, when the booking is requested, then it is rejected with 400 Bad Request.
- AC-2: Given a time range longer than 4 hours, when the booking is requested, then it is rejected with 400 Bad Request.
- AC-3: Given a start time in the past, when the booking is requested, then it is rejected with 400 Bad Request.

## BK-005: Cancel a booking
Status: approved

While a booking is Active and has not started, when its organizer cancels it,
the system shall move it to state Cancelled.

- AC-1: Given an upcoming active booking, when its organizer cancels it, then the response is 204 No Content and the booking is Cancelled.
- AC-2: Given an upcoming active booking, when a different user tries to cancel it, then the response is 403 Forbidden.
- AC-3: Given a booking that has already started, when its organizer tries to cancel it, then the response is 409 Conflict.
- AC-4: Given an unknown booking id, when its organizer tries to cancel it, then the response is 404 Not Found.

## BK-006: My upcoming bookings
Status: approved

When an organizer lists their bookings, the system shall return their active bookings that have not ended,
ordered by start time.

- AC-1: Given bookings from two organizers, when one of them lists their bookings, then only their own bookings are returned, ordered by start time.
- AC-2: Given a cancelled booking and a booking that has already ended, when the organizer lists their bookings, then neither is returned.

## BK-007: Recurring bookings
Status: approved

> Requested by the facilities team.

When an organizer requests a weekly recurring booking for a number of weeks,
the system shall create one booking per week in the same room and time, skipping the weeks
where the booking is not possible, and report the skipped weeks.

- AC-1: Given a free room, when a weekly booking for 4 weeks is requested, then 4 bookings are created.
- AC-2: Given a room that is taken in week 2, when a weekly booking for 4 weeks is requested, then 3 bookings are created and week 2 is reported as skipped.
- AC-3: Given a request for more than 12 weeks, when the booking is requested, then it is rejected with 400 Bad Request.