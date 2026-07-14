namespace Kwytka.Configuration;

public sealed class ConfigurationData
{
    public List<PriceList> PriceLists { get; set; } = [];

    public bool IsSaleEnabled { get; set; }

    public string SalePageHtml { get; set; } = string.Empty;
}
