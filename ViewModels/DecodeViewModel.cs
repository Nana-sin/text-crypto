using System;
using System.IO;
using System.Reactive;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ReactiveUI;
using TextToImageCrypto.Services;
using TextToImageCrypto.Services.Cryptography;
using TextToImageCrypto.Utils;
using TextToImageCrypto.Views;

namespace TextToImageCrypto.ViewModels;

public class DecodeViewModel :ViewModelBase
{
    private readonly MainWindowViewModel _mainViewModel;
    private string _decodedText = "";
    private string _status = "";
    private double _progress;
    
    public string DecodedText
    {
        get => _decodedText;
        set => this.RaiseAndSetIfChanged(ref _decodedText, value);
    }
    
    public string Status
    {
        get => _status;
        set => this.RaiseAndSetIfChanged(ref _status, value);
    }
    
    public double Progress
    {
        get => _progress;
        set => this.RaiseAndSetIfChanged(ref _progress, value);
    }
    
    public ReactiveCommand<Unit, Unit> DecodeCommand { get; }
    public ReactiveCommand<Unit, Unit> GoToEncodeCommand { get; }

    public DecodeViewModel(MainWindowViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
        
        DecodeCommand = ReactiveCommand.CreateFromTask(Decode);
        GoToEncodeCommand = ReactiveCommand.Create(() => _mainViewModel.ShowEncodeView());
    }

    private async Task Decode()
    {
        var window = WindowUtils.GetMainWindow();
         if (window == null)
         {
             Status = "Failed to retrieve the application window";
             return;
         }
         
         DecodedText = "";
         Status = "";
         Progress = 0;

         try
         {
             var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
             {
                 Title = "Select Encode Image",
                 AllowMultiple = false,
                 FileTypeFilter = [new FilePickerFileType("PNG Images") { Patterns = ["*.png"] }]
             });
        
             if (files.Count == 0) return;
             var file = files[0];
             Progress = 10;

             
             var passwordDialog = new PasswordDialogWindow("Введите пароль для расшифровки:");
             var password = await passwordDialog.ShowDialog<string>(window);
             if (string.IsNullOrEmpty(password))
             {
                 Status = "Password not entered";
                 return;
             }
             Progress = 20;

             
             var stegoService = new SteganographyService();
             byte[] payload = stegoService.DecodeFromImage(file.Path.AbsolutePath);
             Progress = 40;
             
             if (payload.Length < 16 + 12 + 16)
             {
                 Status = "Insufficient data in the image";
                 return;
             }
             
             byte[] salt = new byte[16];
             byte[] nonce = new byte[12];
             byte[] tag = new byte[16];
        
             Buffer.BlockCopy(payload, 0, salt, 0, salt.Length);
             Buffer.BlockCopy(payload, salt.Length, nonce, 0, nonce.Length);
             Buffer.BlockCopy(payload, salt.Length + nonce.Length, tag, 0, tag.Length);
        
             byte[] ciphertext = new byte[payload.Length - salt.Length - nonce.Length - tag.Length];
             Buffer.BlockCopy(payload, salt.Length + nonce.Length + tag.Length, 
                 ciphertext, 0, ciphertext.Length);

            
             byte[] key = KeyGenerator.DeriveKeyFromPassword(password, salt);
             Progress = 80;
             
             Console.WriteLine($"Salt: {Convert.ToBase64String(salt)}");
             Console.WriteLine($"Nonce: {Convert.ToBase64String(nonce)}");
             Console.WriteLine($"Tag: {Convert.ToBase64String(tag)}");
             Console.WriteLine($"Ciphertext length: {ciphertext.Length}");
             
             File.WriteAllBytes("debug_payload.bin", payload);

             
             using var aesService = new AesGcmService(key);
             DecodedText = aesService.Decrypt(ciphertext, nonce, tag);
             Progress = 100;
             Status = "Text successfully decoded!";
         }
         catch (CryptographicException ex)
         {
             Status = $"Encryption error: {ex.Message}";
             DecodedText = "";
         }
         catch (Exception ex)
         {
             Status = $"Error: {ex.Message}";
             DecodedText = "";
         }
    }
}