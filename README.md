# LungFirst AI Flagging Prototype

## Overview

This prototype is a clinical decision support tool for respiratory screening and invitation prioritization. It identifies patients at higher risk for follow-up and displays them in a simple, color-coded invitation queue.

The goal is to make it easy for clinical teams to review a prioritized shortlist of patients, understand why they were flagged, and optionally use a sample queue for demonstration without needing a live database.

## What it does

- Runs a screening algorithm on patient records
- Applies exclusion rules for patients who should not enter the screening queue
- Scores patients from 3 to 7 and groups them into green, amber, and red risk bands
- Presents a prioritized invitation queue
- Lets users send an invitation action per patient
- Includes a demo queue button so medical teams can review example cases immediately

## Clinical terminology

This tool is best described as:

- Clinical decision support
- Population health screening
- Risk stratification
- Patient prioritization for invitation
- Respiratory screening flagging

It is not a diagnostic system. It is designed to flag patients for follow-up invitations only.

## How clinical users can use it

- Press `Analyze database` to run the screening logic against the available patient dataset
- Review the invitation queue sorted by risk band and final score
- Use the color legend to interpret the queue:
  - Green: standard invite
  - Amber: priority invite
  - Red: urgent priority invite
- Click `Invite` to mark a patient as invited
- Use `Show example queue` to view a predefined demonstration queue without database access

## Technical stack

The project is built as a server-side Ikon application with the following technologies:

- C# / .NET for the application logic and UI
- Ikon App framework for reactive server-driven UI and client synchronization
- JSON demo data for offline/demo use
- Optional PostgreSQL backend for real patient database integration

### Key code areas

- `app/Ikon.App.MyProject/MyProjectApp.cs`
  - Main application entrypoint
  - UI definition, buttons, queue layout, state management
  - Database analysis workflow and example queue display

- `app/Ikon.App.MyProject/Clinical/`
  - `CopdRiskScorer.cs` — scoring logic and band mapping
  - `HardGateEvaluator.cs` — exclusion gate logic
  - `CopdPatientRecord.cs` — patient record model

- `app/Ikon.App.MyProject/Data/`
  - `IPatientRecordRepository.cs` — repository interface
  - `DemoPatientRecordRepository.cs` — bundled JSON data loader
  - `PostgresPatientRecordRepository.cs` — PostgreSQL loader
  - `PatientRecordRepositoryFactory.cs` — chooses demo or PostgreSQL source
  - `copd_demo_patients.json` — bundled demo patient dataset

## How the tool is built

- The server hosts the UI and maintains reactive state for each client
- UI components are declared in C# and streamed to the browser
- `Analyze database` loads patient records, evaluates risk, and builds the queue
- `Show example queue` loads predefined sample patients directly in the UI
- Invitations are tracked in-memory per session using reactive state

## Data handling

- The app can work with demo JSON data by default
- If a PostgreSQL connection is configured, it will use the database instead
- The browser never connects directly to the database
- All patient data access happens on the server side

## Scaling and future integration

To scale this tool for larger databases or additional disease monitoring:

- Add real EHR / clinical data connectors
- Use a hosted database or data warehouse for large patient cohorts
- Extend the scorer with additional conditions and disease-specific rules
- Keep the UI as a clinician-friendly prioritization dashboard

## Deployment note

If the app is deployed and the button or data do not match expectations, it usually means the deployed build is not the latest version or the server configuration differs from the source code. The sample queue button and demo data are built into the app; they do not require user database access.

---

For clinical audiences, this prototype is a way to preview how an invitation and screening workflow can be surfaced to care teams, while technical teams can use the same app as a foundation for a more connected production deployment.
