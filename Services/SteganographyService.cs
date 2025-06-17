using System;
using System.IO;
using SkiaSharp;

namespace TextToImageCrypto.Services;
// намеренное "упрятывание информации" от посторонних в картинку
public class SteganographyService
{
    public void EncodeToImage(byte[] data, string outputPath)
    {
        byte[] signature = { 0x53, 0x54, 0x45, 0x47 }; // "STEG"
        byte[] sizeBytes = BitConverter.GetBytes(data.Length);
        
        byte[] payload = new byte[signature.Length + sizeBytes.Length + data.Length];
        Buffer.BlockCopy(signature, 0, payload, 0, signature.Length);
        Buffer.BlockCopy(sizeBytes, 0, payload, signature.Length, sizeBytes.Length);
        Buffer.BlockCopy(data, 0, payload, signature.Length + sizeBytes.Length, data.Length);
        
        int totalBits = payload.Length * 8;
        int pixelsNeeded = (int)Math.Ceiling(totalBits / 3.0);
        int size = (int)Math.Ceiling(Math.Sqrt(pixelsNeeded)) + 1; // +1 для запаса
        
        using var surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Rgba8888));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        int bitIndex = 0;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (bitIndex >= totalBits) break;

                SKColor color = SKColors.White;
                byte r = color.Red;
                byte g = color.Green;
                byte b = color.Blue;
                
                for (int i = 0; i < 3; i++)
                {
                    if (bitIndex >= totalBits) break;

                    int byteIndex = bitIndex / 8;
                    int bitOffset = 7 - (bitIndex % 8);
                    int bitValue = (payload[byteIndex] >> bitOffset) & 1;

                    if (i == 0) r = (byte)((r & 0xFE) | bitValue);
                    else if (i == 1) g = (byte)((g & 0xFE) | bitValue);
                    else if (i == 2) b = (byte)((b & 0xFE) | bitValue);

                    bitIndex++;
                }

                canvas.DrawPoint(x, y, new SKColor(r, g, b));
            }
        }
        var gridPaint = new SKPaint { Color = SKColors.LightGray, StrokeWidth = 0.5f };
        for (int x = 0; x <= size; x++)
        {
            canvas.DrawLine(x, 0, x, size, gridPaint);
        }
        for (int y = 0; y <= size; y++)
        {
            canvas.DrawLine(0, y, size, y, gridPaint);
        }
        using var image = surface.Snapshot();
        using var dataEnc = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(outputPath);
        dataEnc.SaveTo(stream);
    }

    public byte[] DecodeFromImage(string inputPath)
    {
        using var stream = File.OpenRead(inputPath);
        using var bitmap = SKBitmap.Decode(stream);
        if (bitmap == null) throw new InvalidOperationException("Invalid image format");
        
        int totalBits = bitmap.Width * bitmap.Height * 3;
        byte[] bits = new byte[totalBits];
        
        int bitIndex = 0;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitIndex >= totalBits) break;
                
                var color = bitmap.GetPixel(x, y);
                bits[bitIndex++] = (byte)(color.Red & 1);
                if (bitIndex >= totalBits) break;
                
                bits[bitIndex++] = (byte)(color.Green & 1);
                if (bitIndex >= totalBits) break;
                
                bits[bitIndex++] = (byte)(color.Blue & 1);
                if (bitIndex >= totalBits) break;
            }
        }

        // конвертация :3
        int totalBytes = totalBits / 8;
        byte[] data = new byte[totalBytes];
        for (int i = 0; i < totalBytes; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                data[i] = (byte)((data[i] << 1) | bits[i * 8 + j]);
            }
        }

       
        byte[] expectedSignature = { 0x53, 0x54, 0x45, 0x47 }; 
        for (int i = 0; i < expectedSignature.Length; i++)
        {
            if (data[i] != expectedSignature[i])
                throw new InvalidOperationException("Invalid data signature");
        }

        // размер данных
        int dataSize = BitConverter.ToInt32(data, 4);
        if (dataSize <= 0 || dataSize > data.Length - 8)
            throw new InvalidOperationException("Invalid data size");

        // полезные данные
        byte[] payload = new byte[dataSize];
        Buffer.BlockCopy(data, 8, payload, 0, dataSize);
        return payload;
    }
}