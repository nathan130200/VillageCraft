using System.Numerics;
using Raylib_cs;
namespace VillageCraft;

public sealed class TerrainSettings
{
    public int MaxMountainHeight {get;set;}=7;
    public int LakeRadius {get;set;}=3;
    public float TreeDensity {get;set;}=.065f;
    public float GrassDensity {get;set;}=.14f;
    public int StarterHalfWidth {get;set;}=9;
    public int StarterHalfDepth {get;set;}=10;
    public string TreePrefab {get;set;}="oak_tree";
}
public sealed class TerrainData
{
    public const float BlockHeight=.25f;
    [System.Text.Json.Serialization.JsonIgnore] public int MaximumHeight {get;private set;}
    public int Seed {get;set;}
    public int[] Elevation {get;set;}=[];
    public byte[] Water {get;set;}=[];
    public byte[] Grass {get;set;}=[];
    public static TerrainData Flat(int width,int height)=>new(){Elevation=new int[width*height],Water=new byte[width*height],Grass=new byte[width*height]};
    public static TerrainData Generate(int width,int height,int seed,TerrainSettings settings)
    {
        var result=Flat(width,height);result.Seed=seed;var rng=new Random(seed);
        int ax=width/2,az=height/2;
        int Distance(int x,int z)=>Math.Max(0,Math.Abs(x-ax)-settings.StarterHalfWidth)+Math.Max(0,Math.Abs(z-az)-settings.StarterHalfDepth);
        var peaks=new List<(int X,int Z,int Height)>();
        int count=Math.Max(3,width*height/1600);
        for(int i=0;i<count;i++)
        {
            int x=i==0?width-3:rng.Next(width),z=i==0?height-3:rng.Next(height);
            peaks.Add((x,z,rng.Next(4,settings.MaxMountainHeight+1)));
        }
        var lakes=new List<(int X,int Z,int R)>{(3,height-4,settings.LakeRadius)};
        for(int i=0;i<Math.Max(1,width*height/4500);i++)lakes.Add((rng.Next(Math.Max(1,width/3)),rng.Next(height),rng.Next(2,settings.LakeRadius+1)));
        for(int z=0;z<height;z++)for(int x=0;x<width;x++)
        {
            int n=z*width+x,distance=Distance(x,z),level=0;
            foreach(var peak in peaks)level=Math.Max(level,peak.Height-Math.Abs(x-peak.X)-Math.Abs(z-peak.Z));
            level=Math.Min(level,distance);
            foreach(var lake in lakes)level=Math.Min(level,Math.Max(0,Math.Abs(x-lake.X)+Math.Abs(z-lake.Z)-lake.R*2));
            result.Elevation[n]=level;
            bool starterGrove=Math.Abs(x-(ax-11))<=2&&Math.Abs(z-az)<=3;
            if(distance>1&&!starterGrove&&level==0&&lakes.Any(l=>(x-l.X)*(x-l.X)+(z-l.Z)*(z-l.Z)<=l.R*l.R))
            {result.Water[n]=1;result.Elevation[n]=-1;}
            else if(distance>0&&level<3&&rng.NextDouble()<settings.GrassDensity)result.Grass[n]=(byte)rng.Next(1,4);
        }
        result.Validate(width,height);return result;
    }
    public void Validate(int width,int height)
    {
        if(Elevation.Length!=width*height||Water.Length!=Elevation.Length||Grass.Length!=Elevation.Length)throw new InvalidDataException("Invalid terrain dimensions");
        MaximumHeight=Elevation.Max();
        for(int z=0;z<height;z++)for(int x=0;x<width;x++)
        {
            int n=z*width+x;
            if(Elevation[n] is < -1 or >32||Water[n]>1||Grass[n]>3||Water[n]==1&&Elevation[n]!=-1)throw new InvalidDataException("Invalid terrain tile");
            if(x>0&&Math.Abs(Elevation[n]-Elevation[n-1])>1||z>0&&Math.Abs(Elevation[n]-Elevation[n-width])>1)throw new InvalidDataException("Terrain step exceeds one block");
        }
    }
}
