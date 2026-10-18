set_script_type(eST_Mod);

// Called whenever placement, demolition, training or a new day changes employment.
// One worker per workplace before extra slots; specialists first, citizens assist.
function onAssignJobs() {
    if (world.professionPrioritiesActive) return onReassignProfessions();
    const available = Array.from(world.getVillagers());
    // A town with an active warehouse must retain a courier, including older saves.
    if (available.length > 1 && world.getWorkplaces().some(b => b.enabled && b.connected && b.job === 'Delivery') && !available.some(v => v.profession === 'deliveryman')) {
        const recruit = available.find(v => !v.manualProfession && v.profession === 'citizen') || available.find(v => !v.manualProfession && !v.workplace) || available.find(v => !v.manualProfession);
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
    const professions = world.getProfessions();
    const openings = [];
    for (const building of buildings) {
        const eligible = professions.filter(p => p.id === 'citizen' || p.jobs.includes(building.job) || building.allowedProfessions.includes(p.id));
        eligible.sort((a,b) => a.priority - b.priority || Number(a.id === 'citizen') - Number(b.id === 'citizen'));
        const profession = eligible[0];
        if (!profession) continue;
        for (let slot=0; slot<building.jobs; slot++) openings.push({building, profession, slot});
    }
    openings.sort((a,b) => a.profession.priority - b.profession.priority || a.slot - b.slot || b.building.priority - a.building.priority || a.building.id - b.building.id);
    // Reserve exactly one trained courier even when delivery has the lowest priority.
    const courier = openings.findIndex(o => o.building.job === 'Delivery');
    if (courier >= 0) {
        const opening = openings.splice(courier,1)[0];
        opening.profession = professions.find(p => p.id === 'deliveryman');openings.unshift(opening);
    }
    const assignedPeople = [], assignedJobs = [];
    // Explicit inspector choices survive automatic policies and save/load.
    for (let i=people.length-1;i>=0;i--) {
        const person=people[i];if(!person.manualProfession)continue;
        const slot=openings.findIndex(o => o.building.qualified(person));
        people.splice(i,1);
        if(slot>=0){const opening=openings.splice(slot,1)[0];assignedPeople.push(person.id);assignedJobs.push(opening.building.id);}
    }
    for (const {building, profession} of openings) {
        if (people.length === 0) break;
        const index = Math.max(0, people.findIndex(v => v.profession === profession.id));
        const person = people.splice(index, 1)[0];person.setProfession(profession.id);
        assignedPeople.push(person.id);assignedJobs.push(building.id);
    }
    for (const person of people) person.setProfession('citizen');
    world.assignJobs(assignedPeople, assignedJobs);
}
