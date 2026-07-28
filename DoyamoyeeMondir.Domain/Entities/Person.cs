using DoyamoyeeMondir.Domain.Common;
using DoyamoyeeMondir.Domain.Enums;

namespace DoyamoyeeMondir.Domain.Entities;

public class Person : BaseAuditableEntity
{
    public string NameBn { get; set; } = string.Empty;

    public string? NameEn { get; set; }

    public string? MobileNumber { get; set; }

    public string? Email { get; set; }

    public Gender? Gender { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? FatherName { get; set; }

    public string? MotherName { get; set; }

    public string? Occupation { get; set; }

    public string? NationalIdNumber { get; set; }

    public string? AddressBn { get; set; }

    public string? AddressEn { get; set; }

    public string? PhotoPath { get; set; }

    public bool IsActive { get; set; } = true;
}