# Traceability report

Generated on 2026-10-29 16:50 by SpecTrace.

- Active requirements: **10** (approved or implemented)
- Implemented: **10/10**
- With at least one test: **10/10**
- Acceptance criteria with tests: **28/28**
- Acceptance criteria passing: **28/28**
- Mutants detected by the tests: **77%** (72/93) in the code of the requirements
- Unreviewed spec changes: **0**

## All requirements

| Requirement | Status | Implemented in | Verified by | Criteria | Mutants detected |
| --- | --- | --- | --- | --- | --- |
| [BK-001](specs/bookings.spec.md#L10) Create a booking | approved | `BookingService.Create` (src/RoomBooking.Api/Domain/BookingService.cs:53)<br>`BookingEndpoints.CreateBooking` (src/RoomBooking.Api/Endpoints/BookingEndpoints.cs:28) | `BookingsApiTests.Booking_a_free_room_creates_an_active_booking` (tests/RoomBooking.Tests/BookingsApiTests.cs:16) (AC-1) ✅<br>`BookingsApiTests.End_not_after_start_is_rejected` (tests/RoomBooking.Tests/BookingsApiTests.cs:34) (AC-2) ✅<br>`BookingsApiTests.Booking_an_unknown_room_is_not_found` (tests/RoomBooking.Tests/BookingsApiTests.cs:45) (AC-3) ✅<br>`BookingsApiTests.Booking_without_a_user_is_unauthorized` (tests/RoomBooking.Tests/BookingsApiTests.cs:56) (AC-4) ✅ | 4/4 ✅ | 100% (4/4) |
| [BK-002](specs/bookings.spec.md#L21) Prevent double booking | approved | `BookingService.FindConflict` (src/RoomBooking.Api/Domain/BookingService.cs:199) | `BookingsApiTests.Overlapping_booking_of_the_same_room_is_a_conflict` (tests/RoomBooking.Tests/BookingsApiTests.cs:69) (AC-1) ✅<br>`BookingsApiTests.Back_to_back_bookings_are_allowed` (tests/RoomBooking.Tests/BookingsApiTests.cs:81) (AC-2) ✅<br>`BookingsApiTests.A_cancelled_booking_frees_its_time` (tests/RoomBooking.Tests/BookingsApiTests.cs:93) (AC-3) ✅<br>`BookingsApiTests.The_same_time_can_be_booked_in_another_room` (tests/RoomBooking.Tests/BookingsApiTests.cs:106) (AC-4) ✅<br>`BookingsApiTests.Concurrent_requests_for_the_same_slot_create_only_one_booking` (tests/RoomBooking.Tests/BookingsApiTests.cs:118) (AC-1) ✅ | 4/4 ✅ | 100% (4/4) |
| [BK-003](specs/bookings.spec.md#L32) Respect room capacity | approved | `BookingService.CheckCapacity` (src/RoomBooking.Api/Domain/BookingService.cs:189) | `BookingsApiTests.More_attendees_than_capacity_is_rejected` (tests/RoomBooking.Tests/BookingsApiTests.cs:133) (AC-1) ✅<br>`BookingsApiTests.Exactly_the_capacity_is_allowed` (tests/RoomBooking.Tests/BookingsApiTests.cs:144) (AC-2) ✅ | 2/2 ✅ | 66% (6/9) |
| [BK-004](specs/bookings.spec.md#L41) Opening hours and duration | approved | `BookingService.CheckSchedule` (src/RoomBooking.Api/Domain/BookingService.cs:169) | `BookingsApiTests.Bookings_outside_opening_hours_are_rejected` (tests/RoomBooking.Tests/BookingsApiTests.cs:159) (AC-1) ✅<br>`BookingsApiTests.Bookings_longer_than_four_hours_are_rejected` (tests/RoomBooking.Tests/BookingsApiTests.cs:170) (AC-2) ✅<br>`BookingsApiTests.Bookings_in_the_past_are_rejected` (tests/RoomBooking.Tests/BookingsApiTests.cs:183) (AC-3) ✅ | 3/3 ✅ | 66% (10/15) |
| [BK-005](specs/bookings.spec.md#L51) Cancel a booking | approved | `BookingService.Cancel` (src/RoomBooking.Api/Domain/BookingService.cs:124)<br>`BookingEndpoints.CancelBooking` (src/RoomBooking.Api/Endpoints/BookingEndpoints.cs:55) | `BookingsApiTests.The_organizer_can_cancel_an_upcoming_booking` (tests/RoomBooking.Tests/BookingsApiTests.cs:196) (AC-1) ✅<br>`BookingsApiTests.Another_user_cannot_cancel_the_booking` (tests/RoomBooking.Tests/BookingsApiTests.cs:209) (AC-2) ✅<br>`BookingsApiTests.A_booking_that_has_started_cannot_be_cancelled` (tests/RoomBooking.Tests/BookingsApiTests.cs:222) (AC-3) ✅ | 3/3 ✅ | 60% (9/15) |
| [BK-006](specs/bookings.spec.md#L61) My upcoming bookings | approved | `BookingService.ListUpcoming` (src/RoomBooking.Api/Domain/BookingService.cs:144)<br>`BookingEndpoints.ListMine` (src/RoomBooking.Api/Endpoints/BookingEndpoints.cs:64) | `BookingsApiTests.Organizers_see_only_their_bookings_ordered_by_start` (tests/RoomBooking.Tests/BookingsApiTests.cs:237) (AC-1) ✅<br>`BookingsApiTests.Cancelled_and_finished_bookings_are_not_listed` (tests/RoomBooking.Tests/BookingsApiTests.cs:251) (AC-2) ✅ | 2/2 ✅ | 75% (3/4) |
| [BK-007](specs/bookings.spec.md#L70) Recurring bookings | approved | `BookingService.CreateWeekly` (src/RoomBooking.Api/Domain/BookingService.cs:78)<br>`BookingEndpoints.CreateRecurringBooking` (src/RoomBooking.Api/Endpoints/BookingEndpoints.cs:39) | `BookingsApiTests.Weekly_booking_of_a_free_room_creates_one_booking_per_week` (tests/RoomBooking.Tests/BookingsApiTests.cs:270) (AC-1) ✅<br>`BookingsApiTests.Weekly_booking_skips_and_reports_a_taken_week` (tests/RoomBooking.Tests/BookingsApiTests.cs:288) (AC-2) ✅<br>`BookingsApiTests.Weekly_booking_for_more_than_twelve_weeks_is_rejected` (tests/RoomBooking.Tests/BookingsApiTests.cs:307) (AC-3) ✅ | 3/3 ✅ | 90% (18/20) |
| [RM-001](specs/rooms.spec.md#L11) List rooms | approved | `BookingService.ListRooms` (src/RoomBooking.Api/Domain/BookingService.cs:32)<br>`RoomEndpoints.ListRooms` (src/RoomBooking.Api/Endpoints/RoomEndpoints.cs:20) | `RoomsApiTests.Lists_all_rooms_ordered_by_name` (tests/RoomBooking.Tests/RoomsApiTests.cs:13) (AC-1) ✅<br>`RoomsApiTests.Filters_rooms_by_minimum_capacity` (tests/RoomBooking.Tests/RoomsApiTests.cs:24) (AC-2) ✅ | 2/2 ✅ | 100% (5/5) |
| [RM-002](specs/rooms.spec.md#L20) Room details | approved | `BookingService.FindRoom` (src/RoomBooking.Api/Domain/BookingService.cs:39)<br>`RoomEndpoints.GetRoom` (src/RoomBooking.Api/Endpoints/RoomEndpoints.cs:24) | `RoomsApiTests.Returns_the_details_of_a_room` (tests/RoomBooking.Tests/RoomsApiTests.cs:35) (AC-1) ✅<br>`RoomsApiTests.Unknown_room_is_not_found` (tests/RoomBooking.Tests/RoomsApiTests.cs:46) (AC-2) ✅ | 2/2 ✅ | 0% (0/2) ⚠️ |
| [RM-003](specs/rooms.spec.md#L28) Room availability for a day | approved | `Availability.FreeSlots` (src/RoomBooking.Api/Domain/Availability.cs:12)<br>`RoomEndpoints.GetAvailability` (src/RoomBooking.Api/Endpoints/RoomEndpoints.cs:30) | `AvailabilityTests.Back_to_back_and_overlapping_bookings_merge_into_one_busy_block` (tests/RoomBooking.Tests/AvailabilityTests.cs:17) (AC-2) ✅<br>`AvailabilityTests.A_fully_booked_day_has_no_free_slots` (tests/RoomBooking.Tests/AvailabilityTests.cs:28) (AC-2) ✅<br>`RoomsApiTests.Room_without_bookings_is_free_all_day` (tests/RoomBooking.Tests/RoomsApiTests.cs:57) (AC-1) ✅<br>`RoomsApiTests.Booked_time_is_left_out_of_the_free_slots` (tests/RoomBooking.Tests/RoomsApiTests.cs:68) (AC-2) ✅<br>`RoomsApiTests.Cancelled_bookings_do_not_block_the_room` (tests/RoomBooking.Tests/RoomsApiTests.cs:82) (AC-3) ✅ | 3/3 ✅ | 86% (13/15) |

## Changes the tests do not detect

Each line is a change to the requirement's code that no test failed with: a test that should exist and does not.

**[RM-002](specs/rooms.spec.md#L20) Room details**: 0% (0/2) ⚠️

- [src/RoomBooking.Api/Endpoints/RoomEndpoints.cs:28](src/RoomBooking.Api/Endpoints/RoomEndpoints.cs#L28) String mutation: `$""` (survived)
- [src/RoomBooking.Api/Endpoints/RoomEndpoints.cs:28](src/RoomBooking.Api/Endpoints/RoomEndpoints.cs#L28) String mutation: `""` (survived)

**[BK-005](specs/bookings.spec.md#L51) Cancel a booking**: 60% (9/15)

- [src/RoomBooking.Api/Domain/BookingService.cs:129](src/RoomBooking.Api/Domain/BookingService.cs#L129) Linq method mutation (FirstOrDefault() to First()): `_store.Bookings.First` (survived)
- [src/RoomBooking.Api/Domain/BookingService.cs:130](src/RoomBooking.Api/Domain/BookingService.cs#L130) String mutation: `$""` (no test runs this code)
- [src/RoomBooking.Api/Domain/BookingService.cs:133](src/RoomBooking.Api/Domain/BookingService.cs#L133) String mutation: `""` (survived)
- [src/RoomBooking.Api/Domain/BookingService.cs:136](src/RoomBooking.Api/Domain/BookingService.cs#L136) String mutation: `""` (no test runs this code)
- [src/RoomBooking.Api/Domain/BookingService.cs:137](src/RoomBooking.Api/Domain/BookingService.cs#L137) Equality mutation: `booking.Start < Now` (survived)
- … and 1 more.

**[BK-003](specs/bookings.spec.md#L32) Respect room capacity**: 66% (6/9)

- [src/RoomBooking.Api/Domain/BookingService.cs:192](src/RoomBooking.Api/Domain/BookingService.cs#L192) Equality mutation: `attendees <= 1` (survived)
- [src/RoomBooking.Api/Domain/BookingService.cs:192](src/RoomBooking.Api/Domain/BookingService.cs#L192) String mutation: `""` (no test runs this code)
- [src/RoomBooking.Api/Domain/BookingService.cs:194](src/RoomBooking.Api/Domain/BookingService.cs#L194) String mutation: `$""` (survived)

**[BK-004](specs/bookings.spec.md#L41) Opening hours and duration**: 66% (10/15)

- [src/RoomBooking.Api/Domain/BookingService.cs:172](src/RoomBooking.Api/Domain/BookingService.cs#L172) Equality mutation: `request.Start < Now` (survived)
- [src/RoomBooking.Api/Domain/BookingService.cs:172](src/RoomBooking.Api/Domain/BookingService.cs#L172) String mutation: `""` (survived)
- [src/RoomBooking.Api/Domain/BookingService.cs:176](src/RoomBooking.Api/Domain/BookingService.cs#L176) Equality mutation: `request.End >= closes` (survived)
- [src/RoomBooking.Api/Domain/BookingService.cs:178](src/RoomBooking.Api/Domain/BookingService.cs#L178) String mutation: `$""` (survived)
- [src/RoomBooking.Api/Domain/BookingService.cs:183](src/RoomBooking.Api/Domain/BookingService.cs#L183) String mutation: `$""` (survived)

**[BK-006](specs/bookings.spec.md#L61) My upcoming bookings**: 75% (3/4)

- [src/RoomBooking.Api/Domain/BookingService.cs:151](src/RoomBooking.Api/Domain/BookingService.cs#L151) Equality mutation: `b.End >= now` (survived)

**[RM-003](specs/rooms.spec.md#L28) Room availability for a day**: 86% (13/15)

- [src/RoomBooking.Api/Domain/Availability.cs:27](src/RoomBooking.Api/Domain/Availability.cs#L27) Equality mutation: `booking.End >= cursor` (survived)
- [src/RoomBooking.Api/Endpoints/RoomEndpoints.cs:34](src/RoomBooking.Api/Endpoints/RoomEndpoints.cs#L34) Conditional (true) mutation: `(true?Results.Ok(new AvailabilityDto(id, date, result.Val...` (survived)

**[BK-007](specs/bookings.spec.md#L70) Recurring bookings**: 90% (18/20)

- [src/RoomBooking.Api/Domain/BookingService.cs:81](src/RoomBooking.Api/Domain/BookingService.cs#L81) Equality mutation: `weeks <= 1` (survived)
- [src/RoomBooking.Api/Domain/BookingService.cs:83](src/RoomBooking.Api/Domain/BookingService.cs#L83) String mutation: `$""` (survived)

**[BK-001](specs/bookings.spec.md#L10) Create a booking**: 100% (4/4)

- The tests of AC-4 detect none of the changes in this code.
