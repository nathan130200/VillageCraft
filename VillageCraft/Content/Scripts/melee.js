set_script_type(eST_AI);

// Live context methods execute validated game actions.
function onMonsterUpdate(ctx, dt) {
    if (ctx.visible && ctx.distance <= ctx.range) ctx.attack();
    else ctx.move();
}
