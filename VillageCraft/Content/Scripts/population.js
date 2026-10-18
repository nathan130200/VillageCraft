set_script_type(eST_Mod);

// One roll per occupied home per day; a queued birth reserves a free bed.
function onDayStarted() {
    const people = world.getVillagers();
    for (const home of world.getBuildings().filter(b => b.homes > 0 && b.enabled && b.connected)) {
        const residents = people.filter(v => v.home && v.home.id === home.id).length;
        if (residents >= 2 && residents < home.homes && Math.random() < world.birthChance)
            world.queueBirth(home.id);
    }
}
