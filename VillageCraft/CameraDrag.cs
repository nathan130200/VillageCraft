using System.Numerics;
namespace VillageCraft;

sealed class CameraDrag
{
    enum Button { None,Left,Right }
    Button held;
    Vector2 start,last;
    bool dragging,wasLeft,wasRight;
    public Vector2 Pan {get;private set;}
    public float Rotation {get;private set;}
    public bool LeftClick {get;private set;}
    public bool RightClick {get;private set;}
    public void Update(Vector2 mouse,bool left,bool right,bool blocked)
    {
        Pan=Vector2.Zero;Rotation=0;LeftClick=RightClick=false;
        if(blocked){held=Button.None;dragging=false;}
        else
        {
            if(held==Button.None)
            {
                if(left&&!wasLeft)held=Button.Left;
                else if(right&&!wasRight)held=Button.Right;
                if(held!=Button.None){start=last=mouse;dragging=false;}
            }
            if(held!=Button.None)
            {
                dragging|=Vector2.DistanceSquared(start,mouse)>=16;
                bool down=held==Button.Left?left:right;
                if(down&&dragging){if(held==Button.Left)Pan=mouse-last;else Rotation=mouse.X-last.X;}
                if(!down){LeftClick=held==Button.Left&&!dragging;RightClick=held==Button.Right&&!dragging;held=Button.None;}
                last=mouse;
            }
        }
        wasLeft=left;wasRight=right;
    }
}
