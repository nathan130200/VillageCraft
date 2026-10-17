# Authoring utility: generates the editable external catalog. The game never runs this script.
$ErrorActionPreference = 'Stop'
$models = [ordered]@{}
function Part([double]$x,[double]$y,[double]$z,[double]$w,[double]$h,[double]$d,$color) { @{ Position=@($x,$y,$z); Size=@($w,$h,$d); Color=$color } }
$oak='#A9814D'; $log='#65492D'; $stone='#858784'; $roof='#84532E'; $glass='#9BC6CC'
function House($roofColor,$height=0.75) {
    $parts = [System.Collections.Generic.List[object]]::new()
    $parts.Add((Part 0 .07 0 .94 .14 .92 $stone))
    for($layer=0;$layer -lt 5;$layer++) {
        $y=.17+$layer*.12
        $parts.Add((Part 0 $y -.37 .78 .115 .08 $oak))
        $parts.Add((Part -.37 $y 0 .08 .115 .78 $oak))
        $parts.Add((Part .37 $y 0 .08 .115 .78 $oak))
        $parts.Add((Part -.27 $y .37 .22 .115 .08 $oak))
        $parts.Add((Part .27 $y .37 .22 .115 .08 $oak))
    }
    foreach($x in @(-.38,.38)){foreach($z in @(-.38,.38)){$parts.Add((Part $x .45 $z .12 .76 .12 $log))}}
    $parts.Add((Part 0 .34 .397 .21 .5 .035 '#6B4829'))
    $parts.Add((Part .065 .34 .42 .025 .025 .02 '#D5B34D'))
    $parts.Add((Part 0 .09 .46 .28 .13 .17 $stone))
    foreach($x in @(-.29,.29)){$parts.Add((Part $x .52 .417 .13 .16 .022 $glass));$parts.Add((Part $x .52 .433 .018 .16 .01 '#DED0A5'))}
    for($i=0;$i -lt 5;$i++){
        $parts.Add((Part 0 (.8+$i*.09) 0 (1.03-$i*.19) .10 1.02 $roofColor))
    }
    $parts.Add((Part -.46 .54 .32 .04 .08 .04 '#FFCE6B'))
    return $parts.ToArray()
}
$models.path=@((Part 0 .012 0 1 .025 1 '#B9A170'),(Part -.25 .03 .15 .21 .012 .18 '#D0B984'),(Part .24 .03 -.25 .27 .012 .16 '#A99163'))
$models.cottage=@(House $roof)
$models.large_house=@(House '#6D4937')+@((Part .24 1.15 -.2 .16 .55 .18 $stone),(Part .24 1.45 -.2 .22 .07 .24 '#585C5A'))
$models.farm=@((Part 0 .04 0 .94 .08 .94 '#624326'))
for($x=-3;$x -le 3;$x++){for($z=-3;$z -le 3;$z++){
    if($x -eq 0){$models.farm+=Part 0 .09 ($z*.13) .09 .025 .13 '#468FB5'}
    else{$models.farm+=Part ($x*.125) .23 ($z*.125) .035 .28 .035 '#C7AB41';$models.farm+=Part ($x*.125+.03) .32 ($z*.125) .075 .07 .035 '#E3C55A'}
}}
foreach($x in @(-.47,.47)){$models.farm+=Part $x .12 0 .06 .22 1 $log}
$models.lumber=@((Part 0 .05 0 .95 .1 .95 '#81734A'))
for($i=0;$i -lt 4;$i++){$models.lumber+=Part (-.3+$i*.15) .18 .25 .13 .17 .58 $log;$models.lumber+=Part (-.3+$i*.15) .18 .55 .10 .11 .018 '#C2A574'}
$models.lumber+=@((Part .25 .45 -.22 .12 .9 .12 $log),(Part .25 .92 -.22 .52 .65 .5 '#397144'),(Part .25 1.29 -.22 .34 .25 .34 '#4F8F42'),(Part -.24 .21 -.25 .27 .4 .27 $log),(Part -.24 .43 -.25 .3 .05 .3 '#C2A574'))
$models.quarry=@((Part 0 .02 0 .94 .06 .94 '#696E6E'))
for($i=0;$i -lt 5;$i++){$models.quarry+=Part (-.36+$i*.17) (.12+$i*.06) -.1 .17 (.2+$i*.12) .55 '#92968E'}
$models.quarry+=@((Part -.3 .55 .25 .07 .9 .07 $log),(Part .3 .55 .25 .07 .9 .07 $log),(Part 0 .95 .25 .75 .08 .08 $log),(Part 0 .6 .25 .02 .55 .02 '#C4B59B'),(Part 0 .31 .25 .18 .16 .2 '#5E6263'))
$models.well=@()
foreach($p in @(@(-.25,0,.15,.65),@(.25,0,.15,.65),@(0,-.25,.4,.15),@(0,.25,.4,.15))){$models.well+=Part $p[0] .25 $p[1] $p[2] .45 $p[3] $stone}
$models.well+=@((Part 0 .2 0 .35 .02 .35 '#468FB5'),(Part -.32 .6 0 .07 1 .07 $log),(Part .32 .6 0 .07 1 .07 $log),(Part 0 1.08 0 .86 .12 .75 $roof),(Part 0 .7 0 .03 .45 .03 '#C4B59B'))
$models.market=@(House '#4D7F73')+@((Part 0 .58 .48 .85 .12 .27 '#C6A04E'),(Part -.35 .25 .45 .13 .3 .18 '#A24C35'),(Part .35 .25 .45 .13 .3 .18 '#5C9048'))
$models.smith=@(House '#555A5C')+@((Part .28 .3 .47 .3 .42 .2 '#454646'),(Part .28 .3 .58 .15 .14 .02 '#F99637'),(Part -.25 .25 .48 .28 .12 .2 '#343E43'),(Part -.25 .16 .48 .13 .18 .12 '#343E43'),(Part .25 1.2 -.25 .18 .7 .2 $stone))
$models.park=@((Part 0 .45 0 .13 .9 .13 $log),(Part 0 1 0 .65 .6 .65 '#468243'),(Part 0 1.35 0 .4 .25 .4 '#5D9848'),(Part 0 .17 .38 .65 .11 .16 $oak),(Part -.35 .1 -.3 .08 .2 .08 '#E8C65A'),(Part .3 .1 .3 .08 .2 .08 '#CF5C52'))
$models.pen=@((Part 0 .03 0 .95 .06 .95 '#84A357'))
foreach($x in @(-.45,0,.45)){foreach($z in @(-.45,.45)){if($x -eq 0 -and $z -gt 0){continue};$models.pen+=Part $x .24 $z .055 .45 .055 $log}}
foreach($x in @(-.45,.45)){$models.pen+=Part $x .25 0 .04 .07 .9 $oak;$models.pen+=Part $x .4 0 .04 .07 .9 $oak}
$models.pen+=@((Part 0 .25 -.45 .9 .07 .04 $oak),(Part 0 .4 -.45 .9 .07 .04 $oak),(Part -.28 .13 .28 .24 .2 .17 '#A88840'),(Part -.28 .24 .28 .18 .015 .12 '#D7C46A'))
$models.coop=@($models.pen)+@((Part .25 .3 -.25 .4 .55 .37 '#B68B52'),(Part .25 .64 -.25 .5 .12 .48 $roof),(Part .25 .24 -.045 .16 .22 .02 '#45392D'))
$models.barracks=@(House '#6B7276')+@((Part -.37 1.35 0 .04 .5 .04 $log),(Part -.23 1.48 0 .25 .25 .02 '#AA4136'))
$models.archery=@($models.barracks)+@((Part .35 .7 .49 .28 .4 .025 '#D0B27A'),(Part .35 .7 .51 .17 .25 .02 '#B1513F'),(Part .35 .7 .53 .07 .11 .02 '#E2CF9B'))
$models.mage=@(House '#745381')+@((Part 0 1.4 0 .15 .4 .15 '#966EC6'),(Part 0 1.66 0 .23 .16 .23 '#BE91F0'))
$models.iron_foundry=@($models.smith)+@((Part -.28 .54 .49 .17 .13 .14 '#D5D1B9'),(Part -.28 .43 .49 .22 .17 .12 '#D5D1B9'))
$models.snow_shrine=@($models.well)+@((Part 0 .5 .22 .28 .25 .24 '#E6EEE9'),(Part 0 .74 .22 .22 .24 .22 '#F1F4EF'),(Part 0 .96 .22 .23 .21 .23 '#C77A30'))
$models.wall=@((Part 0 .5 0 .98 1 .28 $stone))
for($i=-2;$i -le 2;$i++){$models.wall+=Part ($i*.2) 1.08 0 .12 .2 .3 '#969A93'}
$models.gate=@($models.path)+@((Part -.37 .65 0 .22 1.3 .35 $stone),(Part .37 .65 0 .22 1.3 .35 $stone),(Part 0 1.27 0 .98 .22 .35 $stone))
for($i=-2;$i -le 2;$i++){$models.gate+=Part ($i*.1) .5 0 .04 .95 .08 '#715735'}
$models.tower=@((Part 0 .75 0 .7 1.5 .7 $stone),(Part 0 1.53 0 .95 .15 .95 '#969A93'),(Part 0 1.8 0 .1 .5 .1 $log))
function Humanoid($body,$skin,$hat=''){
    $p=@((Part -.065 .18 0 .10 .36 .12 '#493A30'),(Part .065 .18 0 .10 .36 .12 '#493A30'),(Part 0 .48 0 .27 .4 .18 $body),(Part 0 .83 0 .24 .27 .24 $skin),(Part 0 .81 .155 .07 .11 .11 $skin),(Part -.055 .85 .125 .04 .025 .015 '#31513B'),(Part .055 .85 .125 .04 .025 .015 '#31513B'),(Part 0 .91 .128 .15 .024 .015 '#5A4736'),(Part 0 .49 .13 .32 .1 .1 $skin))
    if($hat -ne ''){$p+=Part 0 1 0 .36 .055 .35 $hat;$p+=Part 0 1.055 0 .25 .09 .25 $hat};return $p
}
$models.villager=@(Humanoid '#826548' '#B48B6C')
$models.farmer=@(Humanoid '#87703F' '#B48B6C' '#C8AB56')
$models.woodcutter=@(Humanoid '#73533B' '#B48B6C')+@((Part .2 .48 .18 .035 .38 .035 $log),(Part .23 .65 .18 .14 .12 .035 '#B6B8B2'))
$models.miner=@(Humanoid '#626567' '#B48B6C' '#6B6C62')+@((Part .2 .5 .16 .035 .4 .035 $log),(Part .2 .69 .16 .23 .045 .05 '#A2ABAD'))
$models.smith_worker=@(Humanoid '#483F37' '#B48B6C')+@((Part 0 .48 .108 .2 .3 .02 '#3B3431'))
$models.herder=@(Humanoid '#A88470' '#B48B6C' '#825A3E')
$models.warrior=@(Humanoid '#8C989D' '#B48B6C' '#8C989D')+@((Part .21 .5 .12 .04 .6 .05 '#C8D4D5'),(Part -.22 .5 .1 .18 .3 .06 '#795C3D'))
$models.archer=@(Humanoid '#577745' '#B48B6C' '#577745')+@((Part .23 .52 .12 .035 .48 .03 '#A68A58'),(Part .17 .52 .12 .015 .46 .015 '#E4D9B3'))
$models.mage_worker=@(Humanoid '#71568C' '#B48B6C' '#71568C')+@((Part .24 .47 .12 .035 .8 .035 $log),(Part .24 .9 .12 .12 .12 .12 '#BD8CE9'))
$models.iron_golem=@((Part 0 .68 0 .42 .5 .25 '#C8C9B8'),(Part 0 1.04 0 .23 .27 .23 '#D5D4C3'),(Part -.34 .59 0 .15 .7 .16 '#BDBFAE'),(Part .34 .59 0 .15 .7 .16 '#BDBFAE'),(Part -.13 .21 0 .15 .42 .18 '#BDBFAE'),(Part .13 .21 0 .15 .42 .18 '#BDBFAE'),(Part 0 1 .16 .07 .14 .12 '#AEAA97'),(Part -.06 1.08 .12 .05 .025 .015 '#9D3932'),(Part .06 1.08 .12 .05 .025 .015 '#9D3932'),(Part -.12 .7 .135 .05 .35 .02 '#587442'))
$models.snow_golem=@((Part 0 .2 0 .32 .4 .32 '#E0E9E7'),(Part 0 .52 0 .25 .3 .25 '#F0F2EC'),(Part 0 .8 0 .28 .26 .28 '#C87C32'),(Part -.3 .52 0 .4 .035 .035 $log),(Part .3 .52 0 .4 .035 .035 $log),(Part -.065 .84 .15 .05 .04 .015 '#443126'),(Part .065 .84 .15 .05 .04 .015 '#443126'),(Part 0 .75 .15 .12 .04 .015 '#443126'))
function Animal($skin,$body){
    $p=@((Part 0 .24 0 .22 .24 .36 $body),(Part 0 .37 .2 .17 .18 .19 $skin))
    foreach($x in @(-.075,.075)){foreach($z in @(-.12,.12)){$p+=Part $x .08 $z .055 .16 .055 $skin}}
    $p+=Part -.045 .4 .301 .025 .025 .01 '#252B28';$p+=Part .045 .4 .301 .025 .025 .01 '#252B28';return $p
}
$models.sheep=@(Animal '#A28D7B' '#EEE9DB')
$models.pig=@(Animal '#D58B8E' '#E6A1A2')+@((Part 0 .34 .31 .1 .07 .04 '#C77E80'))
$models.chicken=@((Part 0 .12 0 .096 .096 .128 '#FFFFFF'),(Part 0 .208 .092 .064 .096 .048 '#FFFFFF'),(Part 0 .192 .132 .064 .032 .032 '#FFFFFF'),(Part 0 .16 .116 .032 .032 .032 '#FFFFFF'),(Part -.032 .04 .016 .048 .08 .048 '#FFFFFF'),(Part .032 .04 .016 .048 .08 .048 '#FFFFFF'),(Part -.056 .128 0 .016 .064 .096 '#FFFFFF'),(Part .056 .128 0 .016 .064 .096 '#FFFFFF'))
$models.zombie=@(Humanoid '#387979' '#62834D')
$models.piglin=@(Humanoid '#623D32' '#C19A8B')+@((Part .21 .48 .1 .045 .48 .045 '#E5BF50'),(Part -.17 .96 0 .12 .19 .05 '#C19A8B'),(Part .17 .96 0 .12 .19 .05 '#C19A8B'))
$models.blaze=@((Part 0 .72 0 .26 .26 .26 '#D8AA3B'),(Part -.06 .75 .14 .04 .035 .015 '#392C20'),(Part .06 .75 .14 .04 .035 .015 '#392C20'))
for($i=0;$i -lt 8;$i++){$angle=$i*[Math]::PI/4;$models.blaze+=Part ([Math]::Cos($angle)*.25) (.2+($i%3)*.16) ([Math]::Sin($angle)*.25) .055 .3 .055 '#E7B73D'}
$prefabs=[System.Collections.Generic.List[object]]::new()
function Prefab($id,$name,$category,$model,$job,$jobs,$emerald,$wood,$stone,$description,$extra=@{}) {
    $p=@{Id=$id;Name=$name;Category=$category;Model=$model;Job=$job;Jobs=$jobs;Upkeep=[Math]::Max(0,$jobs);Cost=@{Emeralds=$emerald;Wood=$wood;Stone=$stone};Description=$description}
    foreach($key in $extra.Keys){$p[$key]=$extra[$key]};$prefabs.Add($p)
}
Prefab path 'Dirt path' Infrastructure path '' 0 2 0 0 'Connect entrances to the central square.' @{Path=$true;NeedsPath=$false;Health=60}
Prefab cobble_road 'Cobblestone road' Roads cobble_road '' 0 5 0 3 'Fast for stonecutters and smiths; rough under farmers feet.' @{Path=$true;NeedsPath=$false;MoveSpeed=1.15;JobSpeed=@{Stonecutting=1.35;Forging=1.25;Farming=.8};Health=150}
Prefab gravel_road 'Gravel trail' Roads gravel_road '' 0 3 0 1 'Inexpensive. Woodcutters move faster; armored warriors move slower.' @{Path=$true;NeedsPath=$false;MoveSpeed=1.05;JobSpeed=@{Woodcutting=1.4;warrior=.75;'Warrior training'=.75};Health=90}
Prefab plank_road 'Oak boardwalk' Roads plank_road '' 0 4 3 0 'Quick steps for shepherds and poultry keepers; stonecutters slow down.' @{Path=$true;NeedsPath=$false;MoveSpeed=1.15;JobSpeed=@{Shepherding=1.3;'Poultry keeping'=1.3;'Pig keeping'=1.3;Stonecutting=.7};Health=85}
Prefab ice_road 'Packed ice road' Roads ice_road '' 0 8 0 2 'Snow golems glide rapidly; most villagers struggle for traction.' @{Path=$true;NeedsPath=$false;MoveSpeed=.8;JobSpeed=@{snow_golem=2.8;Archery=1.4;archer=1.4};Health=100}
Prefab soul_road 'Soul sand way' Roads soul_road '' 0 7 0 2 'Magic villagers gain speed; other travelers slow down.' @{Path=$true;NeedsPath=$false;MoveSpeed=.65;JobSpeed=@{Spellcasting=2.8;mage=2.8;iron_golem=.6};Health=100}
Prefab cottage 'Oak cottage' Housing cottage '' 0 65 12 4 'Four beds, oak walls and a stepped plank roof.' @{Homes=4;Upkeep=1}
Prefab family_house 'Chimney house' Housing large_house '' 0 110 20 12 'A larger household with room for six residents.' @{Homes=6;Upkeep=2;Health=180}
Prefab farm 'Wheat farm' Production farm Farming 3 85 10 0 'Farmers tend crops on site and harvest food.' @{Output=@{Food=4}}
Prefab lumber 'Lumber camp' Production lumber Woodcutting 3 75 8 2 'Lumberjacks chop and stack renewable timber.' @{Output=@{Wood=3}}
Prefab quarry 'Stone quarry' Production quarry Stonecutting 3 95 12 0 'Stonecutters mine stone and small amounts of iron ore.' @{Output=@{Stone=3;Iron=.4}}
Prefab forge 'Blacksmith forge' Production smith Forging 2 140 20 25 'Smiths consume iron and timber to forge tools.' @{Input=@{Iron=1;Wood=1};Output=@{Tools=2}}
Prefab well 'Village well' Infrastructure well '' 0 45 4 12 'Clean water for sixteen villagers.' @{Water=16;Upkeep=1}
Prefab market 'Trading hall' Economy market Trading 2 120 18 12 'Staff this hall to import or export resources.' @{}
Prefab park 'Village green' Infrastructure park '' 0 40 4 2 'Trees, flowers and seating raise happiness.' @{Joy=8;Upkeep=1}
Prefab sheep_pen 'Shepherd pasture' Animals pen Shepherding 2 85 16 2 'Keepers feed sheep, breed lambs and gather wool.' @{Animal='sheep';Input=@{Food=.25}}
Prefab pig_pen 'Pig pasture' Animals pen 'Pig keeping' 2 85 16 2 'Keepers feed pigs, breed piglets and produce food.' @{Animal='pig';Input=@{Food=.25}}
Prefab chicken_coop 'Chicken coop' Animals coop 'Poultry keeping' 2 70 18 0 'Keepers breed chicks and gather eggs.' @{Animal='chicken';Input=@{Food=.15}}
Prefab barracks 'Warrior barracks' Defense barracks 'Warrior training' 3 130 15 20 'Warriors intercept raiders with swords and shields.' @{Defender='warrior';Cost=@{Emeralds=130;Wood=15;Stone=20;Tools=4};Health=200}
Prefab archery 'Archery range' Defense archery Archery 3 130 22 12 'Archers fire at enemies from a longer distance.' @{Defender='archer';Cost=@{Emeralds=130;Wood=22;Stone=12;Tools=3}}
Prefab mage_hall 'Enchanter hall' Defense mage Spellcasting 2 180 18 25 'Magic villagers launch powerful violet bolts.' @{Defender='mage';Cost=@{Emeralds=180;Wood=18;Stone=25;Iron=5}}
Prefab iron_foundry 'Iron guardian foundry' Defense iron_foundry 'Golem forging' 2 160 12 30 'Every three days, smiths can awaken a golem. Maximum two per foundry.' @{Defender='iron_golem';Input=@{Iron=12;Tools=2};Health=220}
Prefab snow_shrine 'Snow guardian shrine' Defense snow_shrine 'Snow crafting' 1 90 10 12 'Every three days, a keeper can create a snow golem. Maximum two.' @{Defender='snow_golem';Input=@{Food=3;Wood=2}}
Prefab wall 'Stone wall' Defense wall '' 0 12 0 8 'Blocks ground raiders. Rotate to join a perimeter.' @{NeedsPath=$false;BlocksRaiders=$true;Health=300}
Prefab gate 'Village gate' Defense gate '' 0 35 8 12 'Carries connected paths while barring raiders until destroyed.' @{NeedsPath=$false;Path=$true;BlocksRaiders=$true;Health=240}
Prefab watchtower 'Archer watchtower' Defense tower Archery 2 145 18 30 'A fortified post for village archers.' @{Defender='archer';Health=320}
$units=[ordered]@{}
$workerModels=@{Farming='farmer';Woodcutting='woodcutter';Stonecutting='miner';Forging='smith_worker';Shepherding='herder';'Pig keeping'='herder';'Poultry keeping'='herder';'Warrior training'='warrior';Archery='archer';Spellcasting='mage_worker';'Golem forging'='smith_worker';'Snow crafting'='herder'}
foreach($p in $prefabs){$p.WorkerModel=if($workerModels.ContainsKey($p.Job)){$workerModels[$p.Job]}else{'villager'};if($p.Defender -in @('iron_golem','snow_golem')){$p.CreatesGuardian=$true}}
$prefabs[0].Category='Roads'
$roadTextures=@{cobble_road='cobblestone';gravel_road='gravel';plank_road='oak_planks';ice_road='packed_ice';soul_road='soul_sand'}
foreach($id in $roadTextures.Keys){$models[$id]=@(@{Position=@(0,.035,0);Size=@(1,.07,1);Color='#FFFFFF';Texture=('block/'+$roadTextures[$id]+'.png')})}
$materials=@{'#A9814D'='oak_planks';'#65492D'='oak_log';'#858784'='cobblestone';'#84532E'='oak_planks';'#6D4937'='spruce_planks';'#4D7F73'='prismarine';'#555A5C'='stone_bricks';'#6B7276'='stone_bricks';'#745381'='purple_wool';'#81734A'='dirt';'#624326'='farmland';'#696E6E'='stone';'#92968E'='iron_ore';'#969A93'='stone_bricks';'#B9A170'='dirt_path';'#A99163'='dirt_path';'#D0B984'='dirt_path';'#84A357'='grass_block_top';'#C2A574'='oak_log_top';'#9BC6CC'='glass';'#6B4829'='oak_door_bottom';'#B68B52'='oak_planks';'#468FB5'='water_still';'#468243'='oak_leaves';'#5D9848'='oak_leaves';'#C7AB41'='wheat_stage7';'#E3C55A'='wheat_stage7';'#454646'='furnace_front_on';'#F99637'='lava_still';'#343E43'='anvil';'#D5D1B9'='iron_block'}
$materials['#397144']='oak_leaves';$materials['#4F8F42']='oak_leaves'
$skins=@{villager='villager/villager';farmer='villager/farmer';woodcutter='villager/farmer';miner='villager/smith';smith_worker='villager/smith';herder='villager/butcher';warrior='villager/smith';archer='villager/farmer';mage_worker='villager/priest';zombie='zombie/zombie';piglin='pig/pig';sheep='sheep/sheep';pig='pig/pig';chicken='chicken';iron_golem='iron_golem/iron_golem';snow_golem='snow_golem';blaze='blaze'}
foreach($model in $models.Keys){
    $parts=$models[$model]
    if($skins.ContainsKey($model)){
        for($i=0;$i -lt $parts.Count;$i++){
            $p=$parts[$i]
            if($model -in @('villager','farmer','woodcutter','miner','smith_worker','herder','warrior','archer','mage_worker','zombie','piglin')){
                if($i -gt 5){continue}
                $p.Texture='entity/'+$skins[$model]+'.png';$p.Color='#FFFFFF'
                switch($i){0{$p.UvOrigin=@(0,22);$p.UvSize=@(4,12,4)}1{$p.UvOrigin=@(0,22);$p.UvSize=@(4,12,4)}2{$p.UvOrigin=@(16,20);$p.UvSize=@(8,12,6)}3{$p.UvOrigin=@(0,0);$p.UvSize=@(8,10,8)}4{$p.UvOrigin=@(24,0);$p.UvSize=@(2,4,2)}5{$p.UvOrigin=@(44,22);$p.UvSize=@(4,8,4)}}
                if($model -eq 'zombie'){if($i -eq 3){$p.UvSize=@(8,8,8)};if($i -lt 2){$p.UvOrigin=@(0,16)};if($i -eq 2){$p.UvOrigin=@(16,16);$p.UvSize=@(8,12,4)}}
            }else{
                $p.Texture='entity/'+$skins[$model]+'.png';$p.Color='#FFFFFF';$p.UvOrigin=@(0,0);$p.UvSize=@(8,8,8)
                if($model -in @('sheep','pig')){if($i -eq 0){$p.UvOrigin=@(28,8);$p.UvSize=@(8,16,6)}elseif($i -gt 1){$p.UvOrigin=@(0,16);$p.UvSize=@(4,6,4)}}
                if($model -eq 'sheep' -and $i -eq 0){$p.Texture='entity/sheep/sheep_fur.png'}
                if($model -eq 'iron_golem'){if($i -eq 0){$p.UvOrigin=@(0,40);$p.UvSize=@(18,12,11)}elseif($i -eq 1){$p.UvSize=@(8,10,8)}elseif($i -in @(2,3)){$p.UvOrigin=@(60,21);$p.UvSize=@(4,30,6)}else{$p.UvOrigin=@(37,0);$p.UvSize=@(6,16,5)}}
                if($model -eq 'blaze' -and $i -gt 0){$p.UvOrigin=@(0,16);$p.UvSize=@(2,8,2)}
                if($model -eq 'chicken'){
                    switch($i){0{$p.UvOrigin=@(0,9);$p.UvSize=@(6,8,6)}1{$p.UvOrigin=@(0,0);$p.UvSize=@(4,6,3)}2{$p.UvOrigin=@(14,0);$p.UvSize=@(4,2,2)}3{$p.UvOrigin=@(14,4);$p.UvSize=@(2,2,2)}{$_ -in 4,5}{$p.UvOrigin=@(26,0);$p.UvSize=@(3,5,3)}{$_ -in 6,7}{$p.UvOrigin=@(24,13);$p.UvSize=@(1,4,6)}}
                }
                if($model -eq 'snow_golem'){if($i -eq 0){$p.UvOrigin=@(0,36);$p.UvSize=@(12,12,12)}elseif($i -eq 1){$p.UvOrigin=@(0,16);$p.UvSize=@(10,10,10)}elseif($i -eq 2){$p.Texture='block/carved_pumpkin.png';$p.Remove('UvOrigin');$p.Remove('UvSize')}else{$p.Remove('Texture');$p.Remove('UvOrigin');$p.Remove('UvSize')}}
            }
        }
    }else{foreach($p in $parts){if($materials.ContainsKey($p.Color)){$p.Texture='block/'+$materials[$p.Color]+'.png'}}}
}
function Unit($id,$model,$hp,$damage,$range,$speed,$cooldown){$units[$id]=@{Model=$model;Health=$hp;Damage=$damage;Range=$range;Speed=$speed;Cooldown=$cooldown}}
Unit warrior warrior 50 14 .9 .9 .8
Unit archer archer 50 9 4 .8 1
Unit mage mage_worker 50 20 3.5 .7 1.5
Unit iron_golem iron_golem 250 30 1 .65 1.2
Unit snow_golem snow_golem 65 5 4 .7 .6
Unit zombie zombie 55 6 .7 .27 1.3
Unit piglin piglin 75 9 .8 .36 1
Unit blaze blaze 45 7 3 .25 1.7
$out=Join-Path $PSScriptRoot '../VillageCraft/Content'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$models.cave=@((Part 0 .018 0 .94 .035 .94 '#282C2D'),(Part -.36 .25 -.1 .22 .5 .8 '#969A93'),(Part .36 .25 -.1 .22 .5 .8 '#969A93'),(Part 0 .28 -.4 .6 .56 .15 '#696E6E'),(Part 0 .59 -.1 .94 .18 .82 '#858784'),(Part 0 .3 .25 .44 .55 .025 '#111517'),(Part -.32 .3 .38 .06 .6 .06 '#65492D'),(Part .32 .3 .38 .06 .6 .06 '#65492D'),(Part 0 .6 .38 .7 .08 .08 '#65492D'),(Part -.27 .42 .43 .06 .08 .05 '#FFCE6B'))
foreach($p in $models.cave){if($materials.ContainsKey($p.Color)){$p.Texture='block/'+$materials[$p.Color]+'.png'}}
$models.explorer=@($models.miner)+@((Part -.2 .45 .12 .04 .4 .04 '#65492D'),(Part -.2 .69 .12 .09 .09 .09 '#FFCE6B'))
Prefab cave 'Cave entrance' Production cave Expedition 4 180 16 32 'Miners and explorers search underground for gold, diamonds and ancient debris. Citizens can assist at half efficiency.' @{Professions=@('miner','explorer');WorkerModel='miner';Input=@{Food=.25;Tools=.1};Output=@{Iron=1;Gold=.4;Diamonds=.15;AncientDebris=.04};Cost=@{Emeralds=180;Wood=16;Stone=32;Tools=4};Health=240}
foreach($p in $prefabs){if($p.Id -eq 'forge'){$p.UpgradeStation=$true}}
function TomlValue($v){
    if($null -eq $v){return '[]'}
    if($v -is [string]){return '"'+$v.Replace('\','\\').Replace('"','\"').Replace("`r",'').Replace("`n",'\n')+'"'}
    if($v -is [bool]){return $v.ToString().ToLowerInvariant()}
    if($v -is [System.Collections.IDictionary]){return '{ '+(($v.Keys | Sort-Object | ForEach-Object { (TomlValue ([string]$_))+' = '+(TomlValue $v[$_]) }) -join ', ')+' }'}
    if($v -is [System.Collections.IEnumerable]){return '['+(($v | ForEach-Object {TomlValue $_}) -join ', ')+']'}
    return ([System.IFormattable]$v).ToString($null,[Globalization.CultureInfo]::InvariantCulture)
}
function WriteToml($file,$values){
    if($values.Contains('FormatVersion')){$values['Categories']=@('Roads','Housing','Production','Infrastructure','Economy','Animals','Defense')}
    $lines=@('# VillageCraft content. Read once at startup; restart after editing.')
    foreach($key in $values.Keys){if($key -notin @('Starter','Upgrades')){$lines+=([string]$key)+' = '+(TomlValue $values[$key])}}
    foreach($key in @('Starter','Upgrades')){if($values.Contains($key)){foreach($entry in $values[$key]){$lines+="`n[["+$key+']]';foreach($property in $entry.Keys | Sort-Object){$lines+=$property+' = '+(TomlValue $entry[$property])}}}}
    $lines | Set-Content -LiteralPath $file -Encoding utf8
}
foreach($folder in @('Prefabs','Models','Units','Professions')){New-Item -ItemType Directory -Force -Path (Join-Path $out $folder) | Out-Null}
$defaults=[ordered]@{Id='';Name='';Category='';Description='';Model='';Job='';WorkerModel='villager';Jobs=0;Homes=0;Water=0;Joy=0;Upkeep=0;Health=120;Path=$false;NeedsPath=$true;BlocksRaiders=$false;Cost=@{};Input=@{};Output=@{};Animal='';Defender='';CreatesGuardian=$false;MoveSpeed=1;JobSpeed=@{};Professions=@();UpgradeStation=$false}
foreach($p in $prefabs){$complete=[ordered]@{};foreach($key in $defaults.Keys){$complete[$key]=if($p.ContainsKey($key)){$p[$key]}else{$defaults[$key]}};WriteToml (Join-Path $out ('Prefabs/'+$p.Id+'.toml')) $complete}
foreach($id in $models.Keys){
    $file=Join-Path $out ('Models/'+$id+'.toml');@('# Model geometry in tile units. UV coordinates are in pixels.',('Id = '+(TomlValue $id))) | Set-Content -LiteralPath $file -Encoding utf8
    foreach($p in $models[$id]){
        Add-Content -LiteralPath $file -Value "`n[[Parts]]"
        foreach($key in @('Position','Size','Color','Texture','UvOrigin','UvSize')){$value=if($p.ContainsKey($key)){$p[$key]}elseif($key -eq 'Texture'){''}else{@()};Add-Content -LiteralPath $file -Value ($key+' = '+(TomlValue $value))}
    }
}
foreach($name in @('zombie','piglin','blaze')){$units[$name].Speed=@{zombie=.9;piglin=.95;blaze=.8}[$name]}
foreach($id in $units.Keys){$unit=[ordered]@{Id=$id};foreach($key in @('Model','Health','Damage','Range','Speed','Cooldown')){$unit[$key]=$units[$id][$key]};WriteToml (Join-Path $out ('Units/'+$id+'.toml')) $unit}
$professionData=@(
    @('citizen','Citizen','villager',@(),.5,@{}),@('farmer','Farmer','farmer',@('Farming'),1,@{}),@('woodcutter','Woodcutter','woodcutter',@('Woodcutting'),1,@{}),
    @('miner','Miner','miner',@('Stonecutting','Expedition'),1,@{Iron=1.5;Gold=1.5}),@('explorer','Explorer','explorer',@('Expedition'),1,@{Diamonds=2;AncientDebris=2}),
    @('smith','Smith','smith_worker',@('Forging','Golem forging'),1,@{}),@('keeper','Animal keeper','herder',@('Shepherding','Pig keeping','Poultry keeping','Snow crafting'),1,@{}),
    @('trader','Trader','villager',@('Trading'),1,@{}),@('warrior','Warrior','warrior',@('Warrior training'),1,@{}),@('archer','Archer','archer',@('Archery'),1,@{}),@('mage','Magic villager','mage_worker',@('Spellcasting'),1,@{})
)
foreach($p in $professionData){WriteToml (Join-Path $out ('Professions/'+$p[0]+'.toml')) ([ordered]@{Id=$p[0];Name=$p[1];Model=$p[2];Jobs=$p[3];Efficiency=$p[4];TrainingCost=$(if($p[0] -eq 'citizen'){@{}}else{@{Emeralds=10;Tools=2}});ResourceBonus=$p[5]})}
$resourceData=@(@('Emeralds','item/emerald.png',30),@('Food','item/wheat.png',30),@('Wood','block/oak_log.png',30),@('Stone','block/cobblestone.png',30),@('Iron','item/iron_ingot.png',60),@('Tools','item/iron_pickaxe.png',60),@('Wool','block/white_wool.png',30),@('Eggs','item/egg.png',30),@('Gold','item/gold_ingot.png',120),@('Diamonds','item/diamond.png',240),@('AncientDebris','block/obsidian.png',360))
@('# Resource names, UI icons, and prices per twenty items.') | Set-Content -LiteralPath (Join-Path $out 'resources.toml')
foreach($r in $resourceData){Add-Content -LiteralPath (Join-Path $out 'resources.toml') -Value ("`n[[Resources]]`nId = "+(TomlValue $r[0])+"`nIcon = "+(TomlValue $r[1])+"`nBuyPrice = "+$r[2]+"`nSellPrice = "+($r[2]*.7).ToString([Globalization.CultureInfo]::InvariantCulture))}
$upgrades=@(
    @{Id='iron_armor';Name='Iron armor';Kind='Armor';Level=1;Effect=.15;Cost=@{Emeralds=80;Iron=16;Tools=4}},
    @{Id='diamond_armor';Name='Diamond armor';Kind='Armor';Level=2;Effect=.2;Cost=@{Emeralds=150;Diamonds=8;Gold=10}},
    @{Id='netherite_armor';Name='Netherite armor';Kind='Armor';Level=3;Effect=.25;Cost=@{Emeralds=250;AncientDebris=6;Gold=16;Diamonds=4}},
    @{Id='iron_tools';Name='Tempered tools';Kind='Tools';Level=1;Effect=.15;Cost=@{Emeralds=60;Iron=12;Tools=6}},
    @{Id='diamond_tools';Name='Diamond tools';Kind='Tools';Level=2;Effect=.25;Cost=@{Emeralds=120;Diamonds=6;Gold=6}},
    @{Id='netherite_tools';Name='Netherite tools';Kind='Tools';Level=3;Effect=.35;Cost=@{Emeralds=200;AncientDebris=4;Gold=12;Tools=8}}
)
WriteToml (Join-Path $out 'upgrades.toml') ([ordered]@{Upgrades=$upgrades})
$starter=[System.Collections.Generic.List[object]]::new()
for($x=-8;$x -le 7;$x++){$starter.Add(@{Prefab='path';X=$x;Z=0;Rotation=0;Industry=$false;Residents=@()})}
for($z=-9;$z -le 9;$z++){if($z -ne 0){$starter.Add(@{Prefab='path';X=0;Z=$z;Rotation=0;Industry=$false;Residents=@()})}}
$homes=@(@(-4,-1,0,@('farmer','woodcutter','citizen')),@(-2,1,2,@('miner','keeper','citizen')),@(2,-1,0,@('farmer','smith','explorer')),@(4,1,2,@('keeper','miner','keeper')))
foreach($h in $homes){$starter.Add(@{Prefab='cottage';X=$h[0];Z=$h[1];Rotation=$h[2];Industry=$false;Residents=$h[3]})}
foreach($p in @(@('farm',-7,-1,0),@('well',-6,1,2),@('lumber',-5,-1,0),@('quarry',-1,-7,3),@('sheep_pen',1,-7,1),@('pig_pen',1,-5,1),@('chicken_coop',-1,-5,3))){$starter.Add(@{Prefab=$p[0];X=$p[1];Z=$p[2];Rotation=$p[3];Industry=$true;Residents=@()})}
WriteToml (Join-Path $out 'game.toml') ([ordered]@{FormatVersion=1;DefaultMapWidth=32;DefaultMapHeight=32;GroundTexture='block/grass_block_top.png';GroundTint='#92B968';DirtTexture='block/dirt.png';MigrationThreshold=3;CitizenGrowthSupport=.2;ImmigrationProfessions=@('citizen','farmer','miner','citizen','explorer','keeper','smith','woodcutter','citizen','warrior','archer','mage');InitialStock=@{Emeralds=1100;Food=160;Wood=200;Stone=160;Iron=45;Tools=15;Wool=0;Eggs=0;Gold=0;Diamonds=0;AncientDebris=0};Rules=@{RaidsEnabled=$true;RaidInterval=30;WaveBase=6;WaveGrowth=3;MaxRaiders=100;MaxPopulation=256;AnimalsPerPen=8;GolemsPerWorkshop=2;DayLength=600;ProductionCycle=2};Starter=$starter.ToArray()})


# Preserve runtime script bindings when refreshing the original authoring templates.
Get-ChildItem (Join-Path $out 'Units') -Filter '*.toml' | ForEach-Object {
    if (-not (Select-String -LiteralPath $_.FullName -Pattern '^Behavior =')) {
        $behavior = if ($_.BaseName -eq 'blaze') { 'ranged' } else { 'melee' }
        Add-Content -LiteralPath $_.FullName -Value ('Behavior = "' + $behavior + '"')
    }
}
Add-Content -LiteralPath (Join-Path $out 'Prefabs/well.toml') -Value 'Script = "well"'
