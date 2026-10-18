using System.Text.Json;
using System.Text.Json.Serialization;
namespace VillageCraft;

// Closed simulation states are typed; human-readable strings remain at content/UI/JS boundaries.
[JsonConverter(typeof(TaskIdConverter))]
public enum TaskId { TamingCat, FeedingCat, Resting, DefendingVillage, NightPatrol, GoingHome, SupportingFamilies, Homeless, ScriptedBehavior, CollectingMilk, CollectingFood, WaitingStorage, Delivering, WaitingDeliveries, CollectingDelivery, WaitingTreeRegrowth, GoingToTrees, ChoppingTree, GoingToWork, Working, Assisting, CaravanTravelling, TalkingToResidents, LeavingVillage, WanderingHomeless, SeekingHome, CollectingRepairSupplies, RepairingBuilding }
public enum CaravanState { Entering, Talking, Departing }
public enum PillageState { Looting, Retreating }
public enum EquipmentKind { Armor, Tools }
[JsonConverter(typeof(JsonStringEnumConverter<EffectId>))]
public enum EffectId { Burning, Poison, Slowness, Weakness }
public static class Tasks
{
    public static string Label(TaskId id)=>id switch
    {
        TaskId.Resting => "Resting",
        TaskId.DefendingVillage => "Defending village",
        TaskId.NightPatrol => "Night patrol",
        TaskId.GoingHome => "Going home",
        TaskId.SupportingFamilies => "Helping families grow",
        TaskId.Homeless => "Homeless",
        TaskId.ScriptedBehavior => "Scripted behavior",
        TaskId.CollectingMilk => "Collecting milk",
        TaskId.CollectingFood => "Collecting food",
        TaskId.WaitingStorage => "Waiting for storage space",
        TaskId.Delivering => "Delivering resources",
        TaskId.WaitingDeliveries => "Waiting for deliveries",
        TaskId.CollectingDelivery => "Collecting delivery",
        TaskId.WaitingTreeRegrowth => "Waiting for trees to regrow",
        TaskId.GoingToTrees => "Going to trees",
        TaskId.ChoppingTree => "Chopping tree",
        TaskId.GoingToWork => "Going to work",
        TaskId.Working => "Working",
        TaskId.Assisting => "Assisting",
        TaskId.CaravanTravelling => "Travelling with caravan",
        TaskId.TalkingToResidents => "Talking to residents",
        TaskId.LeavingVillage => "Leaving village",
        TaskId.WanderingHomeless => "Wandering without a home",
        TaskId.SeekingHome => "Seeking a home",
        TaskId.CollectingRepairSupplies => "Collecting repair supplies",
        TaskId.RepairingBuilding => "Repairing building",
        _=>id.ToString()
    };
    public static TaskId Parse(string? text)
    {
        if(Enum.TryParse<TaskId>(text,out var id)&&Enum.IsDefined(id))return id;
        foreach(var candidate in Enum.GetValues<TaskId>())if(Label(candidate)==text)return candidate;
        if(text?.StartsWith("Going to ",StringComparison.Ordinal)==true)return TaskId.GoingToWork;
        if(text?.StartsWith("Assisting: ",StringComparison.Ordinal)==true)return TaskId.Assisting;
        return TaskId.Working;
    }
    public static bool Moving(TaskId id)=>id is TaskId.GoingHome or TaskId.GoingToWork or TaskId.GoingToTrees or TaskId.CollectingFood or TaskId.CollectingMilk or TaskId.CollectingDelivery or TaskId.Delivering or TaskId.LeavingVillage or TaskId.CaravanTravelling or TaskId.WanderingHomeless;
}
public sealed class TaskIdConverter:JsonConverter<TaskId>
{
    public override TaskId Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)=>Tasks.Parse(reader.GetString());
    public override void Write(Utf8JsonWriter writer,TaskId value,JsonSerializerOptions options)=>writer.WriteStringValue(Tasks.Label(value));
}
