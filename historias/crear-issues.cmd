@echo off
rem Creates the three test user stories in amondejar/room-booking. Requires the GitHub CLI (gh auth login).
gh label create user-story --repo amondejar/room-booking --color 5B34E0 --description "User story to turn into a spec" 2>nul
gh issue create --repo amondejar/room-booking --label user-story --title "[Story] Move a booking to another time" --body-file 1-move-booking.md
gh issue create --repo amondejar/room-booking --label user-story --title "[Story] Room equipment: projector and video call" --body-file 2-room-equipment.md
gh issue create --repo amondejar/room-booking --label user-story --title "[Story] Fairer use of the big room" --body-file 3-fair-use.md
