namespace NetScrapper;

public record ConfigPage
{
    public string url { get; set; }
    public int interval { get; set; }
    public List<string> hours { get; set; }
}