using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Raylib_cs;
namespace VillageCraft;

public enum GameLogLevel { Debug, Info, Warning, Error, Critical }

/// <summary>One UTF-8 file per run, including managed console output and native renderer errors.</summary>
public static class GameLog
{
    static readonly object sync=new();
    static StreamWriter? file;
    static readonly TextWriter originalOut=Console.Out,originalError=Console.Error;
    static bool initialized,reportedFailure;
    public static string FilePath {get;private set;}=string.Empty;
    public static void Initialize()
    {
        if(initialized)return;initialized=true;
        try
        {
            string folder=Path.Combine(AppContext.BaseDirectory,"UserData","Logs");Directory.CreateDirectory(folder);
            FilePath=Path.Combine(folder,$"VillageCraft-{DateTime.UtcNow:yyyyMMdd-HHmmss-fffffff}-{Environment.ProcessId}.log");
            file=new StreamWriter(new FileStream(FilePath,FileMode.CreateNew,FileAccess.Write,FileShare.Read),new UTF8Encoding(false)){AutoFlush=true};
            Console.SetOut(new ConsoleLogWriter(originalOut,GameLogLevel.Info));Console.SetError(new ConsoleLogWriter(originalError,GameLogLevel.Error));
        }
        catch(Exception e)when(e is IOException or UnauthorizedAccessException){originalError.WriteLine("File logging unavailable: "+e.Message);FilePath=string.Empty;}
        AppDomain.CurrentDomain.UnhandledException+=(_,e)=>Write(GameLogLevel.Critical,"Unhandled exception: "+e.ExceptionObject);
        TaskScheduler.UnobservedTaskException+=(_,e)=>Write(GameLogLevel.Critical,"Unobserved task exception: "+e.Exception);
        AppDomain.CurrentDomain.ProcessExit+=(_,_)=>Shutdown();
        Write(GameLogLevel.Info,"VillageCraft "+typeof(GameLog).Assembly.GetName().Version+" started. Log timestamps are UTC.");
    }
    public static void Write(GameLogLevel level,string message)
    {
        var console=level>=GameLogLevel.Error?originalError:originalOut;
        Capture(level,message);
        try{console.WriteLine($"[{level}] {message}");}catch(Exception e)when(e is IOException or ObjectDisposedException){}
    }
    static void Capture(GameLogLevel level,string message)
    {
        lock(sync)
        {
            if(file==null)return;
            try{file.WriteLine($"{DateTime.UtcNow:O} [{level}] {message}");}
            catch(Exception e)when(e is IOException or ObjectDisposedException)
            {if(!reportedFailure){reportedFailure=true;originalError.WriteLine("File logging failed: "+e.Message);}}
        }
    }
    public static void Script(string component,GameLogLevel level,string message)=>Write(level,"Script ["+(string.IsNullOrEmpty(component)?"unknown":component)+"] "+message);
    public static unsafe void CaptureRenderer()=>Raylib.SetTraceLogCallback(&RendererMessage);
    [UnmanagedCallersOnly(CallConvs=[typeof(CallConvCdecl)])]
    static unsafe void RendererMessage(int level,sbyte* text,sbyte* args)
    {
        // Native callbacks must never throw across the interop boundary.
        try
        {
            var severity=level>=(int)TraceLogLevel.Fatal?GameLogLevel.Critical:level>=(int)TraceLogLevel.Error?GameLogLevel.Error:level>=(int)TraceLogLevel.Warning?GameLogLevel.Warning:GameLogLevel.Info;
            Write(severity,"Renderer: "+Logging.GetLogMessage((IntPtr)text,(IntPtr)args));
        }
        catch(Exception e){try{originalError.WriteLine("Renderer logging failed: "+e.Message);}catch{}}
    }
    public static void Shutdown()
    {
        lock(sync){try{file?.Dispose();}catch(IOException e){originalError.WriteLine("Log close failed: "+e.Message);}finally{file=null;}}
    }
    sealed class ConsoleLogWriter(TextWriter original,GameLogLevel level):TextWriter
    {
        public override Encoding Encoding=>Encoding.UTF8;
        public override void WriteLine(string? value){original.WriteLine(value);Capture(level,value??string.Empty);}
        public override void Write(string? value){original.Write(value);if(value!=null)Capture(level,value);}
        public override void Write(char value)=>Write(value.ToString());
        public override void Flush(){original.Flush();lock(sync)file?.Flush();}
    }
}
