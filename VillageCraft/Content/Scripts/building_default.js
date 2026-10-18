set_script_type(eST_Building);

// Default building component: validated C# actions apply recipes and spawn units.
// Override onBuildingProduce to change WHEN a recipe runs, or to supply your own work.
/** @param {ScriptBuilding} building @param {ScriptVillager} worker */
function onBuildingProduce(building, worker) {
    if (building.harvestSource.length > 0 || building.job === 'Delivery') return false;
    if (building.createsGuardian) return building.rewardWork(worker);
    return building.produce(worker);
}

/** @param {ScriptBuilding} building */
function onBuildingDay(building) {
    if (!building.worked) return;
    if (building.animal.length > 0) {
        if (world.day % 3 === 0) {
            building.serviceCredit += building.efficiency;
            building.breed();
        }
        const product = { sheep: resources.Wool, chicken: resources.Eggs, pig: resources.Food }[building.animal];
        if (product && building.adultAnimals >= 2) building.collectAnimalProduct(product, building.adultAnimals * building.efficiency);
    }
    if (building.createsGuardian && building.defender.length > 0 && world.day % 3 === 0) {
        building.serviceCredit += building.efficiency;
        building.createGuardian();
    }
}
