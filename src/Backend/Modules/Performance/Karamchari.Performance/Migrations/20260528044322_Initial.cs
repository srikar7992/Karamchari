using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Performance.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "__tenant__");

            migrationBuilder.CreateTable(
                name: "CalibrationBoardProjections",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CalibrationSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewCycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TotalEntries = table.Column<int>(type: "int", nullable: false),
                    TopPerformerCount = table.Column<int>(type: "int", nullable: false),
                    HighPerformerCount = table.Column<int>(type: "int", nullable: false),
                    MeetsExpectationsCount = table.Column<int>(type: "int", nullable: false),
                    BelowExpectationsCount = table.Column<int>(type: "int", nullable: false),
                    UnderPerformerCount = table.Column<int>(type: "int", nullable: false),
                    TotalPanelMembers = table.Column<int>(type: "int", nullable: false),
                    PanelMembersJoined = table.Column<int>(type: "int", nullable: false),
                    AdjustmentsMade = table.Column<int>(type: "int", nullable: false),
                    EntriesPendingFinalization = table.Column<int>(type: "int", nullable: false),
                    FinalizedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastRefreshedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalibrationBoardProjections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CalibrationSessions",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ReviewCycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BellCurvePolicy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FinalizedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalibrationSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CareerFrameworks",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerFrameworks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeGrowthPlans",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetLevelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetLevelDisplay = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeGrowthPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSkillInventoryItems",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeDisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CareerLevel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SkillCategoryName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CurrentProficiency = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TargetProficiency = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ProficiencyGap = table.Column<int>(type: "int", nullable: true),
                    LastAssessmentSource = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    LastAssessedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastRefreshedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeSkillInventoryItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSkillProfiles",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeSkillProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExportJobs",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ParametersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BlobUri = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeedbackRequests",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    SubjectEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeedbackProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsAnonymous = table.Column<bool>(type: "bit", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Context = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExpiresOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedbackRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeedbackSubmissions",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsAnonymous = table.Column<bool>(type: "bit", nullable: false),
                    AnonymityToken = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    StrengthsNarrative = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    GrowthAreasNarrative = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    OverallComment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Rating = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IdempotencyKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedbackSubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoalCycles",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoalCycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Goals",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    CycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerType = table.Column<int>(type: "int", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentGoalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    TargetValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrentProgress = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FinalScore = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Goals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KPIDefinitions",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Aggregation = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Polarity = table.Column<int>(type: "int", nullable: false),
                    Periodicity = table.Column<int>(type: "int", nullable: false),
                    Threshold_RedBelow = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Threshold_YellowBelow = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Threshold_GreenAbove = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    FormulaExpression = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KPIDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KPIResults",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    KPIDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodLabel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    RawValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    NormalizedScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OverriddenBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OverrideJustification = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ComputedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KPIResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KPISnapshots",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    KPIDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodLabel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    NormalizedScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Band = table.Column<int>(type: "int", nullable: false),
                    KPICode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KPIDisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SnapshotTakenUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KPISnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManagerDashboardProjections",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ManagerEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewCycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CycleName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TotalTeamGoals = table.Column<int>(type: "int", nullable: false),
                    OnTrackGoals = table.Column<int>(type: "int", nullable: false),
                    OffTrackGoals = table.Column<int>(type: "int", nullable: false),
                    OverdueGoals = table.Column<int>(type: "int", nullable: false),
                    GoalCompletionRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TotalReviewsRequired = table.Column<int>(type: "int", nullable: false),
                    ReviewsCompleted = table.Column<int>(type: "int", nullable: false),
                    ReviewsOverdue = table.Column<int>(type: "int", nullable: false),
                    PendingApprovals = table.Column<int>(type: "int", nullable: false),
                    KPIsAtRisk = table.Column<int>(type: "int", nullable: false),
                    KPIsOffTrack = table.Column<int>(type: "int", nullable: false),
                    PendingPromotionApprovals = table.Column<int>(type: "int", nullable: false),
                    PendingFeedbackRequests = table.Column<int>(type: "int", nullable: false),
                    LastRefreshedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagerDashboardProjections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Objectives",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    CycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerType = table.Column<int>(type: "int", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentObjectiveId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsStretch = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AggregatedScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Objectives", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OKRCycles",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OKRCycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceSnapshots",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewCycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CycleName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReviewPeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    ReviewPeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    CompositeScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PerformanceBucket = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GoalCompletionRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    OKRScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    KPIScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    PeerFeedbackScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    IsHighPerformer = table.Column<bool>(type: "bit", nullable: false),
                    IsAtRetentionRisk = table.Column<bool>(type: "bit", nullable: false),
                    MaterializedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromotionPipelineItems",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PromotionRecommendationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeDisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CurrentCareerLevel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProposedCareerLevel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CurrentStage = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReadinessScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DaysInCurrentStage = table.Column<int>(type: "int", nullable: false),
                    ReviewCycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastRefreshedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionPipelineItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromotionRecommendations",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NominatedByManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentGrade = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProposedGrade = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CurrentTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProposedTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReadinessScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ProposedEffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApprovedEffectiveDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionRecommendations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReviewAssignments",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    CycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevieweeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsFinalized = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    ReviewerSlots = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewAssignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReviewCycles",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewPeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    ReviewPeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    SubmissionDeadline = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    GoalCycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OKRCycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TotalAssignments = table.Column<int>(type: "int", nullable: false),
                    CompletedAssignments = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewCycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReviewSubmissions",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevieweeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewerRole = table.Column<int>(type: "int", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ComputedScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    SubmittedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AcknowledgedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IdempotencyKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    ReopenHistory = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewSubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReviewTaskInboxItems",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReviewerEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewCycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CycleName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RevieweeEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevieweeDisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReviewerRole = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Deadline = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsOverdue = table.Column<bool>(type: "bit", nullable: false),
                    HoursUntilDeadline = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    LastRefreshedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewTaskInboxItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReviewTemplates",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ApplicableCycleType = table.Column<int>(type: "int", nullable: false),
                    IncludesSelfReview = table.Column<bool>(type: "bit", nullable: false),
                    IncludesManagerReview = table.Column<bool>(type: "bit", nullable: false),
                    IncludesPeerReview = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Sections = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SkillTaxonomies",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillTaxonomies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TalentHeatmapEntries",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeDisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CareerLevel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReviewCycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CycleName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CompositeScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PerformanceBucket = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PotentialRating = table.Column<int>(type: "int", nullable: false),
                    NineBoxPosition = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsHighPerformer = table.Column<bool>(type: "bit", nullable: false),
                    IsAtRetentionRisk = table.Column<bool>(type: "bit", nullable: false),
                    IsPromotionReady = table.Column<bool>(type: "bit", nullable: false),
                    IsSuccessionCandidate = table.Column<bool>(type: "bit", nullable: false),
                    MaterializedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastRefreshedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TalentHeatmapEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TeamGoalSummaries",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ManagerEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GoalCycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CycleName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TotalGoals = table.Column<int>(type: "int", nullable: false),
                    CompletedGoals = table.Column<int>(type: "int", nullable: false),
                    InProgressGoals = table.Column<int>(type: "int", nullable: false),
                    OffTrackGoals = table.Column<int>(type: "int", nullable: false),
                    NotStartedGoals = table.Column<int>(type: "int", nullable: false),
                    CompletionRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    AverageProgress = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TeamSize = table.Column<int>(type: "int", nullable: false),
                    EmployeesWithNoGoals = table.Column<int>(type: "int", nullable: false),
                    LastRefreshedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamGoalSummaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CalibrationEntries",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreCalibrationScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PreCalibrationBucket = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CalibratedScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CalibratedBucket = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsFinalized = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalibrationEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalibrationEntries_CalibrationSessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "__tenant__",
                        principalTable: "CalibrationSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CalibrationPanelMembers",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalibrationPanelMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalibrationPanelMembers_CalibrationSessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "__tenant__",
                        principalTable: "CalibrationSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CareerTracks",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerTracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareerTracks_CareerFrameworks_FrameworkId",
                        column: x => x.FrameworkId,
                        principalSchema: "__tenant__",
                        principalTable: "CareerFrameworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DevelopmentActions",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrowthPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TargetCompletionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompletionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CompletedOnDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevelopmentActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DevelopmentActions_EmployeeGrowthPlans_GrowthPlanId",
                        column: x => x.GrowthPlanId,
                        principalSchema: "__tenant__",
                        principalTable: "EmployeeGrowthPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSkillAssessments",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    AssessedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Evidence = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AssessedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeSkillAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSkillAssessments_EmployeeSkillProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalSchema: "__tenant__",
                        principalTable: "EmployeeSkillProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoalProgressUpdates",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GoalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ProgressPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoalProgressUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoalProgressUpdates_Goals_GoalId",
                        column: x => x.GoalId,
                        principalSchema: "__tenant__",
                        principalTable: "Goals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoalRevisionRecords",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GoalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousTitle = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    PreviousTarget = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NewTitle = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    NewTarget = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RevisedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoalRevisionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoalRevisionRecords_Goals_GoalId",
                        column: x => x.GoalId,
                        principalSchema: "__tenant__",
                        principalTable: "Goals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KeyResults",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectiveId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Target = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    DependsOnKrId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ProgressPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ConfidenceLevel = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LastCheckInUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KeyResults_Objectives_ObjectiveId",
                        column: x => x.ObjectiveId,
                        principalSchema: "__tenant__",
                        principalTable: "Objectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromotionApprovalActions",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecommendationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Stage = table.Column<int>(type: "int", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Approved = table.Column<bool>(type: "bit", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ActedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionApprovalActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromotionApprovalActions_PromotionRecommendations_RecommendationId",
                        column: x => x.RecommendationId,
                        principalSchema: "__tenant__",
                        principalTable: "PromotionRecommendations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReviewResponses",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionType = table.Column<int>(type: "int", nullable: false),
                    RatingValue = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    TextValue = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewResponses_ReviewSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalSchema: "__tenant__",
                        principalTable: "ReviewSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkillCategories",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    TaxonomyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillCategories_SkillTaxonomies_TaxonomyId",
                        column: x => x.TaxonomyId,
                        principalSchema: "__tenant__",
                        principalTable: "SkillTaxonomies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CalibrationAdjustmentRecords",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CalibrationEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    NewScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PreviousBucket = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NewBucket = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AdjustedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AdjustedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalibrationAdjustmentRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalibrationAdjustmentRecords_CalibrationEntries_CalibrationEntryId",
                        column: x => x.CalibrationEntryId,
                        principalSchema: "__tenant__",
                        principalTable: "CalibrationEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CareerLevels",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrackId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompetencyRequirements = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareerLevels_CareerTracks_TrackId",
                        column: x => x.TrackId,
                        principalSchema: "__tenant__",
                        principalTable: "CareerTracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KRCheckIns",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KeyResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ConfidenceLevel = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KRCheckIns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KRCheckIns_KeyResults_KeyResultId",
                        column: x => x.KeyResultId,
                        principalSchema: "__tenant__",
                        principalTable: "KeyResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Skills",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Domain = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ProficiencyDescriptors = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Skills_SkillCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "__tenant__",
                        principalTable: "SkillCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalibrationAdjustmentRecords_CalibrationEntryId",
                schema: "__tenant__",
                table: "CalibrationAdjustmentRecords",
                column: "CalibrationEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_CalibrationBoardProjections_Cycle",
                schema: "__tenant__",
                table: "CalibrationBoardProjections",
                columns: new[] { "TenantId", "ReviewCycleId" });

            migrationBuilder.CreateIndex(
                name: "UIX_CalibrationBoardProjections_Session",
                schema: "__tenant__",
                table: "CalibrationBoardProjections",
                columns: new[] { "TenantId", "CalibrationSessionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalibrationEntries_Session_Employee",
                schema: "__tenant__",
                table: "CalibrationEntries",
                columns: new[] { "SessionId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalibrationPanelMembers_SessionId",
                schema: "__tenant__",
                table: "CalibrationPanelMembers",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CalibrationSessions_TenantId_ReviewCycleId_Name",
                schema: "__tenant__",
                table: "CalibrationSessions",
                columns: new[] { "TenantId", "ReviewCycleId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CareerLevels_Track_Order",
                schema: "__tenant__",
                table: "CareerLevels",
                columns: new[] { "TrackId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CareerTracks_FrameworkId",
                schema: "__tenant__",
                table: "CareerTracks",
                column: "FrameworkId");

            migrationBuilder.CreateIndex(
                name: "IX_DevelopmentActions_GrowthPlanId",
                schema: "__tenant__",
                table: "DevelopmentActions",
                column: "GrowthPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeGrowthPlans_TenantId_EmployeeId_Status",
                schema: "__tenant__",
                table: "EmployeeGrowthPlans",
                columns: new[] { "TenantId", "EmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SkillAssessments_Profile_Skill_Date",
                schema: "__tenant__",
                table: "EmployeeSkillAssessments",
                columns: new[] { "ProfileId", "SkillId", "AssessedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSkillInventoryItems_Dept_Domain",
                schema: "__tenant__",
                table: "EmployeeSkillInventoryItems",
                columns: new[] { "TenantId", "Department", "Domain" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSkillInventoryItems_Skill_Proficiency",
                schema: "__tenant__",
                table: "EmployeeSkillInventoryItems",
                columns: new[] { "TenantId", "SkillId", "CurrentProficiency" });

            migrationBuilder.CreateIndex(
                name: "UIX_EmployeeSkillInventoryItems_Employee_Skill",
                schema: "__tenant__",
                table: "EmployeeSkillInventoryItems",
                columns: new[] { "TenantId", "EmployeeId", "SkillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSkillProfiles_TenantId_EmployeeId",
                schema: "__tenant__",
                table: "EmployeeSkillProfiles",
                columns: new[] { "TenantId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_IdempotencyKey",
                schema: "__tenant__",
                table: "ExportJobs",
                columns: new[] { "TenantId", "IdempotencyKey" });

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_Status_CreatedAt",
                schema: "__tenant__",
                table: "ExportJobs",
                columns: new[] { "TenantId", "Status", "CreatedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_Tenant_Employee_Status",
                schema: "__tenant__",
                table: "ExportJobs",
                columns: new[] { "TenantId", "RequestedByEmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackRequests_TenantId_SubjectEmployeeId_Status",
                schema: "__tenant__",
                table: "FeedbackRequests",
                columns: new[] { "TenantId", "SubjectEmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackSubmissions_TenantId_IdempotencyKey",
                schema: "__tenant__",
                table: "FeedbackSubmissions",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackSubmissions_TenantId_SubjectEmployeeId_Status",
                schema: "__tenant__",
                table: "FeedbackSubmissions",
                columns: new[] { "TenantId", "SubjectEmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_GoalCycles_TenantId_Name_Type_StartDate",
                schema: "__tenant__",
                table: "GoalCycles",
                columns: new[] { "TenantId", "Name", "Type", "StartDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoalProgressUpdates_GoalId",
                schema: "__tenant__",
                table: "GoalProgressUpdates",
                column: "GoalId");

            migrationBuilder.CreateIndex(
                name: "IX_GoalRevisionRecords_GoalId",
                schema: "__tenant__",
                table: "GoalRevisionRecords",
                column: "GoalId");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_TenantId_CycleId_OwnerId",
                schema: "__tenant__",
                table: "Goals",
                columns: new[] { "TenantId", "CycleId", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_KeyResults_ObjectiveId",
                schema: "__tenant__",
                table: "KeyResults",
                column: "ObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_KPIDefinitions_TenantId_Code",
                schema: "__tenant__",
                table: "KPIDefinitions",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KPIResults_TenantId_IdempotencyKey",
                schema: "__tenant__",
                table: "KPIResults",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KPIResults_TenantId_KPIDefinitionId_EmployeeId_PeriodLabel",
                schema: "__tenant__",
                table: "KPIResults",
                columns: new[] { "TenantId", "KPIDefinitionId", "EmployeeId", "PeriodLabel" });

            migrationBuilder.CreateIndex(
                name: "IX_KPISnapshots_TenantId_EmployeeId_KPIDefinitionId_PeriodLabel",
                schema: "__tenant__",
                table: "KPISnapshots",
                columns: new[] { "TenantId", "EmployeeId", "KPIDefinitionId", "PeriodLabel" });

            migrationBuilder.CreateIndex(
                name: "IX_KRCheckIns_KeyResultId",
                schema: "__tenant__",
                table: "KRCheckIns",
                column: "KeyResultId");

            migrationBuilder.CreateIndex(
                name: "UIX_ManagerDashboardProjections_Manager_Cycle",
                schema: "__tenant__",
                table: "ManagerDashboardProjections",
                columns: new[] { "TenantId", "ManagerEmployeeId", "ReviewCycleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Objectives_TenantId_CycleId_OwnerId",
                schema: "__tenant__",
                table: "Objectives",
                columns: new[] { "TenantId", "CycleId", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceSnapshots_TenantId_EmployeeId_ReviewCycleId",
                schema: "__tenant__",
                table: "PerformanceSnapshots",
                columns: new[] { "TenantId", "EmployeeId", "ReviewCycleId" });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceSnapshots_TenantId_IsHighPerformer_IsAtRetentionRisk",
                schema: "__tenant__",
                table: "PerformanceSnapshots",
                columns: new[] { "TenantId", "IsHighPerformer", "IsAtRetentionRisk" });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionApprovalActions_RecommendationId",
                schema: "__tenant__",
                table: "PromotionApprovalActions",
                column: "RecommendationId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionPipelineItems_Cycle_Status",
                schema: "__tenant__",
                table: "PromotionPipelineItems",
                columns: new[] { "TenantId", "ReviewCycleId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UIX_PromotionPipelineItems_Recommendation",
                schema: "__tenant__",
                table: "PromotionPipelineItems",
                columns: new[] { "TenantId", "PromotionRecommendationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRecommendations_TenantId_EmployeeId_Status",
                schema: "__tenant__",
                table: "PromotionRecommendations",
                columns: new[] { "TenantId", "EmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewAssignments_TenantId_CycleId_RevieweeId",
                schema: "__tenant__",
                table: "ReviewAssignments",
                columns: new[] { "TenantId", "CycleId", "RevieweeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewCycles_TenantId_Name_ReviewPeriodStart",
                schema: "__tenant__",
                table: "ReviewCycles",
                columns: new[] { "TenantId", "Name", "ReviewPeriodStart" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewResponses_SubmissionId",
                schema: "__tenant__",
                table: "ReviewResponses",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewSubmissions_TenantId_AssignmentId_ReviewerId",
                schema: "__tenant__",
                table: "ReviewSubmissions",
                columns: new[] { "TenantId", "AssignmentId", "ReviewerId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewSubmissions_TenantId_IdempotencyKey",
                schema: "__tenant__",
                table: "ReviewSubmissions",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewTaskInboxItems_Reviewer_Priority",
                schema: "__tenant__",
                table: "ReviewTaskInboxItems",
                columns: new[] { "TenantId", "ReviewerEmployeeId", "Priority" });

            migrationBuilder.CreateIndex(
                name: "UIX_ReviewTaskInboxItems_Assignment",
                schema: "__tenant__",
                table: "ReviewTaskInboxItems",
                columns: new[] { "TenantId", "ReviewAssignmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillCategories_TaxonomyId",
                schema: "__tenant__",
                table: "SkillCategories",
                column: "TaxonomyId");

            migrationBuilder.CreateIndex(
                name: "IX_Skills_CategoryId",
                schema: "__tenant__",
                table: "Skills",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Skills_Tenant_Name_Domain",
                schema: "__tenant__",
                table: "Skills",
                columns: new[] { "TenantId", "Name", "Domain" });

            migrationBuilder.CreateIndex(
                name: "IX_TalentHeatmapEntries_Dept_Cycle",
                schema: "__tenant__",
                table: "TalentHeatmapEntries",
                columns: new[] { "TenantId", "Department", "ReviewCycleId" });

            migrationBuilder.CreateIndex(
                name: "IX_TalentHeatmapEntries_NineBox_Cycle",
                schema: "__tenant__",
                table: "TalentHeatmapEntries",
                columns: new[] { "TenantId", "NineBoxPosition", "ReviewCycleId" });

            migrationBuilder.CreateIndex(
                name: "UIX_TalentHeatmapEntries_Employee_Cycle",
                schema: "__tenant__",
                table: "TalentHeatmapEntries",
                columns: new[] { "TenantId", "EmployeeId", "ReviewCycleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UIX_TeamGoalSummaries_Manager_Cycle",
                schema: "__tenant__",
                table: "TeamGoalSummaries",
                columns: new[] { "TenantId", "ManagerEmployeeId", "GoalCycleId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalibrationAdjustmentRecords",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "CalibrationBoardProjections",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "CalibrationPanelMembers",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "CareerLevels",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "DevelopmentActions",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "EmployeeSkillAssessments",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "EmployeeSkillInventoryItems",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "ExportJobs",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "FeedbackRequests",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "FeedbackSubmissions",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "GoalCycles",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "GoalProgressUpdates",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "GoalRevisionRecords",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "KPIDefinitions",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "KPIResults",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "KPISnapshots",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "KRCheckIns",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "ManagerDashboardProjections",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "OKRCycles",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "PerformanceSnapshots",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "PromotionApprovalActions",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "PromotionPipelineItems",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "ReviewAssignments",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "ReviewCycles",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "ReviewResponses",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "ReviewTaskInboxItems",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "ReviewTemplates",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Skills",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "TalentHeatmapEntries",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "TeamGoalSummaries",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "CalibrationEntries",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "CareerTracks",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "EmployeeGrowthPlans",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "EmployeeSkillProfiles",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Goals",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "KeyResults",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "PromotionRecommendations",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "ReviewSubmissions",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "SkillCategories",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "CalibrationSessions",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "CareerFrameworks",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Objectives",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "SkillTaxonomies",
                schema: "__tenant__");
        }
    }
}
