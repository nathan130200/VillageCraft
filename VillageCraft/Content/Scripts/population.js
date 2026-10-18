set_script_type(eST_Mod);

// Each vacant bed in a home with two adults gets one daily roll.
function onDayStarted() {
    const people = world.getVillagers();
    for (const home of world.getBuildings().filter(b => b.homes > 0 && b.enabled && b.connected)) {
        const residents = people.filter(v => v.home && v.home.id === home.id).length;
        if (residents < 2) continue;
        const vacant = Math.max(0, home.homes - residents - home.pendingBirths);
        for (let bed=0; bed<vacant; bed++)
            if (Math.random() < world.birthChance) world.queueBirth(home.id);
    }
}
