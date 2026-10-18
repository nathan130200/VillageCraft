set_script_type(eST_AI, "cat");
const destinations=new Map();
function onAnimalRemoved(animal) { destinations.delete(animal.id); }
function onAnimalUpdate(ctx,dt) {
    const cat=ctx.self;
    if(!cat.tameRequested)cat.state+=dt;
    if(cat.tamed&&!cat.leaving) {
        cat.food-=dt/world.dayLength;cat.milk-=dt/world.dayLength;
        cat.unsupplied=(cat.food<=0||cat.milk<=0)?cat.unsupplied+dt:0;
        if(cat.unsupplied>=world.dayLength*world.catSettings.supplyGraceDays)cat.leaving=true;
    } else if(!cat.tamed&&!cat.tameRequested&&cat.state>=world.dayLength*world.catSettings.wildStayDays)cat.leaving=true;
    if(cat.leaving) {
        if(ctx.x<.15||ctx.z<.15||ctx.x>world.width-1.15||ctx.z>world.height-1.15){cat.remove();destinations.delete(cat.id);return;}
        let edge=destinations.get(cat.id);
        if(!edge||!edge.edge) {
            const candidates=[];
            for(let i=0;i<world.width;i++){candidates.push({x:i,z:0,edge:true},{x:i,z:world.height-1,edge:true});}
            for(let i=1;i<world.height-1;i++){candidates.push({x:0,z:i,edge:true},{x:world.width-1,z:i,edge:true});}
            candidates.sort((a,b)=>ai.distance(ctx,a)-ai.distance(ctx,b));
            edge=candidates.find(p=>ctx.canReach(p.x,p.z));
            if(edge)destinations.set(cat.id,edge);
        }
        if(edge) {
            if(ai.distance(ctx,edge)<.15){cat.remove();destinations.delete(cat.id);}
            else ctx.moveTo(edge.x,edge.z);
        }
        return;
    }
    if(cat.tameRequested||cat.tamed) {
        const cats=world.getAnimals('cat');
        const people=world.getVillagers();
        const free=v=>!cats.some(c=>c.id!==cat.id&&(c.farmerId===v.id||c.courierId===v.id));
        const farmer=people.find(v=>v.id===cat.farmerId&&v.profession==='farmer')||people.find(v=>v.profession==='farmer'&&free(v));
        const courier=people.find(v=>v.id===cat.courierId&&v.profession==='deliveryman')||people.find(v=>v.profession==='deliveryman'&&free(v));
        if(courier&&(farmer||cat.tamed))cat.assignCarers(farmer?farmer.id:-1,courier.id);
        if(cat.tameRequested&&cat.tameFeeds===3)cat.attemptTame();
        // Hold still while both villagers approach and hand over supplies.
        if(cat.tameRequested||cat.food<1||cat.milk<1)return;
    }
    let target=destinations.get(cat.id);
    if(!target||target.edge||ai.distance(ctx,target)<.15||target.until<cat.state) {
        const houses=world.getBuildings().filter(b=>b.homes>0&&b.enabled);
        houses.sort((a,b)=>ai.distance(ctx,a)-ai.distance(ctx,b));
        target=null;
        for(const house of houses.slice(0,8)) {
            const angle=Math.random()*Math.PI*2;
            const p={x:Math.max(0,Math.min(world.width-1,house.x+Math.cos(angle)*1.5)),z:Math.max(0,Math.min(world.height-1,house.z+Math.sin(angle)*1.5)),until:cat.state+15};
            if(ctx.canReach(p.x,p.z)){target=p;destinations.set(cat.id,p);break;}
        }
    }
    if(target)ctx.moveTo(target.x,target.z);
}
