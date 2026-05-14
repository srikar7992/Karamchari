using Karamchari.Billing.DependencyInjection;
using Karamchari.Capability.DependencyInjection;
using Karamchari.Compensation.DependencyInjection;
using Karamchari.Core.DependencyInjection;
using Karamchari.FinancialOps.DependencyInjection;
using Karamchari.Forecasting.DependencyInjection;
using Karamchari.Governance.DependencyInjection;
using Karamchari.HR.DependencyInjection;
using Karamchari.Intelligence.DependencyInjection;
using Karamchari.Notifications.DependencyInjection;
using Karamchari.Payroll.DependencyInjection;
using Karamchari.Performance.DependencyInjection;
using Karamchari.PSA.DependencyInjection;
using Karamchari.Recruitment.DependencyInjection;
using Karamchari.TimeAttendance.DependencyInjection;
using Karamchari.Workflow.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Karamchari.Api.DependencyInjection;

public static class CapabilityRegistry
{
    public static IReadOnlyList<ICapabilityModule> GetModules(IConfiguration configuration)
    {
        return new ICapabilityModule[]
        {
            // Foundation
            new WorkflowModule(configuration),
            new NotificationsModule(configuration),
            
            // Workforce Ops
            new HRModule(configuration),
            new TimeAttendanceModule(configuration),
            new FinancialOpsModule(configuration),
            
            // Payroll
            new PayrollModule(configuration),
            
            // Hiring
            new RecruitmentModule(configuration),
            
            // Performance
            new PerformanceModule(configuration),
            
            // PSA
            new PSAModule(configuration),
            
            // Deferred / Future Modules
            new CapabilityModule(configuration),
            new IntelligenceModule(configuration),
            new GovernanceModule(configuration),
            new ForecastingModule(configuration),
            new CompensationModule(configuration),
            new BillingModule(configuration)
        };
    }
}
