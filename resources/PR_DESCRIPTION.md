# Fix timer persistence between application restarts

This PR fixes issue #21 by implementing a timer persistence mechanism that saves timer states to a local file and loads them when the application restarts.

## Changes made:

1. Created a new `TimerPersistenceService` class to handle saving and loading timer states
2. Modified the `RocketMissionService` to use the `TimerPersistenceService` to persist mission timer states
3. Updated the `RocketCard` component to properly handle timer states loaded from persistent storage
4. Registered the `TimerPersistenceService` in the dependency injection container

## How it works:

- Timer states (mission return times) are saved to a JSON file in the application's data directory
- When the application starts, it tries to load timer states from the database first
- If no timer states are found in the database, it falls back to the persisted timer states
- This ensures that timers continue to run correctly even after the application is restarted

Fixes #21
