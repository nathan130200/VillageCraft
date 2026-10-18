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
    // Seek the closest opponent across the map; visibility only gates attacks.
    chooseHostileTarget(ctx) {
        let best = null, kind = '', distance = Infinity;
        for (const [type, list] of [['guardian', world.getGuardians()], ['villager', world.getVillagers()], ['building', world.getAttackableBuildings()]]) {
            const candidate = ai.nearest(ctx, list, item => item.health > 0);
            if (candidate && ai.distance(ctx, candidate) < distance) {
                best = candidate; kind = type; distance = ai.distance(ctx, candidate);
            }
        }
        return best ? ctx.selectTarget(kind, best.id) : false;
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
