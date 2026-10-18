set_script_type(eST_Mod);

// Called whenever placement, demolition, training or a new day changes employment.
// One qualified specialist per workplace before extra slots.
function onAssignJobs() {
    if (world.professionPrioritiesActive) return onReassignProfessions();
    const available = Array.from(world.getVillagers());
    // A town with an active warehouse must retain a courier, including older saves.
    if (available.length > 1 && world.getWorkplaces().some(b => b.enabled && b.connected && b.job === 'Delivery') && !available.some(v => v.profession === 'deliveryman')) {
        const recruit = available.find(v => !v.manualProfession && !v.workplace) || available.find(v => !v.manualProfession);
        if (recruit) recruit.setProfession('deliveryman');
    }
    available.sort((a,b)=>a.id-b.id);
    const workplaces = world.getWorkplaces().filter(b => b.enabled && b.connected);
    workplaces.sort((a, b) => b.priority - a.priority || a.id - b.id);
    const people = [], jobs = [];
    const maxSlots = workplaces.reduce((max, b) => Math.max(max, b.jobs), 0);
    for (let slot = 0; slot < maxSlots; slot++) {
        for (const building of workplaces) {
            if (slot >= building.jobs) continue;
            let index = available.findIndex(v => building.qualified(v));
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
        const eligible = professions.filter(p => p.jobs.includes(building.job) || building.allowedProfessions.includes(p.id));
        eligible.sort((a,b) => a.priority - b.priority);
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
    // Surplus residents retain a real profession while waiting for a workplace.
    world.assignJobs(assignedPeople, assignedJobs);
}

// A grown villager receives a specialty for an actual vacancy, without retraining the whole city.
function onVillagerBorn(person) { assignNewResident(person); }
function assignNewResident(person) {
    const people=world.getVillagers();
    const professions=world.getProfessions();
    const occupants=people.filter(v=>v.id!==person.id);
    const workers=b=>occupants.filter(v=>v.workplace&&v.workplace.id===b.id).length;
    const defenseNeeded=world.getMonsters().length>world.getGuardians().length+occupants.filter(v=>v.workplace&&v.workplace.defender.length>0).length;
    const specialtyPriority=b=>Math.min(...professions.filter(p=>p.jobs.includes(b.job)||b.allowedProfessions.includes(p.id)).map(p=>p.priority),5);
    const openings=world.getWorkplaces().filter(b=>b.enabled&&b.connected&&workers(b)<b.jobs);
    openings.sort((a,b)=>Number(b.job==='Delivery'&&!people.some(v=>v.profession==='deliveryman'))-Number(a.job==='Delivery'&&!people.some(v=>v.profession==='deliveryman'))
        || Number(b.job==='Farming'&&world.stock(resources.Food)<people.length*3)-Number(a.job==='Farming'&&world.stock(resources.Food)<people.length*3)
        || Number(defenseNeeded&&b.defender.length>0&&!b.createsGuardian)-Number(defenseNeeded&&a.defender.length>0&&!a.createsGuardian)
        || Number(workers(b)===0)-Number(workers(a)===0)||specialtyPriority(a)-specialtyPriority(b)
        || b.priority-a.priority||a.id-b.id);
    for(const building of openings) {
        const eligible=professions.filter(p=>p.jobs.includes(building.job)||building.allowedProfessions.includes(p.id));
        eligible.sort((a,b)=>a.priority-b.priority);
        if(eligible.length===0)continue;
        person.setProfession(eligible[0].id);
        const assigned=people.filter(v=>v.id!==person.id&&v.workplace);
        world.assignJobs(assigned.map(v=>v.id).concat(person.id),assigned.map(v=>v.workplace.id).concat(building.id));
        return;
    }
    // No vacant workplace: keep a real specialty, never an unemployed profession.
    person.setProfession(people.filter(v=>v.profession==='farmer').length<2?'farmer':'deliveryman');
}
