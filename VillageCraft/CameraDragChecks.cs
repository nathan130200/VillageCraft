using System.Numerics;
namespace VillageCraft;
static class CameraDragChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception("CAMERA DRAG: "+message);}
    public static void Run()
    {
        var drag=new CameraDrag();
        drag.Update(Vector2.Zero,true,false,false);drag.Update(new(2,0),false,false,false);
        Check(drag.LeftClick&&drag.Pan==Vector2.Zero,"small motion remains a normal click");
        drag.Update(Vector2.Zero,true,false,false);drag.Update(new(10,5),true,false,false);
        Check(drag.Pan==new Vector2(10,5)&&!drag.LeftClick,"held LMB pans");
        drag.Update(new(10,5),false,false,false);Check(!drag.LeftClick,"ending drag cannot inspect or place");
        drag.Update(Vector2.Zero,false,true,false);drag.Update(new(15,0),false,true,false);
        Check(drag.Rotation==15&&drag.Pan==Vector2.Zero,"held RMB rotates");
        drag.Update(new(15,0),false,false,false);Check(!drag.RightClick,"ending rotation cannot inspect");
        drag.Update(Vector2.Zero,true,false,true);drag.Update(new(20,0),true,false,false);drag.Update(new(20,0),false,false,false);
        Check(!drag.LeftClick&&drag.Pan==Vector2.Zero,"a UI press cannot become a world drag");
        drag.Update(Vector2.Zero,true,false,false);drag.Update(new(30,0),true,false,true);drag.Update(new(30,0),false,false,false);
        Check(!drag.LeftClick,"opening a menu cancels a pending world gesture");
        Console.WriteLine("PASS: drag versus click, LMB pan, RMB rotation, release suppression and UI cancellation.");
    }
}
