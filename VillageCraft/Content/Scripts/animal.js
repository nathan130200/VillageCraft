set_script_type(eST_AI);

// Pen wandering; reproduction and resource yields remain building simulation rules.
/** @param {MobScriptContext} ctx @param {number} dt */
function onAnimalUpdate(ctx, dt) {
    const home = ctx.self.home;
    if (!home) return;
    ctx.moveTo(home.x + Math.sin(world.elapsedDay * 0.5 + ctx.self.age + home.id) * 0.28,
               home.z + Math.cos(world.elapsedDay * 0.4 + home.id) * 0.25);
}
