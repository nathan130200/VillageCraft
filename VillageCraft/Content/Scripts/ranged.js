set_script_type(eST_AI);

function onMonsterUpdate(ctx, dt) {
    if (!ctx.visible || ctx.distance > ctx.range) ctx.move();
    else if (ctx.cooldown <= 0) ctx.attack();
}
