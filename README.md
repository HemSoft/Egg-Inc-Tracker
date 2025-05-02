# Egg Inc Tracker

A comprehensive tracking and analytics application for the mobile game Egg Inc. This application provides dashboards, player statistics, rankings, and event tracking for Egg Inc players.

## Table of Contents

- [Overview](#overview)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Features](#features)
- [Setup and Installation](#setup-and-installation)
- [Configuration](#configuration)
- [Running the Application](#running-the-application)
- [API Documentation](#api-documentation)
- [Database](#database)
- [Contributing](#contributing)

## Overview

Egg Inc Tracker is a suite of applications designed to track player progress, events, and contracts in the mobile game Egg Inc. The application consists of:

- A Blazor Server dashboard for visualizing player data
- Azure Functions for automated data collection
- A RESTful API for accessing player data
- News gathering functionality with AI-powered content parsing

The application helps players track their progress, compare with other players, and stay updated on game events and contracts.

## Tech Stack

- **Framework**: .NET 8.0 and .NET 9.0
- **Frontend**: Blazor Server with MudBlazor component library
- **Backend**:
  - ASP.NET Core Web API
  - Azure Functions
  - Entity Framework Core
- **Database**:
  - SQL Server for main application data
  - SQLite for news data
- **AI Integration**: Microsoft.Extensions.AI for content parsing
- **Authentication**: Azure AD (optional)
- **Deployment**: Docker support for containerization

## Project Structure

The solution is organized into the following projects:

### Sources

- **HemSoft.EggIncTracker.Dashboard.BlazorServer**: Main Blazor Server application for the dashboard UI
- **HemSoft.EggIncTracker.Api**: RESTful API for accessing player data
- **HemSoft.EggIncTracker.Functions**: Azure Functions for automated data collection
- **HemSoft.EggIncTracker.Domain**: Business logic and domain models
- **HemSoft.EggIncTracker.Data**: Data access layer with Entity Framework Core
- **HemSoft.News.Functions**: Azure Functions for news gathering
- **HemSoft.News.Data**: Data access layer for news data
- **HemSoft.News.Tools**: Tools for news processing
- **HemSoft.AI**: AI integration services

### Tests

- **HemSoft.News.Functions.Tests**: Tests for news functions
- **HemSoft.News.Data.Tests**: Tests for news data access

## Features

### Dashboard

- Player statistics visualization
- Progress tracking with goals
- Soul Egg and Earning Bonus tracking
- Prestige count tracking (daily and weekly)
- Player rankings and comparisons
- Event calendar and notifications

### Player Tracking

- Automated data collection from Egg Inc API
- Historical data tracking
- Progress visualization
- Goal setting and tracking
- Player rankings

### Events and Contracts

- Real-time event tracking
- Contract information and rewards
- Prophecy Egg tracking
- Event notifications

### News

- Automated news gathering from various sources
- AI-powered content parsing and summarization
- News dashboard

## Setup and Installation

### Prerequisites

- .NET 8.0 SDK or later
- SQL Server (or SQL Server Express)
- Visual Studio 2022 or later (recommended) or Visual Studio Code
- Azure Functions Core Tools (for local function development)

### Database Setup

1. Create a SQL Server database named `db-egginc`
2. The application will automatically create the necessary tables on first run

### Application Setup

1. Clone the repository:
   ```
   git clone https://github.com/HemSoft/Egg-Inc-Tracker.git
   cd Egg-Inc-Tracker
   ```

2. Restore NuGet packages:
   ```
   dotnet restore
   ```

3. Build the solution:
   ```
   dotnet build
   ```

## Configuration

### Connection Strings

Update the connection strings in the following files:

- `sources/HemSoft.EggIncTracker.Dashboard.BlazorServer/appsettings.json`
- `sources/HemSoft.EggIncTracker.Api/appsettings.json`
- `sources/HemSoft.EggIncTracker.Functions/local.settings.json`

Example connection string:
```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=localhost;Initial Catalog=db-egginc;Integrated Security=True;Encrypt=False;Trust Server Certificate=True"
}
```

### Azure Functions Configuration

For the Azure Functions projects, update the `local.settings.json` files with appropriate settings:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}
```

### AI Configuration (Optional)

If using AI features, configure API keys in user secrets:

```
dotnet user-secrets set "AI:ApiKey" "your-api-key" --project sources/HemSoft.News.Functions
```

## Running the Application

### Running the Dashboard

```
cd sources/HemSoft.EggIncTracker.Dashboard.BlazorServer
dotnet run
```

The dashboard will be available at `https://localhost:5001`

### Running the API

```
cd sources/HemSoft.EggIncTracker.Api
dotnet run
```

The API will be available at `https://localhost:5000` with Swagger documentation at the root URL.

### Running the Azure Functions

```
cd sources/HemSoft.EggIncTracker.Functions
func start
```

## API Documentation

The API documentation is available through Swagger UI when running the API project. The API provides endpoints for:

- Player data
- Events
- Contracts
- Rankings
- Goals

## Database

### Main Database (SQL Server)

The main database contains tables for:

- Players
- Events
- Contracts
- PlayerContracts
- Goals
- MajPlayerRankings
- PlayerRankings

### News Database (SQLite)

The news database contains tables for:

- News sources
- News items
- News categories

## Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/my-feature`
3. Commit your changes: `git commit -am 'Add my feature'`
4. Push to the branch: `git push origin feature/my-feature`
5. Submit a pull request
