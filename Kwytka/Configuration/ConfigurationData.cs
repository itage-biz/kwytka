namespace Kwytka.Configuration;

public sealed class ConfigurationData
{
    public string CountColumns { get; set; } = "налич,наявн";

    public List<PriceList> PriceLists { get; set; } = [];

    public bool IsSaleEnabled { get; set; }

    public string SalePageHtml { get; set; } = string.Empty;
}
