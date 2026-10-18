set_script_type(eST_AI);

// Default for every profession. A profession/instance override takes precedence.
// Separate functions make it easy to copy just one routine into your own script.
/** @param {ScriptVillager} v @param {number} dt */
function onVillagerUpdate(v, dt) {
    const job = v.workplace;
    const defender = job && job.defender.length > 0 && !job.createsGuardian;
    if (defender && defend(v, dt)) return;
    if (personalSupplies(v, dt)) return;
    if (v.helpCat(dt)) return;
    if (job && job.job === 'Delivery' && (!world.night || v.cargoCount > 0) && deliverResources(v, dt)) return;
    if (repairBuildings(v, dt)) return;
    if (defender && world.night) { patrol(v, dt); return; }
    const workTime = !world.night && (world.timeOfDay < 17 / 24 || !v.hasWorked);
    if (job && job.harvestSource.length > 0 && workTime) { harvestTrees(v, job, dt); return; }
    const target = workTime ? job || v.home : v.home;
    if (!target) { v.task = tasks.Homeless; return; }
    v.task = job && target.id === job.id ? tasks.GoingToWork : tasks.GoingHome;
    if (!v.walkTo(target, dt, true)) return;
    if (job && target.id === job.id) {
        v.task = tasks.Working;
        v.work(dt);
    } else v.task = tasks.Resting;
}

/** @param {ScriptVillager} v @param {number} dt */
function defend(v, dt) {
    const enemy = ai.nearest(v, world.getMonsters(), m => m.health > 0);
    if (!enemy) return false;
    v.task = tasks.DefendingVillage;
    if (ai.distance(v, enemy) > v.combatRange * 0.85) v.patrol(enemy.x, enemy.z, dt);
    v.attackMonster(enemy.id);
    return true;
}

/** @param {ScriptVillager} v @param {number} dt */
function patrol(v, dt) {
    const roads = world.roads();
    if (roads.length > 0) {
        const target = roads[(Math.floor(world.elapsedDay / 12) + v.id) % roads.length];
        v.patrol(target.x, target.z, dt);
    }
    v.task = tasks.NightPatrol; v.hasWorked = true;
}

/** @param {ScriptVillager} v @param {number} dt */
function personalSupplies(v, dt) {
    if (v.health < v.maximumHealth || v.hasEffects) {
        if (!v.drinkMilk()) {
            const milk = ai.supply(v, resources.Milk);
            if (milk) { v.task = tasks.CollectingMilk; v.collect(milk, resources.Milk, 1, dt); return true; }
        }
    }
    if (v.inventory(resources.Food) < 1) {
        const food = ai.supply(v, resources.Food);
        if (food) { v.task = tasks.CollectingFood; v.collect(food, resources.Food, 2 - v.inventory(resources.Food), dt); return true; }
    }
    return false;
}

/** @param {ScriptVillager} v @param {ScriptBuilding} camp @param {number} dt */
function harvestTrees(v, camp, dt) {
    let tree = world.building(v.resourceTarget);
    if (!tree || tree.prefab !== camp.harvestSource || tree.growthDays > 0 || ai.distance(camp, tree) > camp.harvestRadius) {
        const reserved = new Set(world.getVillagers().filter(other => other.id !== v.id && (other.task === tasks.GoingToTrees || other.task === tasks.ChoppingTree)).map(other => other.resourceTarget));
        const trees = world.getHarvestSources(camp.harvestSource, camp.x, camp.z, camp.harvestRadius).filter(b => b.growthDays === 0 && !reserved.has(b.id) && ai.distance(camp, b) <= camp.harvestRadius);
        trees.sort((a, b) => ai.distance(v, a) - ai.distance(v, b));
        tree = trees.find(b => v.canReach(b));
        v.resourceTarget = tree ? tree.id : -1;
    }
    if (!tree) { v.resourceTarget = -1; v.walkTo(camp, dt, true); v.task = tasks.WaitingTreeRegrowth; v.hasWorked = true; return; }
    if (ai.distance(v, tree) > 0.4) { v.task = tasks.GoingToTrees; v.walkTo(tree, dt); return; }
    v.task = tasks.ChoppingTree;
    v.chop(tree, dt);
    if (tree.growthDays > 0) {
        v.resourceTarget = -1;
        v.task = tasks.GoingToTrees; // Re-query other grown trees on the next update, not this stump.
    }
}

/** @param {ScriptVillager} v @param {number} dt */
function deliverResources(v, dt) {
    const buildings = world.getStores();
    const warehouses = buildings.filter(b => b.warehouse && b.enabled && b.connected && b.storageFree > 0);
    if (v.cargoCount > 0) {
        let to = world.building(v.deliveryTarget);
        if (!to || !to.enabled || to.storageFree <= 0 || !v.canReach(to)) to = ai.nearest(v, warehouses, b => v.canReach(b));
        if (!to) { v.task = tasks.WaitingStorage; return true; }
        v.deliveryTarget = to.id; v.task = tasks.Delivering;
        if (v.walkTo(to, dt)) v.deliver(to);
        return true;
    }
    let from = world.building(v.pickupSource), to = world.building(v.deliveryTarget), resource = v.deliveryResource;
    if (!from || !to || !resource || !from.enabled || !to.enabled || from.storage(resource) < 0.01 || to.storageFree < 0.01 || !v.canReach(from) || !from.canReach(to)) {
        from = null; to = null;
        const consumers = buildings.filter(b => b.enabled && b.connected && b.storageFree > 0.01 && (b.inputs.length > 0 || b.requestedResource.length > 0));
        // Empty/near-empty inputs outrank filling a nearby building's already healthy buffer.
        function coverage(b) { return Math.min(...Array.from(b.inputs).map(r => b.storage(r) / Math.max(0.001, b.inputAmount(r))), Infinity); }
        consumers.sort((a, b) => Number(coverage(b) < 1) - Number(coverage(a) < 1) || b.priority - a.priority || coverage(a) - coverage(b) || ai.distance(v, a) - ai.distance(v, b));
        const otherWorkers = world.getVillagers().filter(other => other.id !== v.id);
        for (const consumer of consumers) {
            const inputs = Array.from(consumer.inputs);
            if (consumer.requestedResource && !inputs.includes(consumer.requestedResource)) inputs.push(consumer.requestedResource);
            for (const input of inputs) {
                const target = consumer.requestedResource === input ? 20 : consumer.inputTarget(input);
                const refillAt = consumer.requestedResource === input ? target : target * 0.5;
                if (consumer.storage(input) >= refillAt - 0.001 || otherWorkers.some(other => other.deliveryTarget === consumer.id && other.deliveryResource === input && (other.cargoCount > 0 || other.pickupSource >= 0))) continue;
                const source = ai.supply(v, input, consumer.id);
                if (!source || !source.canReach(consumer)) continue;
                from = source; to = consumer; resource = input; break;
            }
            if (from) break;
        }
        if (!from) {
            const sources = buildings.filter(b => !b.warehouse && b.enabled && Array.from(b.outputs).some(r => b.availableSupply(r) > 0.001));
            sources.sort((a, b) => ai.distance(v, a) - ai.distance(v, b));
            for (const source of sources) {
                const warehouse = ai.nearest(source, warehouses, b => source.canReach(b));
                if (!warehouse || !v.canReach(source)) continue;
                from = source; to = warehouse; resource = Array.from(source.outputs).find(r => source.availableSupply(r) > 0.001); break;
            }
        }
        if (!from || !to) { v.pickupSource = -1; v.deliveryTarget = -1; v.task = tasks.WaitingDeliveries; return false; }
        v.pickupSource = from.id; v.deliveryTarget = to.id; v.deliveryResource = resource;
    }
    if (!resource) { v.pickupSource = -1; return false; }
    v.task = tasks.CollectingDelivery;
    if (v.walkTo(from, dt)) { v.pickup(from, to, resource); v.pickupSource = -1; }
    return true;
}


// Every profession can help repair. Reserve one worker per building to avoid crowds.
/** @param {ScriptVillager} v @param {number} dt */
function repairBuildings(v, dt) {
    // Check the complete recipe before leaving home, not just the first material.
    const stores = world.getStores();
    function supplied(b) {
        return Array.from(b.repairInputs).every(r => v.inventory(r) + stores
            .filter(s => s.enabled && s.availableSupply(r) > 0 && v.canReach(s))
            .reduce((sum,s) => sum + s.availableSupply(r), 0) >= b.repairCost(r));
    }
    let building = world.building(v.repairTarget);
    if (!building || building.health >= building.maximumHealth || !v.canReach(building) || !supplied(building)) {
        v.repairTarget = -1;
        const reserved = new Set(world.getVillagers().filter(other => other.id !== v.id).map(other => other.repairTarget));
        const damaged = world.getDamagedBuildings().filter(b => !reserved.has(b.id));
        damaged.sort((a, b) => a.health / a.maximumHealth - b.health / b.maximumHealth || ai.distance(v, a) - ai.distance(v, b));
        building = damaged.find(b => v.canReach(b) && supplied(b));
        if (building) v.repairTarget = building.id;
    }
    if (!building) return false;
    for (const resource of building.repairInputs) {
        const missing = building.repairCost(resource) - v.inventory(resource);
        if (missing <= 0) continue;
        const source = ai.supply(v, resource);
        if (!source) { v.repairTarget = -1; return false; }
        v.task = tasks.CollectingRepairSupplies;
        v.collect(source, resource, missing, dt);
        return true;
    }
    v.task = tasks.RepairingBuilding;
    if (v.walkTo(building, dt)) v.repair(building, dt);
    if (building.health >= building.maximumHealth) v.repairTarget = -1;
    return true;
}
