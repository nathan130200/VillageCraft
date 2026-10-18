set_script_type(eST_AI);

/** @param {MobScriptContext} ctx @param {number} dt */
function onMonsterUpdate(ctx, dt) {
    const cats=world.getAnimals('cat').filter(c=>c.health>0&&ai.distance(ctx,c)<=world.catSettings.creeperAvoidRadius);
    if(cats.length>0) {
        ctx.state=0;
        const nearest=ai.nearest(ctx,cats);nearest.purr();
        let dx=ctx.x-nearest.x,dz=ctx.z-nearest.z;
        if(Math.abs(dx)+Math.abs(dz)<.01){dx=Math.cos(ctx.self.id);dz=Math.sin(ctx.self.id);}
        const angle=Math.atan2(dz,dx),goals=[];
        for(let i=0;i<12;i++) {
            const a=angle+(i%2===0?1:-1)*Math.ceil(i/2)*Math.PI/6;
            const goal={x:Math.max(0,Math.min(world.width-1,ctx.x+Math.cos(a)*6)),z:Math.max(0,Math.min(world.height-1,ctx.z+Math.sin(a)*6))};
            goal.safety=Math.min(...cats.map(c=>ai.distance(goal,c)));
            goals.push(goal);
        }
        goals.sort((a,b)=>b.safety-a.safety);
        const goal=goals.find(p=>ctx.canReach(p.x,p.z));
        if(goal)ctx.moveTo(goal.x,goal.z);
        return;
    }
    if (ctx.distance > 1.4 || !ctx.visible) { ctx.state = 0; ctx.move(); return; }
    ctx.state += ctx.dt;
    if (ctx.state >= 0.9) ctx.explode();
}
