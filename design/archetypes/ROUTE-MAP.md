# Route → Archetype Map

Every screen in `docs/product/web-app-screen-inventory.md` mapped to one archetype.
No screen ships without a row here. New screens add a row in the same change that
adds the route. A screen that fits no archetype is a doctrine amendment discussion
(§11), not a free pass.

Implemented screens (karamchari-web) are marked ●.

## Shell & Cross-Cutting

| Screen | Archetype |
|---|---|
| Login / Register / Session | administration |
| Global Search | registry |
| Notification Center ● | timeline |
| Approvals Inbox (Unified) ● | approval |
| My Profile | record-detail |

## Employee Self-Service

| Screen | Archetype |
|---|---|
| My Dashboard ● | dashboard |
| My Attendance ● (Attendance Ops) | command-center |
| My Leave | registry |
| My Payslips & Pay | registry |
| My Reimbursements | registry |
| My Loans & Advances | registry |
| My Goals & Reviews | case |
| My Skills & Learning | record-detail |
| My Assets | registry |
| My Helpdesk Tickets | case |

## Manager Workspace

| Screen | Archetype |
|---|---|
| Manager Dashboard ● | dashboard |
| Team Attendance Board ● | command-center |
| Regularization Approval Queue | approval |
| Team Leave Calendar & Approvals | approval |
| Review Inbox & Team Goals | approval |
| Talent Heatmap & Promotion Pipeline (Team) | topology |
| Team Risk Panel | intelligence |

## Workforce Management (HR Core)

| Screen | Archetype |
|---|---|
| Employee Directory ● | registry |
| Employee Detail (360) ● | record-detail |
| Employee Onboard Form | record-detail |
| Org Structure Admin (8 screens) | administration |
| Position Management | registry |
| Org Chart & Workforce Graph | topology |
| Org Scenario Planner | builder |
| HR Report Center | administration |

## Onboarding & Offboarding

| Screen | Archetype |
|---|---|
| Onboarding Case List & Detail ● | case |
| Onboarding Template Admin | administration |
| Offboarding View | case |

## Recruitment

| Screen | Archetype |
|---|---|
| Requisition List / Detail / Create | registry |
| Candidate Pipeline ● | registry |
| Candidate Detail | record-detail |
| Offer Management | approval |
| Recruitment Analytics | analytics |

## Time & Attendance

| Screen | Archetype |
|---|---|
| Attendance Records Browser | registry |
| Attendance Health Dashboard | analytics |
| Attendance Policy Admin | administration |
| Shift Definition Admin ● | administration |
| Biometric Device Admin | administration |
| Biometric Enrollment | case |
| Geo Zone Admin | administration |
| Period Finalization Console ● | command-center |
| Attendance Reports (7) | analytics |
| Timesheets | approval |

## Scheduling

| Screen | Archetype |
|---|---|
| Roster List & Roster Builder ● | builder |
| Assignment Console | builder |
| Shift Swap Center | approval |
| Coverage Rules Admin | administration |
| Roster Skills & Availability | administration |

## Leave Administration

| Screen | Archetype |
|---|---|
| Leave Type & Policy Admin ● | administration |
| Accrual & Carry-Forward Console | command-center |
| Approval SLA & Escalation Admin ● | administration |
| Leave Investigation & Absence Cases | case |
| Leave Liability & Statutory Reports | analytics |

## Payroll

| Screen | Archetype |
|---|---|
| Payroll Cockpit ● | command-center |
| Payroll Run Console ● | command-center |
| Salary Structure Admin | administration |
| Tax & Statutory Config | administration |
| IT Declaration Review | approval |
| Payslip Browser | registry |
| Arrears Console | approval |
| Corrections Console | approval |
| Disbursement Center | command-center |
| Full & Final Settlement | approval |
| Reimbursement Admin | approval |
| Loan Admin | approval |
| Variable Pay Console | approval |
| Salary Revision Console | approval |
| Payroll Simulation Lab | builder |
| Payroll Compliance Filings | registry |

## Compensation

| Screen | Archetype |
|---|---|
| Compensation Bands | administration |
| Compensation Records | registry |
| Pay Equity Report | analytics |
| Bonus Plans | case |
| Merit Matrix & Budget Pools | builder |

## Billing, PSA & Financial Ops

| Screen | Archetype |
|---|---|
| Billing Contracts & Rate Cards | registry |
| Billable Entries & Invoices | registry |
| Invoice Detail | record-detail |
| Collections Workbench | case |
| Revenue Forecast | analytics |
| PSA: Clients & Projects | registry |
| Project Profitability | analytics |
| Expense Claims Admin | approval |

## Capability & Learning

| Screen | Archetype |
|---|---|
| Skill Catalog Admin | administration |
| Role Requirements Admin | administration |
| Employee Capability Profile | record-detail |
| Gap Analysis | analytics |
| Learning Catalog Admin & Enrollments | registry |
| Internal Mobility Marketplace | registry |
| Career Readiness Board | analytics |

## Succession & Career Development

| Screen | Archetype |
|---|---|
| Critical Roles Register | registry |
| Succession Plan Workspace | case |
| Bench Strength & 9-Box ● (Succession Workspace) | topology |
| Career Path Designer | builder |

## Performance

| Screen | Archetype |
|---|---|
| Goal Cycle Admin | administration |
| KPI Management | registry |
| Review Cycle Admin | administration |
| Review Submission | case |
| 360 Feedback | case |
| Calibration Board | topology |
| Promotion Pipeline (HR) | approval |
| Skill Taxonomy & Growth Plans | administration |

## Engagement

| Screen | Archetype |
|---|---|
| Survey Studio | builder |
| Recognition | timeline |
| Announcements | registry |
| Polls | registry |

## Helpdesk

| Screen | Archetype |
|---|---|
| Agent Ticket Queue | case |
| Category & Escalation Admin | administration |
| Knowledge Base Studio | builder |

## Asset Management

| Screen | Archetype |
|---|---|
| Asset Registry | registry |
| Asset Detail | record-detail |
| Assignment & Lifecycle | registry |
| Maintenance & Depreciation | registry |
| Asset Category Admin | administration |

## Compliance & Governance

| Screen | Archetype |
|---|---|
| Compliance Dashboard ● | dashboard |
| Retention Policy Admin | administration |
| Legal Hold Console | case |
| Regulatory Register | registry |
| Policy Violations Queue | case |
| Audit Package Builder | builder |
| Compliance Gate Tester | administration |
| Audit Trail Explorer | timeline |
| Governance: SLOs / Incidents / Schemas | administration |

## Workforce Intelligence & Strategy

| Screen | Archetype |
|---|---|
| Workforce Intelligence Dashboard | dashboard |
| Employee Risk Profile | record-detail |
| Strategy Workspace | intelligence |
| Intervention Tracker | registry |

## Platform Intelligence

| Screen | Archetype |
|---|---|
| Decision Center | intelligence |
| Scenario & Simulation Lab | intelligence |
| Optimization Console | intelligence |
| Executive Digest | intelligence |
| Risk & Recommendation Board | intelligence |

## Workforce Planning (Forecasting)

| Screen | Archetype |
|---|---|
| Demand & Supply Forecast Board | analytics |
| WFP Scenario Planner | builder |
| Forecast Accuracy Monitor | analytics |

## Executive Area

| Screen | Archetype |
|---|---|
| Executive Dashboard ● | dashboard |

## Administration & Platform Operations

| Screen | Archetype |
|---|---|
| Tenant Administration | administration |
| User & Role Administration | administration |
| Workflow Definition Studio ● | builder |
| Webhook & Integration Admin | administration |
| Event Catalog Browser | registry |
| Endpoint Catalog Viewer | registry |
| Data Migration Console | command-center |
| Ops Health & Metrics | command-center |
| System Pulse ● | command-center |
| Notification Template Admin | administration |

## Distribution

| Archetype | Screens (approx, multi-screen rows expanded) |
|---|---|
| administration | 34 |
| registry | 26 |
| approval | 15 |
| analytics | 14 |
| case | 13 |
| command-center | 9 |
| builder | 9 |
| record-detail | 9 |
| intelligence | 7 |
| dashboard | 6 |
| topology | 4 |
| timeline | 3 |

Administration + registry = 40% of the platform. Uniformity there pays for the
craft spent on record-detail and dashboard.
