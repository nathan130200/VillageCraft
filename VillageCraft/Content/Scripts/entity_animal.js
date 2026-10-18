set_script_type(eST_AI);

// Pen wandering; reproduction and resource yields remain building simulation rules.
/** @param {MobScriptContext} ctx @param {number} dt */
function onAnimalUpdate(ctx, dt) {
    if (world.night) return; // Animals sleep indoors; no wandering until dawn.
    const home = ctx.self.home;
    if (!home) return;
    // Siblings share age and home: use their unique ID so they do not follow one path in lockstep.
    const phase = ctx.self.id * 2.3999632297;
    ctx.moveTo(home.x + Math.sin(world.elapsedDay * 0.5 + phase) * 0.22,
               home.z + Math.cos(world.elapsedDay * 0.4 + phase) * 0.22);
}
