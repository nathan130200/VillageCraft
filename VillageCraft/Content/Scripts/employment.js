set_script_type(eST_Mod);

// Called whenever placement, demolition, training or a new day changes employment.
// One worker per workplace before extra slots; specialists first, citizens assist.
function onAssignJobs() {
    const available = Array.from(world.getVillagers());
    // A town with an active warehouse must retain a courier, including older saves.
    if (available.length > 1 && world.getWorkplaces().some(b => b.enabled && b.connected && b.job === 'Delivery') && !available.some(v => v.profession === 'deliveryman')) {
        const recruit = available.find(v => v.profession === 'citizen') || available.find(v => !v.workplace) || available[available.length - 1];
        if (recruit) recruit.setProfession('deliveryman');
    }
    available.sort((a, b) => Number(a.profession === 'citizen') - Number(b.profession === 'citizen') || a.id - b.id);
    const workplaces = world.getWorkplaces().filter(b => b.enabled && b.connected);
    workplaces.sort((a, b) => b.priority - a.priority || a.id - b.id);
    const people = [], jobs = [];
    const maxSlots = workplaces.reduce((max, b) => Math.max(max, b.jobs), 0);
    for (let slot = 0; slot < maxSlots; slot++) {
        for (const building of workplaces) {
            if (slot >= building.jobs) continue;
            let index = available.findIndex(v => v.profession !== 'citizen' && building.qualified(v));
            if (index < 0) index = available.findIndex(v => v.profession === 'citizen');
            if (index < 0) continue;
            people.push(available[index].id); jobs.push(building.id);
            available.splice(index, 1);
        }
    }
    world.assignJobs(people, jobs);
}

// Explicit bulk reassignment requested from the People panel; no per-villager clicks.
function onReassignProfessions() {
    const people = Array.from(world.getVillagers());
    const buildings = world.getWorkplaces().filter(b => b.enabled && b.connected);
    buildings.sort((a, b) => Number(b.job === 'Delivery') - Number(a.job === 'Delivery') || b.priority - a.priority || a.id - b.id);
    const professions = world.getProfessions();
    const assignedPeople = [], assignedJobs = [];
    const maxSlots = buildings.reduce((max, b) => Math.max(max, b.jobs), 0);
    for (let slot = 0; slot < maxSlots; slot++) for (const building of buildings) {
        if (slot >= building.jobs || people.length === 0) continue;
        const profession = professions.find(p => p.id !== 'citizen' && (p.jobs.includes(building.job) || building.allowedProfessions.includes(p.id)));
        if (!profession) continue;
        const index = Math.max(0, people.findIndex(v => v.profession === profession.id));
        const person = people.splice(index, 1)[0];person.setProfession(profession.id);
        assignedPeople.push(person.id);assignedJobs.push(building.id);
    }
    for (const person of people) person.setProfession('citizen');
    world.assignJobs(assignedPeople, assignedJobs);
}
