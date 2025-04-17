# Plan: Migrating Blazor Client Functionality to Blazor Server

This document outlines the steps to migrate UI components, services, and logic from the `HemSoft.EggIncTracker.Dashboard.BlazorClient` project to the `HemSoft.EggIncTracker.Dashboard.BlazorServer` project. The goal is to consolidate the dashboard functionality into the server-side rendering model for easier debugging and potentially simpler architecture.

## 1. Identify Client Components and Logic

- **Goal:** Create an inventory of all relevant files and features in the `BlazorClient` project.
- **Action:** List the contents of the following directories within `sources/HemSoft.EggIncTracker.Dashboard.BlazorClient`:
    - `Components/`
    - `Extensions/`
    - `Layout/`
    - `Models/` (if any client-specific models exist)
    - `Pages/`
    - `Services/`
    - `wwwroot/` (note custom assets/CSS/JS)
- **Action:** Review `Program.cs` for service registrations, HttpClient configurations, and other setup logic.
- **Action:** Review `_Imports.razor` and `Routes.razor`.

## 2. Analyze Dependencies and Server Compatibility

- **Goal:** Identify code that relies on WebAssembly or client-side execution and determine server-side equivalents.
- **Action:** Examine services like `ApiService` and `PlayerApiClient`. How do they fetch data? (Likely `HttpClient`).
- **Action:** Analyze `DashboardState`. How is state managed? Is it suitable for Blazor Server's scoped lifecycle?
- **Action:** Check for any JavaScript interop calls. Will they still be needed or can they be replaced with server-side C# logic?

## 3. Plan Feature Migration

- **Goal:** Define where each piece of client functionality will reside in the `BlazorServer` project.
- **Action:**
    - **UI Components (`Components/`, `Layout/`, `Pages/`):** Plan to move `.razor` and associated `.cs`/`.css` files to the corresponding directories under `sources/HemSoft.EggIncTracker.Dashboard.BlazorServer/Components/`. Adjust namespaces as needed.
    - **Services (`Services/`):**
        - `ApiService`/`PlayerApiClient`: Decide on the data access strategy for the server project.
            - **Option A (Keep API Calls):** Configure `HttpClient` in the server's `Program.cs` to call the `HemSoft.EggIncTracker.Api` endpoints. Services might be reusable with minimal changes.
            - **Option B (Direct Domain/Data Access):** Modify services to directly use domain logic (`HemSoft.EggIncTracker.Domain`) and data access (`HemSoft.EggIncTracker.Data`) projects. This often simplifies things if the projects are in the same solution. Requires adding project references. *This is likely the preferred approach for Blazor Server.*
        - `DashboardState`: Adapt for Blazor Server's state management patterns (e.g., ensure it's registered as a scoped service).
        - `PlayerDataService`: Adapt based on the chosen data access strategy (Option A or B).
    - **Helper Classes (`Extensions/`, `Models/`):** Move to appropriate locations within the `BlazorServer` project structure (e.g., create `Extensions` or `Models` folders if needed). Adjust namespaces.
    - **Static Assets (`wwwroot/`):** Merge necessary CSS, JavaScript, images, and other assets into the `BlazorServer`'s `wwwroot` directory. Be careful not to overwrite essential server files (like `_Host.cshtml` or `app.css` unless intended). Merge `appsettings.json` configurations.
    - **Configuration (`Program.cs`, `_Imports.razor`, `Routes.razor`):**
        - Merge service registrations from the client's `Program.cs` into the server's `Program.cs`, adjusting service lifetimes (e.g., transient/singleton in WASM might become scoped in Server). Configure `HttpClientFactory` if Option A is chosen. Add necessary `using` statements.
        - Merge `using` statements from the client's `_Imports.razor` into `sources/HemSoft.EggIncTracker.Dashboard.BlazorServer/Components/_Imports.razor`.
        - Integrate routing logic from `Routes.razor` into `sources/HemSoft.EggIncTracker.Dashboard.BlazorServer/Components/App.razor`.

## 4. Execute Migration (Step-by-Step)

- **Goal:** Move and adapt code incrementally.
- **Action:**
    - Add project references from `BlazorServer` to `Domain` and `Data` projects if choosing Option B for data access.
    - Move helper classes/models first.
    - Move UI components (Layout, Components, Pages) one section at a time, fixing namespace issues and compilation errors.
    - Adapt and register services (`Program.cs`), implementing the chosen data access strategy.
    - Merge `_Imports.razor` and `App.razor`/`Routes.razor`.
    - Merge `wwwroot` assets and `appsettings.json`.
    - Build and test frequently.

## 5. Refactor Data Access (If needed)

- **Goal:** Implement the chosen data access strategy (Option A or B).
- **Action:** If Option B, replace `HttpClient` calls in services with direct calls to domain managers or DbContexts. Inject necessary domain/data services.

## 6. State Management Review

- **Goal:** Ensure state is handled correctly in the server environment.
- **Action:** Test components that rely on `DashboardState` across multiple user sessions and interactions to verify correct scoping and behavior.

## 7. Testing

- **Goal:** Verify all migrated functionality works as expected in the Blazor Server environment.
- **Action:** Perform thorough testing of all pages, components, and user interactions. Check browser console for errors.

## 8. Cleanup

- **Goal:** Remove the old client project.
- **Action:** Once the migration is complete and verified:
    - Remove the `HemSoft.EggIncTracker.Dashboard.BlazorClient` project from the solution (`EggIncTracker.sln`).
    - Delete the `sources/HemSoft.EggIncTracker.Dashboard.BlazorClient` directory.
    - Update any solution-level configurations or build scripts if necessary.
