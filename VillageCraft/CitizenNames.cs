namespace VillageCraft;

public sealed class NamePool
{
    public string[] FirstNames {get;set;}=[];
    public string[] Surnames {get;set;}=[];
}
public static class CitizenNames
{
    static NamePool pool=new();
    public static void Load(string path)
    {
        pool=TomlFile.Read<NamePool>(path);
        if(pool.FirstNames.Length==0||pool.Surnames.Length==0||pool.FirstNames.Concat(pool.Surnames).Any(s=>string.IsNullOrWhiteSpace(s)||s.Length>60))
            throw new InvalidDataException("Invalid names.toml");
    }
    public static void Ensure(Citizen v)
    {
        if(string.IsNullOrWhiteSpace(v.FirstName))v.FirstName=pool.FirstNames[Random.Shared.Next(pool.FirstNames.Length)];
        if(string.IsNullOrWhiteSpace(v.Surname))v.Surname=pool.Surnames[Random.Shared.Next(pool.Surnames.Length)];
    }
}
