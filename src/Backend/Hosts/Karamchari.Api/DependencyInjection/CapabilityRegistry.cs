using Karamchari.AssetManagement.DependencyInjection;
using Karamchari.Billing.DependencyInjection;
using Karamchari.Capability.DependencyInjection;
using Karamchari.Compensation.DependencyInjection;
using Karamchari.Compliance.DependencyInjection;
using Karamchari.Core.DependencyInjection;
using Karamchari.DataMigration.DependencyInjection;
using Karamchari.Engagement.DependencyInjection;
using Karamchari.FinancialOps.DependencyInjection;
using Karamchari.Forecasting.DependencyInjection;
using Karamchari.Governance.DependencyInjection;
using Karamchari.Helpdesk.DependencyInjection;
using Karamchari.HR.DependencyInjection;
using Karamchari.Identity.Infrastructure;
using Karamchari.Integration.DependencyInjection;
using Karamchari.Intelligence.DependencyInjection;
using Karamchari.Notifications.DependencyInjection;
using Karamchari.Payroll.DependencyInjection;
using Karamchari.Performance.DependencyInjection;
using Karamchari.PlatformIntelligence.DependencyInjection;
using Karamchari.PSA.DependencyInjection;
using Karamchari.Recruitment.DependencyInjection;
using Karamchari.Succession.DependencyInjection;
using Karamchari.TimeAttendance.DependencyInjection;
using Karamchari.Workflow.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Karamchari.Api.DependencyInjection;

public static class CapabilityRegistry
{
    public static ICapabilityModule[] GetModules(IConfiguration configuration)
    {
        return new ICapabilityModule[]
        {
            new HRModule(configuration),
            new PayrollModule(configuration),
            new RecruitmentModule(configuration),
            new TimeAttendanceModule(configuration),
            new NotificationsModule(configuration),
            new WorkflowModule(configuration),
            new PSAModule(configuration),
            new PerformanceModule(configuration),
            new IntelligenceModule(configuration),
            new DataMigrationModule(configuration),
            // Identity is registered manually in Program.cs to avoid authentication scheme collisions
            new FinancialOpsModule(configuration),
            new GovernanceModule(configuration),
            new ForecastingModule(configuration),
            new CompensationModule(configuration),
            new BillingModule(configuration),
            new CapabilityModule(configuration),
            new ComplianceModule(configuration),
            new PlatformIntelligenceModule(configuration),
            new HelpdeskModule(configuration),
            new AssetManagementModule(configuration),
            new EngagementModule(configuration),
            new SuccessionModule(configuration),
            new IntegrationModule(configuration),
        };
    }
}
