using System;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ReactiveUI;
using TextToImageCrypto.Services;
using TextToImageCrypto.Services.Cryptography;
using TextToImageCrypto.Utils;
using TextToImageCrypto.Views;

namespace TextToImageCrypto.ViewModels;

public class EncodeViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainViewModel;
    private string _text = "";
    private string _status = "";
    private Bitmap? _encodedImage;
    public Bitmap? EncodedImage
    {
        get => _encodedImage;
        set => this.RaiseAndSetIfChanged(ref _encodedImage, value);
    }
    
    private string _imagePath = "";
    public string ImagePath
    {
        get => _imagePath;
        set => this.RaiseAndSetIfChanged(ref _imagePath, value);
    }
    
    public string Text
    {
        get => _text;
        set => this.RaiseAndSetIfChanged(ref _text, value);
    }
    
    public string Status
    {
        get => _status;
        set => this.RaiseAndSetIfChanged(ref _status, value);
    }
    
    public ReactiveCommand<Unit, Unit> EncodeCommand { get; }
    public ReactiveCommand<Unit, Unit> GoToDecodeCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenInExplorerCommand { get; }
    
    public EncodeViewModel(MainWindowViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
        
        EncodeCommand = ReactiveCommand.CreateFromTask(Encode);
        GoToDecodeCommand = ReactiveCommand.Create(() => _mainViewModel.ShowDecodeView());
        OpenInExplorerCommand = ReactiveCommand.Create(OpenInExplorer);
    }
    
    private void OpenInExplorer()
    {
        if (!string.IsNullOrEmpty(ImagePath) && File.Exists(ImagePath))
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = $"\"{Path.GetDirectoryName(ImagePath)}\"",
                    UseShellExecute = false
                };
                System.Diagnostics.Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Status = $"Ошибка открытия проводника: {ex.Message}";
            }
        }
    }

    private async Task Encode()
    {
        EncodedImage = null;
        ImagePath = "";
        if (string.IsNullOrWhiteSpace(Text))
        {
            Status = "Введите текст для кодирования";
            return;
        }

        var window = WindowUtils.GetMainWindow();
        if (window == null) 
        {
            Status = "Не удалось получить окно приложения";
            return;
        }

        try
        {
            var passwordDialog = new PasswordDialogWindow("Введите пароль шифрования:");
            var password = await passwordDialog.ShowDialog<string>(window);
            if (string.IsNullOrEmpty(password))
            {
                Status = "Пароль не введён";
                return;
            }
            
            byte[] salt = KeyGenerator.GenerateRandomSalt();
            byte[] key = KeyGenerator.DeriveKeyFromPassword(password, salt);
            
            using var aesService = new AesGcmService(key);
            var (ciphertext, nonce, tag) = aesService.Encrypt(Text);
        
            // payload:
            // Salt 16 байт
            // Nonce 12 байт
            // Tag 16 байт
            // Ciphertext
            byte[] payload = new byte[salt.Length + nonce.Length + tag.Length + ciphertext.Length];
            int offset = 0;
        
            Buffer.BlockCopy(salt, 0, payload, offset, salt.Length);
            offset += salt.Length;
        
            Buffer.BlockCopy(nonce, 0, payload, offset, nonce.Length);
            offset += nonce.Length;
        
            Buffer.BlockCopy(tag, 0, payload, offset, tag.Length);
            offset += tag.Length;
        
            Buffer.BlockCopy(ciphertext, 0, payload, offset, ciphertext.Length);
            
            var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Сохранить закодированное изображение",
                SuggestedFileName = "secret_image.png",
                DefaultExtension = "bmp",
                FileTypeChoices = [new FilePickerFileType("PNG Image") { Patterns = ["*.png"] }]
            });
        
            if (file != null)
            {
                var outputPath = file.Path.AbsolutePath;
                var testFile = Path.Combine(Path.GetTempPath(), "test_payload.bin");
                File.WriteAllBytes(testFile, payload);
                var stegoService = new SteganographyService();
                stegoService.EncodeToImage(payload, outputPath);
                Status = $"Изображение сохранено: {outputPath}";
                var stegoTest = new SteganographyService();
                ImagePath = outputPath;
                
                await LoadImagePreview(outputPath);

            }
        }
        catch (Exception ex)
        {
            Status = $"Ошибка: {ex.Message}";
        }
    }
    private async Task LoadImagePreview(string imagePath)
    {
        try
        {
            await using var stream = File.OpenRead(imagePath);
            EncodedImage = new Bitmap(stream);
            if (EncodedImage.PixelSize.Width < 10 || EncodedImage.PixelSize.Height < 10)
            {
                var scaled = new RenderTargetBitmap(
                    new PixelSize(300, 300), 
                    new Vector(96, 96));
        
                using var context = scaled.CreateDrawingContext();
                context.DrawImage(EncodedImage, new Rect(0, 0, 300, 300));
        
                EncodedImage = scaled;
            }
        }
        catch (Exception ex)
        {
            Status = $"Ошибка загрузки изображения: {ex.Message}";
        }
    }
}