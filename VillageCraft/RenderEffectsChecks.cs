using Raylib_cs;
namespace VillageCraft;
static class RenderEffectsChecks
{
    static readonly Dictionary<int,System.Numerics.Vector2> previous=[];
    static readonly Dictionary<int,System.Numerics.Vector2> logical=[];
    public static void TrackCrowd(City city,int frame,float dt)
    {
        if(frame==0){previous.Clear();logical.Clear();}
        foreach(var v in city.Citizens)
        {
            var position=Scene.VillagerPosition(city,v);
            float movement=logical.TryGetValue(v.Id,out var origin)?System.Numerics.Vector2.Distance(origin,v.Position):0;
            float limit=Math.Max(.12f,movement+dt*2.6f);
            if(frame>5&&previous.TryGetValue(v.Id,out var p)&&System.Numerics.Vector2.Distance(position,p)>limit)
                throw new InvalidOperationException($"Rendered crowd jumped at frame {frame}, villager {v.Id}: {System.Numerics.Vector2.Distance(position,p)} / {limit}.");
            previous[v.Id]=position;logical[v.Id]=v.Position;
        }
    }
    public static unsafe Color[] ReadFrame()
    {
        var image=Raylib.LoadImageFromScreen();var pixels=Raylib.LoadImageColors(image);
        try{var result=new Color[image.Width*image.Height];for(int i=0;i<result.Length;i++)result[i]=pixels[i];return result;}
        finally{Raylib.UnloadImageColors(pixels);Raylib.UnloadImage(image);}
    }
    public static void Shadows(Color[] clear,Color[] shadowed)
    {
        int changed=0;
        for(int i=0;i<Math.Min(clear.Length,shadowed.Length);i++)
        {
            var a=clear[i];var b=shadowed[i];
            if(a.G>a.R&&a.G>a.B&&a.G-b.G>8&&a.R-b.R>5)changed++;
        }
        if(changed<100)throw new InvalidOperationException("Sun shadows did not darken the rendered ground: "+changed);
        Console.WriteLine("PASS: soft sun shadows darken "+changed+" ground pixels.");
    }
}
