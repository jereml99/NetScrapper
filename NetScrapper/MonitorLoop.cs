using System.IO.Compression;
using System.IO.Hashing;
using System.Net;
using System.Text;
using System.Security.Cryptography;


namespace NetScrapper;

public class MonitorLoop
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly ILogger<MonitorLoop> _logger;
    private readonly IConfiguration _configuration;
    private readonly CancellationToken _cancellationToken;

    public MonitorLoop(
        IBackgroundTaskQueue taskQueue,
        ILogger<MonitorLoop> logger,
        IHostApplicationLifetime applicationLifetime,
        IConfiguration configuration
        )
    {
        
        _taskQueue = taskQueue;
        _logger = logger;
        _configuration = configuration;
        _cancellationToken = applicationLifetime.ApplicationStopping;
    }

    public void StartMonitorLoop()
    {
        _logger.LogInformation($"{nameof(MonitorAsync)} loop is starting.");

        Task.Run(async () => await MonitorAsync());
    }

    private async ValueTask MonitorAsync()
    {
        var i = 0;
        while (!_cancellationToken.IsCancellationRequested)
        {
            var keyStroke = Console.ReadKey();
            var pages = _configuration.GetSection("pages").Get<List<ConfigPage>>();
            if (keyStroke.Key == ConsoleKey.W)
            {
                await _taskQueue.QueueBackgroundWorkItemAsync(BuildTask(pages[(i++)%2].url));
            }
        }
    }

    private Func<CancellationToken, ValueTask> BuildTask(string url)
    {
        return async token =>
        {
            var downloadPage = DownloadPage(url);
            downloadPage.ContinueWith(async task => await CalcCrc(task.Result), token);
            var compress = downloadPage.ContinueWith(task => Compress(task.Result)).Unwrap();
        
            var fielname = Path.Join("files", new Uri(url).Host.Replace('.','_')+".gzip");
            await compress.ContinueWith(task => UpdateFile(task.Result, fielname));
        };
    }
    private void UpdateFile(byte[] downloadPage, string filePath)
    {
        if (File.Exists(filePath))
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            if (!fileData.SequenceEqual(downloadPage))
            {
                File.WriteAllBytes(filePath, downloadPage);
                _logger.LogInformation("Change detected, updating file: {1}", filePath );
            }
            else
            {
                _logger.LogInformation("Changes not detected: {1}", filePath );
            }
        }
        else
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? string.Empty);
            File.WriteAllBytes(filePath, downloadPage);
            _logger.LogInformation("First saving: {1}", filePath );
        }
    }

    private async Task<byte[]> Compress(byte[] dataToCompress)
    {
        using (MemoryStream output = new MemoryStream())
        {
            using (GZipStream gzip = new GZipStream(output, CompressionMode.Compress))
            {
                await gzip.WriteAsync(dataToCompress, 0, dataToCompress.Length);
            }
            
            byte[] compressedData = output.ToArray();
            _logger.LogInformation("Bytes after gzip: {0}",compressedData.Length.ToString());
            return compressedData;
        }
    }


    private async Task CalcCrc(byte[] html)
    {
        var crc = new Crc32();
        var hash = crc.GetHashAndReset(html);
        var hashString = hash.ToString();
        _logger.LogInformation(hashString);
    }

    private async Task<byte[]> DownloadPage(string url)
    {
        byte[] html = null;
        try
        {
            using (var client = new WebClient())
            {
                client.Headers["user-agent"] = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)";
                
                html = await client.DownloadDataTaskAsync(url);
                
                _logger.LogInformation(html.Length.ToString());
            }
        }
        catch (OperationCanceledException)
        {
            // Prevent throwing if the Delay is cancelled
        }

        return html;
    }
}