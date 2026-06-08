namespace Karamchari.Onboarding.Domain;

public record OnboardingCaseId(Guid Value);
public record OnboardingTaskId(Guid Value);
public record OnboardingDocumentId(Guid Value);
public record OnboardingTemplateId(Guid Value);

public enum OnboardingCaseType
{
    NewHire = 1,
    Crossboard = 2,   // internal transfer
    Offboard = 3,
}

public enum OnboardingCaseStatus
{
    Pending = 1,      // created, not yet started
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
}

public enum TaskCategory
{
    HR = 1,
    IT = 2,
    Manager = 3,
    Facilities = 4,
    Finance = 5,
    Legal = 6,
}

public enum TaskStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Skipped = 4,
}

public enum DocumentType
{
    IDProof = 1,
    AddressProof = 2,
    BankDetails = 3,
    TaxForm = 4,
    EducationCertificate = 5,
    PreviousEmploymentRelievingLetter = 6,
    EmploymentContract = 7,
    NDAAgreement = 8,
    MedicalFitnessCertificate = 9,
}

public enum DocumentStatus
{
    Pending = 1,
    Submitted = 2,
    Verified = 3,
    Rejected = 4,
}
