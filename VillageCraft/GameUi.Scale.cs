using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;
namespace VillageCraft;

partial class GameUi
{
    float frameScale=1;
    public void BeginFrame()
    {
        frameScale=GraphicsOptions.Current.UiScale;
        // Keep layout and input in logical pixels; scale the finished draw lists together.
        Raylib.SetMouseScale(1/frameScale,1/frameScale);
        rlImGui.Begin();
        Raylib.SetMouseScale(1,1);
        var io=ImGui.GetIO();io.DisplaySize/=frameScale;
        ImGui.GetMainViewport().Size=io.DisplaySize;
        ImGui.GetMainViewport().WorkSize=io.DisplaySize;
    }
    public void EndFrame()
    {
        ImGui.Render();
        var data=ImGui.GetDrawData();
        if(frameScale!=1)
        {
            for(int n=0;n<data.CmdListsCount;n++)
            {
                var list=data.CmdLists[n];
                for(int i=0;i<list.VtxBuffer.Size;i++)list.VtxBuffer[i].pos*=frameScale;
                for(int i=0;i<list.CmdBuffer.Size;i++)list.CmdBuffer[i].ClipRect*=frameScale;
            }
            data.DisplaySize*=frameScale;
        }
        ImGui.GetIO().DisplaySize=new(Raylib.GetScreenWidth(),Raylib.GetScreenHeight());
        rlImGui.End();
    }
}
