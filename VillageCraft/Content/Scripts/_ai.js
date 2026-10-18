// Shared helpers, loaded into each component's isolated engine before its script.
// Underscore-prefixed files are libraries, not standalone components.
const ai = {
    distance(a, b) { return Math.hypot(a.x - b.x, a.z - b.z); },
    nearest(origin, list, accept) {
        let best = null, distance = Infinity;
        for (const item of list) {
            if (accept && !accept(item)) continue;
            const d = ai.distance(origin, item);
            if (d < distance) { best = item; distance = d; }
        }
        return best;
    },
    // Preserve the original priorities, but expose every threshold for mod authors.
    chooseHostileTarget(ctx) {
        const guard = ai.nearest(ctx, world.getGuardians(), g => ctx.canSee(g.x, g.z));
        if (guard && ai.distance(ctx, guard) < 6) return ctx.selectTarget('guardian', guard.id);
        const resident = ai.nearest(ctx, world.getVillagers(), v => v.health > 0 && ctx.canSee(v.x, v.z));
        if (resident && ai.distance(ctx, resident) < 12) return ctx.selectTarget('villager', resident.id);
        const building = ai.nearest(ctx, world.getAttackableBuildings());
        return building ? ctx.selectTarget('building', building.id) : false;
    },
    supply(v, resource, exclude) {
        const candidates = world.getStores().filter(b => b.id !== exclude && b.enabled && b.availableSupply(resource) > 0.001);
        // A reachable warehouse is preferred to an equally accessible production source.
        candidates.sort((a, b) => Number(b.warehouse) - Number(a.warehouse) || ai.distance(v, a) - ai.distance(v, b) || a.id - b.id);
        return candidates.find(b => v.canReach(b)) || null;
    }
};

// Compatibility: custom onMonsterUpdate callbacks still receive an initial target.
// They may replace it with ctx.selectTarget(kind, id) at any time.
/** @param {MobScriptContext} ctx @param {number} dt */
function __monsterUpdate(ctx, dt) {
    ai.chooseHostileTarget(ctx);
    onMonsterUpdate(ctx, dt);
}
