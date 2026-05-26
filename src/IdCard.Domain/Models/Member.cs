namespace IdCard.Domain.Models;

public sealed class Member
{
    public string MemberId { get; set; } = string.Empty;
    public string SubscriberId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DateOfBirth { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string RelationshipCode { get; set; } = string.Empty;
    public string GroupNumber { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string PlanCode { get; set; } = string.Empty;
    public string EffectiveDate { get; set; } = string.Empty;
    public string TerminationDate { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PcpId { get; set; } = string.Empty;
    public string CopayOffice { get; set; } = string.Empty;
    public string CopaySpecialist { get; set; } = string.Empty;
    public string CopayUrgentCare { get; set; } = string.Empty;
    public string CopayER { get; set; } = string.Empty;
    public string DeductibleIndividual { get; set; } = string.Empty;
    public string DeductibleFamily { get; set; } = string.Empty;
    public string OutOfPocketMax { get; set; } = string.Empty;
    public string RxBinNumber { get; set; } = string.Empty;
    public string RxPcnNumber { get; set; } = string.Empty;
    public string RxGroupNumber { get; set; } = string.Empty;
    public string NetworkName { get; set; } = string.Empty;
}
