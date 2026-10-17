set_script_type(eST_AI);

function onMonsterUpdate(ctx, dt) {
    if (ctx.distance > 1.4 || !ctx.visible) { ctx.state = 0; ctx.move(); return; }
    ctx.state += ctx.dt;
    if (ctx.state >= 0.9) ctx.explode();
}
