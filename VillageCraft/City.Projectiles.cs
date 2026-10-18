using System.Numerics;
using System.Text.Json.Serialization;
namespace VillageCraft;
public enum ProjectileKind { None, Arrow, Fireball, Snowball, Magic }
public sealed class Shot
{
    public Vector2 From {get;}
    public Vector2 To {get;}
    public string Type {get;}
    public Vector3 Position,Velocity;
    public float Life=3,Damage;
    public ProjectileKind Kind;
    public object? Source;
    public bool Hostile;
    public Shot(Vector2 from,Vector2 to,string type){From=from;To=to;Type=type;}
}
public partial class City
{
    internal bool LaunchProjectile(Vector2 from,Vector2 to,string type,UnitPlan unit,float damage,object source,bool hostile)
    {
        if(Shots.Count>=256)return false;
        var start=new Vector3(from.X,SurfaceY(from.X,from.Y)+.45f,from.Y);
        var end=new Vector3(to.X,SurfaceY(to.X,to.Y)+.4f,to.Y);
        var direction=end-start;if(direction.LengthSquared()<.0001f)direction=Vector3.UnitZ;
        direction=Vector3.Normalize(direction);
        Shots.Add(new(from,to,type){Position=start,Velocity=direction*unit.ProjectileSpeed,Kind=unit.Projectile,
            Damage=damage,Source=source,Hostile=hostile,Life=Math.Min(10,unit.Range/unit.ProjectileSpeed+1)});
        Emit(new("onProjectileSpawned",source,Source:Shots[^1]));return true;
    }
    void HitProjectile(Shot shot,object target)
    {
        switch(target)
        {
            case Citizen v:DamageVillager(v,shot.Damage*(1-ArmorReduction),shot.Source);break;
            case Building b:if(shot.Hostile)DamageBuilding(b,shot.Damage,shot.Source);break;
            case Creature m:
                if(Enemies.Contains(m))DamageMonster(m,shot.Damage,shot.Source);
                else {float damage=shot.Damage*(1-ArmorReduction);m.Health-=damage;Emit(new("onGuardianDamaged",m,damage,Source:shot.Source));}
                break;
        }
        Emit(new("onProjectileHit",target,shot.Damage,Source:shot.Source));
    }
    internal void UpdateProjectiles(float dt)
    {
        using var profile=Profiler.Measure(ProfileArea.Projectiles);
        foreach(var shot in Shots.ToArray())
        {
            shot.Life-=dt;if(shot.Life<=0){Shots.Remove(shot);continue;}
            Vector3 from=shot.Position,to=from+shot.Velocity*dt,segment=to-from;
            float length=segment.LengthSquared(),nearest=2;object? hit=null;
            void Test(object entity,Vector3 center,float radius)
            {
                if(ReferenceEquals(entity,shot.Source))return;
                float t=length>0?Math.Clamp(Vector3.Dot(center-from,segment)/length,0,1):0;
                if(t<nearest&&Vector3.DistanceSquared(from+segment*t,center)<=radius*radius){nearest=t;hit=entity;}
            }
            if(shot.Hostile)
            {
                foreach(var v in Citizens)if(v.Health>0)Test(v,new(v.X,SurfaceY(v.X,v.Z)+.4f,v.Z),.24f);
                foreach(var g in Defenders)if(g.Health>0)Test(g,new(g.X,SurfaceY(g.X,g.Z)+.45f,g.Z),.3f);
            }
            else foreach(var m in Enemies)if(m.Health>0)Test(m,new(m.X,SurfaceY(m.X,m.Z)+.4f,m.Z),.25f);
            foreach(var b in Buildings)
                if(!b.Plan.Path&&!b.Plan.Natural&&b.Health>0&&Vector2.DistanceSquared(shot.From,new(b.X,b.Z))>.8f*.8f)
                    Test(b,new(b.X,SurfaceY(b.X,b.Z)+.6f,b.Z),.55f);
            if(hit!=null){shot.Position=from+segment*nearest;HitProjectile(shot,hit);Shots.Remove(shot);continue;}
            shot.Position=to;
            if(to.X<0||to.Z<0||to.X>=Width||to.Z>=Height||to.Y<SurfaceY(to.X,to.Z)+.03f)Shots.Remove(shot);
        }
    }
}
