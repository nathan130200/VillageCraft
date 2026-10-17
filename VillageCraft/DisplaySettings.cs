using Raylib_cs;
namespace VillageCraft;

static class DisplaySettings
{
    static bool pending;
    public static void RequestApply()=>pending=true;
    public static (int Width,int Height) FitWindow(int width,int height,int monitorWidth,int monitorHeight)
    {
        // Leave space for title bars, taskbars and desktop panels on all supported platforms.
        int maxWidth=Math.Max(320,monitorWidth-64),maxHeight=Math.Max(240,monitorHeight-128);
        return (Math.Clamp(width,Math.Min(640,maxWidth),maxWidth),Math.Clamp(height,Math.Min(480,maxHeight),maxHeight));
    }
    public static IEnumerable<(int Width,int Height)> Resolutions()
    {
        int monitor=Raylib.GetCurrentMonitor(),w=Raylib.GetMonitorWidth(monitor),h=Raylib.GetMonitorHeight(monitor);
        return new[]{(1024,640),(1280,720),(1280,800),(1366,768),(1440,900),(1600,900),(1920,1080),(2560,1440),(3840,2160),(w,h),(GraphicsOptions.Current.DisplayWidth,GraphicsOptions.Current.DisplayHeight)}
            .Where(r=>r.Item1<=w&&r.Item2<=h).Distinct().OrderBy(r=>r.Item1).ThenBy(r=>r.Item2);
    }
    public static void ApplyPending()
    {
        if(!pending)return;pending=false;Apply(true);
    }
    public static void Apply(bool save)
    {
        var g=GraphicsOptions.Current;g.Validate();
        int monitor=Raylib.GetCurrentMonitor();
        if(Raylib.IsWindowFullscreen())Raylib.ToggleFullscreen();
        if(Raylib.IsWindowState(ConfigFlags.BorderlessWindowMode))Raylib.ToggleBorderlessWindowed();
        if(Raylib.IsWindowMaximized())Raylib.RestoreWindow();
        int mw=Raylib.GetMonitorWidth(monitor),mh=Raylib.GetMonitorHeight(monitor);
        var origin=Raylib.GetMonitorPosition(monitor);
        Raylib.SetWindowMinSize(320,240);
        if(g.DisplayMode==0)
        {
            var fit=FitWindow(g.DisplayWidth,g.DisplayHeight,mw,mh);
            Raylib.SetWindowSize(fit.Width,fit.Height);
            Raylib.SetWindowPosition((int)origin.X+(mw-fit.Width)/2,(int)origin.Y+(mh-fit.Height)/2);
            Raylib.SetWindowMinSize(Math.Min(1024,fit.Width),Math.Min(640,fit.Height));
            g.DisplayWidth=fit.Width;g.DisplayHeight=fit.Height;
        }
        else
        {
            Raylib.SetWindowSize(Math.Min(g.DisplayWidth,mw),Math.Min(g.DisplayHeight,mh));
            Raylib.SetWindowPosition((int)origin.X+32,(int)origin.Y+48);
            if(g.DisplayMode==1)Raylib.ToggleFullscreen();
            else Raylib.ToggleBorderlessWindowed();
        }
        Raylib.SetTargetFPS(g.Fps);
        if(save)g.Save();
    }
}
