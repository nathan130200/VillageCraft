using System.Numerics;
namespace VillageCraft;

static class EntityOffsets
{
    // Stable sub-tile lanes. No neighbor queries, collision response, velocity or moving slots.
    public static Vector2 Offset(int id)
    {
        double x=id*.7548776662466927,z=id*.5698402909980532;
        return new((float)(x-Math.Floor(x)-.5)*.6f,(float)(z-Math.Floor(z)-.5)*.6f);
    }
    public static Vector2 Position(City city,Vector2 logical,int id,float strength=1)
    {
        var p=logical+Offset(id)*strength;
        return Vector2.Clamp(p,new(-.32f),new(city.Width-.68f,city.Height-.68f));
    }
}
