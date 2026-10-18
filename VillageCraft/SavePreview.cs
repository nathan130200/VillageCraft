using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Raylib_cs;
namespace VillageCraft;

public sealed record SaveSummary(int Day,double PlaySeconds,float Emeralds,byte[] Preview);

static class SavePreview
{
    public const int Width=320,Height=180,MaxBytes=256*1024;
    public static void Validate(byte[] png)
    {
        if(png.Length==0)return; // Headless saves have no rendered frame.
        if(png.Length<33||png.Length>MaxBytes||!png.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10})||
            !png.AsSpan(12,4).SequenceEqual("IHDR"u8)||BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16))!=Width||BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20))!=Height)
            throw new InvalidDataException("Invalid save preview image.");
    }
    public static unsafe byte[] Capture()
    {
        // Called after drawing the world, before submitting the ImGui draw lists.
        Rlgl.DrawRenderBatchActive();
        var image=Raylib.LoadImageFromScreen();
        try
        {
            if(image.Data==null)return [];
            Raylib.ImageResize(ref image,Width,Height);
            fixed(byte* extension=".png\0"u8)
            {
                int length=0;byte* data=Raylib.ExportImageToMemory(image,(sbyte*)extension,&length);
                try
                {
                    if(data==null||length<=0||length>MaxBytes)return [];
                    var result=new byte[length];Marshal.Copy((IntPtr)data,result,0,length);return result;
                }
                finally{if(data!=null)Raylib.MemFree(data);}
            }
        }
        finally{Raylib.UnloadImage(image);}
    }
}
