namespace NaraEyes.Application.Contracts.Models.Bulkoperations
{
    public sealed record GroupedDeviceFilterViewModel(Guid?BranchId,Guid?SupervisionId,string? SearchTerm);
}
