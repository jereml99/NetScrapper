using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using DefaultNamespace;
using Newtonsoft.Json;
using JsonConverter = Newtonsoft.Json.JsonConverter;

namespace WpfApp1;

public class MainModelView :  INotifyPropertyChanged, IDisposable
{
    private NamedPipeClientStream _pipe;
    private int totalPages;
    private string totalTransfer = "0 B";
    private const string pipeName = "scrapperComm";
    
    public int TotalPages
    {
        get => totalPages;
        set
        {
            totalPages = value;
            OnPropertyChanged("TotalPages");
        }
    }

    public string TotalTransfer
    {
        get => totalTransfer;
        set
        {
            totalTransfer = value;
            OnPropertyChanged("TotalTransfer");
        }
    }
    public MainModelView()
    {
        forceDownloadCommand = new RelayCommand(ForceDownload, CanForceDownload);

        Task.Run( () =>
        {
            _pipe = new NamedPipeClientStream(
                ".",
                pipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous);
            
            _pipe.Connect();

            var streamString = new StreamString(_pipe);
            
            while (_pipe.IsConnected)
            {
                try
                {
                    var message = streamString.ReadString();
                    Console.WriteLine("Messages received: {0}", message);
                    var serviceStatus = JsonConvert.DeserializeObject<ServiceStatus>(message);
                    if (serviceStatus != null)
                    {
                        TotalPages = serviceStatus.totalPages;
                        TotalTransfer = serviceStatus.totalTransfer.ToHumanBytes(); 
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

            }
        });
    }



    private bool CanForceDownload(object obj) => _pipe.IsConnected && Uri.IsWellFormedUriString(obj as string, UriKind.Absolute);

    private void ForceDownload(object obj)
    {
        var streamString = new StreamString(_pipe);
        streamString.WriteString(obj as string);
    }

    public ICommand forceDownloadCommand { get; set; }



    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public void Dispose()
    {
        _pipe.Dispose();
    }
}