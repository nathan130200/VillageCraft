set_script_type(eST_AI);

// Guardian pursuit and idle patrol. Copy and target iron_golem/snow_golem to override.
/** @param {MobScriptContext} ctx @param {number} dt */
function onGuardianUpdate(ctx, dt) {
    const enemy = ai.nearest(ctx, world.getMonsters(), m => m.health > 0);
    if (enemy) {
        ctx.selectTarget('monster', enemy.id);
        if (ctx.visible && ctx.distance <= ctx.range) ctx.attack();
        else ctx.move();
        return;
    }
    const home = ctx.self.home;
    ctx.moveTo(Math.max(0, Math.min(world.width - 1, (home ? home.x : world.anchorX) + Math.sin(world.elapsedDay * 0.2) * 2)), home ? home.z : world.anchorZ);
}
