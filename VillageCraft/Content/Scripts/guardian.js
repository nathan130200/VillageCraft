set_script_type(eST_AI);

// Each guardian keeps a destination until it arrives or the route times out.
const patrols = new Map();
const targets = new Map();
let cleanup = 0;
function onUpdate(dt) {
    cleanup -= dt;
    if (cleanup > 0) return;
    cleanup = 10;
    const living = new Set(world.getGuardians().map(g => g.id));
    for (const id of patrols.keys()) if (!living.has(id)) patrols.delete(id);
    const enemies = new Set(world.getMonsters().filter(m => m.health > 0).map(m => m.id));
    for (const [id, target] of targets) if (!living.has(id) || !enemies.has(target)) targets.delete(id);
}

/** @param {MobScriptContext} ctx @param {number} dt */
function onGuardianUpdate(ctx, dt) {
    // Acquire threats across the whole map. Prefer uncovered enemies, then share evenly.
    const monsters = world.getMonsters().filter(m => m.health > 0);
    const living = new Set(world.getGuardians().map(g => g.id));
    const claims = new Map();
    for (const [id, target] of targets) if (id !== ctx.self.id && living.has(id)) claims.set(target, (claims.get(target) || 0) + 1);
    monsters.sort((a,b) => (claims.get(a.id) || 0) - (claims.get(b.id) || 0)
        || Number(b.id === targets.get(ctx.self.id)) - Number(a.id === targets.get(ctx.self.id))
        || ai.distance(ctx,a) - ai.distance(ctx,b));
    const enemy = monsters.find(m => ctx.canReach(m.x,m.z));
    if (enemy) {
        patrols.delete(ctx.self.id);
        targets.set(ctx.self.id, enemy.id);
        ctx.selectTarget('monster', enemy.id);
        if (ctx.visible && ctx.distance <= ctx.range) ctx.attack();
        else ctx.move();
        return;
    }
    targets.delete(ctx.self.id);
    let target = patrols.get(ctx.self.id);
    if (target) target.remaining -= dt;
    if (!target || target.remaining <= 0 || ai.distance(ctx, target) < 0.15) {
        patrols.delete(ctx.self.id);
        const roads = world.roads().filter(p => ai.distance(ctx, p) > 2);
        // Bounded retries keep disconnected or obstructed destinations from stalling a frame.
        for (let attempt = 0; attempt < Math.min(8, roads.length); attempt++) {
            const candidate = roads[Math.floor(Math.random() * roads.length)];
            if (!ctx.canReach(candidate.x, candidate.z)) continue;
            target = { x: candidate.x, z: candidate.z, remaining: 45 };
            patrols.set(ctx.self.id, target);
            break;
        }
        target = patrols.get(ctx.self.id);
    }
    if (target) ctx.moveTo(target.x, target.z);
}
