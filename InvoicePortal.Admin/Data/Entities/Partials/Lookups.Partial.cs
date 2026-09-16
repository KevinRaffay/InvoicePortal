using System.ComponentModel.DataAnnotations.Schema;
using InvoicePortal.Admin.Data.Abstractions;

namespace InvoicePortal.Admin.Data.Entities;

public partial class Bottler : IAuditable
{
    [NotMapped] public string DisplayName => $"{Id} - {Name}";
}

public partial class Payer : IAuditable
{
    [NotMapped] public string DisplayName => $"{Id} - {Name}";
}

public partial class SalesCenter : IAuditable
{
    [NotMapped] public string DisplayName => $"{Id} - {Name}";
}

public partial class Currency : IAuditable
{
    [NotMapped] public string DisplayName => Code;
}

public partial class Company : IAuditable
{
    [NotMapped] public string DisplayName => $"{Code} - {Name}";
}

public partial class Country : IAuditable
{
    [NotMapped] public string DisplayName => $"{Code} - {Name}";
}

public partial class BrandSegmentCategory : IAuditable
{
    [NotMapped] public string DisplayName => Name;
}
