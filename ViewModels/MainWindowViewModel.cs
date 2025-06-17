using ReactiveUI;

namespace TextToImageCrypto.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private ViewModelBase _currentViewModel;
    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        set => this.RaiseAndSetIfChanged(ref _currentViewModel, value);
    }

    public MainWindowViewModel()
    {
        CurrentViewModel = new EncodeViewModel(this);
    }

    public void ShowEncodeView() => CurrentViewModel = new EncodeViewModel(this);
    public void ShowDecodeView() => CurrentViewModel = new DecodeViewModel(this);
}