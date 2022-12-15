using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using DefaultNamespace;

namespace WpfApp1;

public class MainModelView :  INotifyPropertyChanged
{
    private NamedPipeClientStream _pipe;
    private const string pipeName = "scrapperComm";
    public MainModelView()
    {
        forceDownloadCommand = new RelayCommand(ForceDownload, CanForceDownload);

        Task.Run( async () =>
        {
            _pipe = new NamedPipeClientStream(
                ".",
                pipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous);
            
            await _pipe.ConnectAsync();

            var streamString = new StreamString(_pipe);
            
            while (true)
            {
                var message = streamString.ReadString();
                Console.WriteLine("Messages received: {}", message);
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
}