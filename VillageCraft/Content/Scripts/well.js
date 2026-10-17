set_script_type(eST_Building);

function onBuildingDay(building) {
    building.repair(2);
}
