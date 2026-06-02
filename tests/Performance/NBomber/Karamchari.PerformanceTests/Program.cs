// NBomber performance tests entry point.
// Run individual load tests via:
//   dotnet test --filter AuthenticationLoadTest
//   dotnet test --filter RecruitmentJourneyLoadTest
//
// Both tests are marked [Fact(Skip=...)] and require a live Karamchari API instance.
// Set KARAMCHARI_BASE_URL environment variable to point at the target environment.
