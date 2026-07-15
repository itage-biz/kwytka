namespace Kwytka.Configuration;

public sealed class ConfigurationData
{
    public List<PriceList> PriceLists { get; set; } = [];

    public bool IsSaleEnabled { get; set; }

    public string PriceListPrefix { get; set; } = string.Empty;

    public string SalePageHtml { get; set; } = string.Empty;

    public ConfigurationData Clone() => new()
    {
        PriceLists = PriceLists.Select(priceList => new PriceList
        {
            Slug = priceList.Slug,
            Title = priceList.Title,
            HtmlContent = priceList.HtmlContent
        }).ToList(),
        IsSaleEnabled = IsSaleEnabled,
        PriceListPrefix = PriceListPrefix,
        SalePageHtml = SalePageHtml
    };
}
