using System.Numerics;
using ImGuiNET;
using Raylib_cs;
namespace VillageCraft;

partial class GameUi
{
    readonly Dictionary<string,(SaveSummary? Summary,string Error)> saveDetails=new(StringComparer.OrdinalIgnoreCase);
    Texture2D savePreview;
    string previewName=string.Empty;
    void RefreshSaves()
    {
        saves=SaveStore.List();saveDetails.Clear();ClearSavePreview();
        foreach(string name in saves)
        {
            try{saveDetails[name]=(BinarySave.Summary(SaveStore.LoadPathFor(name)),string.Empty);}
            catch(Exception e)when(e is IOException or UnauthorizedAccessException or InvalidDataException)
            {saveDetails[name]=(null,e.Message);}
        }
    }
    void ClearSavePreview()
    {
        if(savePreview.Id!=0)Raylib.UnloadTexture(savePreview);
        savePreview=default;previewName=string.Empty;
    }
    static void SaveStats(SaveSummary summary)
    {
        var time=TimeSpan.FromSeconds(summary.PlaySeconds);
        ImGui.TextUnformatted(L.F("save.details",Math.Max(0,summary.Day-1),$"{(long)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}",summary.Emeralds));
    }
    static string SaveKey(string name)
    {
        try{return Path.GetFileNameWithoutExtension(SaveStore.PathFor(name));}
        catch(ArgumentException){return string.Empty;}
    }
    void SaveList(bool saving)
    {
        ImGui.BeginChild("Save list",new(0,150),ImGuiChildFlags.Borders);
        foreach(string name in saves)
        {
            if(ImGui.Selectable(name,string.Equals(saving?SaveKey(saveName):loadName,name,StringComparison.OrdinalIgnoreCase)))
            {if(saving){saveName=name;overwrite=false;}else loadName=name;}
            if(saveDetails[name].Summary is {} summary)SaveStats(summary);
            else ImGui.TextUnformatted(L.T("save.unavailable"));
            ImGui.Separator();
        }
        if(saves.Length==0)Wrapped(L.T("ui.no_saved_villages_yet"));
        ImGui.EndChild();
    }
    void SelectedSaveDetails(string name)
    {
        name=SaveKey(name);
        if(!saveDetails.TryGetValue(name,out var details))return;
        if(details.Summary is not {} summary){Wrapped(L.T("save.unavailable"));Wrapped(details.Error);return;}
        SaveStats(summary);
        if(previewName!=name)
        {
            ClearSavePreview();previewName=name;
            if(summary.Preview.Length>0)
            {
                var image=Raylib.LoadImageFromMemory(".png",summary.Preview);
                try{if(Raylib.IsImageValid(image))savePreview=Raylib.LoadTextureFromImage(image);}
                finally{Raylib.UnloadImage(image);}
            }
        }
        if(savePreview.Id!=0)
        {
            float width=Math.Min(SavePreview.Width,ImGui.GetContentRegionAvail().X);
            ImGui.Image((IntPtr)savePreview.Id,new Vector2(width,width*SavePreview.Height/SavePreview.Width));
        }
        else Wrapped(L.T("save.no_preview"));
    }
#if DEBUG
    string? previewSmokeName;
    internal void SavePreviewSmoke(City city)
    {
        previewSmokeName="preview-validation-"+Guid.NewGuid().ToString("N");
        city.PlaySeconds=3723;city.SavePreviewPng=SavePreview.Capture();
        if(city.SavePreviewPng.Length==0)throw new InvalidOperationException("No rendered save preview captured.");
        city.Save(SaveStore.PathFor(previewSmokeName));
        var summary=BinarySave.Summary(SaveStore.PathFor(previewSmokeName));
        if(!summary.Preview.SequenceEqual(city.SavePreviewPng))throw new InvalidOperationException("Preview did not survive binary save.");
        RefreshSaves();loadName=previewSmokeName;MenuPage="Load Game";MenuOpen=true;
    }
    internal void SavePreviewSmokeOverwrite(){MenuPage="Save Game As";saveName=previewSmokeName!;overwrite=true;}
#endif
}
