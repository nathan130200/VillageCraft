set_script_type(eST_Building);

// chicken_coop: bound by Prefabs/chicken_coop.toml. Edit this component independently.
// Costs, inputs, outputs and statistics remain in TOML.
/** @param {ScriptBuilding} building @param {ScriptVillager} worker */
function onBuildingProduce(building, worker) {
    return building.produce(worker);
}

/** @param {ScriptBuilding} building */
function onBuildingDay(building) {
    if (!building.worked) return;
    if (world.day % 3 === 0) {
        building.serviceCredit += building.efficiency;
        building.breed();
    }
    if (building.adultAnimals >= 2)
        building.collectAnimalProduct(resources.Eggs, building.adultAnimals * building.efficiency);
}
