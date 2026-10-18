/** VillageCraft live Jint API. Editor declarations only; no imports or CLR/Node APIs required. */
type ResourceName = 'Emeralds' | 'Food' | 'Wood' | 'Stone' | 'Iron' | 'Tools' | 'Wool' | 'Eggs' | 'Gold' | 'Diamonds' | 'AncientDebris' | 'Goods' | 'Leather' | 'Milk';
type TaskName = 'Resting' | 'DefendingVillage' | 'NightPatrol' | 'GoingHome' | 'SupportingFamilies' | 'Homeless' | 'ScriptedBehavior' | 'CollectingMilk' | 'CollectingFood' | 'WaitingStorage' | 'Delivering' | 'WaitingDeliveries' | 'CollectingDelivery' | 'WaitingTreeRegrowth' | 'GoingToTrees' | 'ChoppingTree' | 'GoingToWork' | 'Working' | 'Assisting' | 'CaravanTravelling' | 'TalkingToResidents' | 'LeavingVillage' | 'WanderingHomeless' | 'SeekingHome' | 'CollectingRepairSupplies' | 'RepairingBuilding';
type EntityFilter<T> = string | number | ((entity: T) => boolean);
type EffectName = 'Burning' | 'Poison' | 'Slowness' | 'Weakness';
interface Point { readonly x: number; readonly z: number; }
interface TaggedEntity extends Point {
    readonly id: number;
    readonly health: number;
    tags: string;
    tag: string;
    readonly behavior: string;
    hasTag(key: string): boolean;
    attachBehavior(script: string): boolean;
    remove(): boolean;
}
interface ScriptVillager extends TaggedEntity {
    /** BaseHealth of this villager's current profession. */
    readonly maximumHealth: number;
    readonly name: string;
    readonly profession: string;
    readonly home: ScriptBuilding | null;
    readonly workplace: ScriptBuilding | null;
    task: TaskName;
    /** Inspector-selected profession; automatic policies preserve it. */
    readonly manualProfession: boolean;
    hasWorked: boolean;
    resourceTarget: number;
    repairTarget: number;
    pickupSource: number;
    deliveryTarget: number;
    deliveryResource: ResourceName | '';
    readonly cargoCount: number;
    readonly hasEffects: boolean;
    readonly combatRange: number;
    inventory(resource: ResourceName): number;
    cargo(resource: ResourceName): number;
    effect(name: EffectName): number;
    applyEffect(name: EffectName, seconds: number): boolean;
    give(resource: ResourceName, amount: number): boolean;
    heal(amount: number): void;
    moveTo(x: number, z: number, dt: number): boolean;
    canReach(building: ScriptBuilding): boolean;
    canSee(x: number, z: number): boolean;
    /** Returns true after arrival; otherwise advances a validated route. dt must be <= 0.2. */
    walkTo(building: ScriptBuilding, dt: number, roadsOnly?: boolean): boolean;
    collect(building: ScriptBuilding, resource: ResourceName, amount: number, dt: number): boolean;
    pickup(source: ScriptBuilding, destination: ScriptBuilding, resource: ResourceName): boolean;
    deliver(destination: ScriptBuilding): boolean;
    work(dt: number): boolean;
    chop(tree: ScriptBuilding, dt: number): boolean;
    drinkMilk(): boolean;
    patrol(x: number, z: number, dt: number): boolean;
    attackMonster(id: number): boolean;
    /** Uses carried repair materials and work time; does not spend remote treasury resources. */
    repair(building: ScriptBuilding, dt: number): boolean;
    /** Changes profession, preserving health percentage; does not automatically reassign jobs. */
    setProfession(profession: string): boolean;
}
interface ScriptBuilding extends TaggedEntity {
    readonly prefab: string;
    readonly maximumHealth: number;
    enabled: boolean;
    readonly connected: boolean;
    readonly path: boolean;
    readonly natural: boolean;
    readonly indestructible: boolean;
    readonly blocksRaiders: boolean;
    readonly warehouse: boolean;
    readonly priority: number;
    readonly job: string;
    readonly jobs: number;
    readonly homes: number;
    readonly pendingBirths: number;
    readonly allowedProfessions: string[];
    readonly animal: string;
    readonly adultAnimals: number;
    readonly defender: string;
    readonly createsGuardian: boolean;
    readonly harvestSource: string;
    readonly harvestRadius: number;
    readonly growthDays: number;
    readonly worked: boolean;
    readonly efficiency: number;
    serviceCredit: number;
    readonly storageCapacity: number;
    readonly storageFree: number;
    readonly requestedResource: ResourceName | '';
    readonly inputs: ResourceName[];
    readonly outputs: ResourceName[];
    readonly repairInputs: ResourceName[];
    storage(resource: ResourceName): number;
    availableSupply(resource: ResourceName): number;
    inputAmount(resource: ResourceName): number;
    inputTarget(resource: ResourceName): number;
    repairCost(resource: ResourceName): number;
    canReach(other: ScriptBuilding): boolean;
    qualified(villager: ScriptVillager): boolean;
    /** Applies local TOML recipe atomically; worker must be present and assigned. */
    produce(worker: ScriptVillager): boolean;
    rewardWork(worker: ScriptVillager): boolean;
    breed(): boolean;
    collectAnimalProduct(resource: ResourceName, amount: number): boolean;
    createGuardian(): boolean;
    grow(): void;
    /** Direct mod healing; automatic worker repairs use villager.repair instead. */
    repair(amount: number): void;
}
interface ScriptMonster extends TaggedEntity {
    readonly type: string;
    readonly home: ScriptBuilding | null;
    readonly age: number;
    readonly leatherHelmet: boolean;
    readonly burningInSun: boolean;
    readonly bossWave: boolean;
    state: number;
}
interface ScriptGame {
    readonly day: number;
    readonly width: number;
    readonly height: number;
    readonly timeOfDay: number;
    readonly night: boolean;
    readonly population: number;
    readonly gameOver: boolean;
    stock(resource: ResourceName): number;
    addResource(resource: ResourceName, amount: number): void;
    takeResource(resource: ResourceName, amount: number): boolean;
    villager(id: number): ScriptVillager | null;
    villagers(): ScriptVillager[];
    building(id: number): ScriptBuilding | null;
    buildings(): ScriptBuilding[];
    spawnMob(type: string, x: number, z: number): boolean;
    /** Always writes to the run log; short messages also enter the village journal. */
    log(message: string): void;
    warn(message: string): void;
    error(message: string): void;
    translate(key: string): string;
    format(key: string, arguments: unknown[]): string;
}
interface ScriptWorld extends ScriptGame {
    readonly anchorX: number;
    readonly anchorZ: number;
    readonly elapsedDay: number;
    readonly birthChance: number;
    readonly pendingBirths: number;
    getVillagers(filter?: EntityFilter<ScriptVillager>): ScriptVillager[];
    getMonsters(filter?: EntityFilter<ScriptMonster>): ScriptMonster[];
    getGuardians(filter?: EntityFilter<ScriptMonster>): ScriptMonster[];
    getAnimals(filter?: EntityFilter<ScriptMonster>): ScriptMonster[];
    getBuildings(filter?: EntityFilter<ScriptBuilding>): ScriptBuilding[];
    /** Compatibility alias for getBuildings. */
    gerBuildings(filter?: EntityFilter<ScriptBuilding>): ScriptBuilding[];
    getStores(): ScriptBuilding[];
    getWorkplaces(): ScriptBuilding[];
    getDamagedBuildings(): ScriptBuilding[];
    getAttackableBuildings(): ScriptBuilding[];
    getHarvestSources(prefab: string, x: number, z: number, radius: number): ScriptBuilding[];
    readonly professionPrioritiesActive: boolean;
    getProfessions(): { readonly id: string; readonly jobs: string[]; readonly priority: number }[];
    roads(): Point[];
    resourceNames(): ResourceName[];
    assignJobs(villagers: number[], workplaces: number[]): boolean;
    queueBirth(homeId: number): boolean;
    queueSpawnMonster(type: string, x: number, z: number, tags?: string): boolean;
    queueSpawnBuilding(prefab: string, x: number, z: number, rotation?: number, tags?: string): boolean;
    queueSpawnVillager(profession: string, home: number, tags?: string): boolean;
    queueDelete(entity: TaggedEntity): boolean;
    queueDeleteMonster(id: number): boolean;
    queueDeleteVillager(id: number): boolean;
    queueDeleteBuilding(id: number): boolean;
    queueDamageMonster(id: number, amount: number): boolean;
    queueDamageVillager(id: number, amount: number): boolean;
    queueDamageBuilding(id: number, amount: number): boolean;
}
interface MobScriptContext extends Point {
    readonly self: ScriptMonster;
    readonly game: ScriptGame;
    readonly hook: 'tick';
    readonly type: string;
    readonly health: number;
    state: number;
    readonly dt: number;
    readonly distance: number;
    readonly range: number;
    readonly visible: boolean;
    readonly cooldown: number;
    readonly night: boolean;
    readonly targetVillager: ScriptVillager | null;
    readonly targetBuilding: ScriptBuilding | null;
    readonly targetGuardian: ScriptMonster | null;
    selectTarget(kind: 'villager' | 'building' | 'guardian' | 'monster', id: number): boolean;
    /** Check a walkable route without moving. */
    canReach(x: number, z: number): boolean;
    canSee(x: number, z: number): boolean;
    moveTo(x: number, z: number): boolean;
    move(): void;
    attack(): void;
    explode(): void;
    attackVillager(id: number): void;
    attackBuilding(id: number): void;
    attackGuardian(id: number): void;
    attackMonster(id: number): void;
}
declare const world: ScriptWorld;
declare const resources: Readonly<Record<ResourceName, ResourceName>>;
declare const tasks: Readonly<Record<TaskName, TaskName>>;
declare const eST_Mod: 0;
declare const eST_AI: 1;
declare const eST_Building: 2;
declare function set_script_type(type: 0 | 1 | 2, target?: string): void;
declare const ai: {
    distance(a: Point, b: Point): number;
    nearest<T extends Point>(origin: Point, list: T[], accept?: (item: T) => boolean): T | null;
    supply(villager: ScriptVillager, resource: ResourceName, exclude?: number): ScriptBuilding | null;
    chooseHostileTarget(ctx: MobScriptContext): boolean;
};
/** All callbacks are optional. Define only those your component needs. */
declare function onInit(): void;
declare function onUpdate(dt: number): void;
declare function onShutdown(): void;
declare function onWorldLoaded(): void;
declare function onDayStarted(): void;
declare function onNightStarted(): void;
declare function onDawn(): void;
declare function onAssignJobs(): void;
declare function onReassignProfessions(): void;
declare function onMonsterUpdate(ctx: MobScriptContext, dt: number): void;
declare function onGuardianUpdate(ctx: MobScriptContext, dt: number): void;
declare function onAnimalUpdate(ctx: MobScriptContext, dt: number): void;
declare function onVillagerUpdate(villager: ScriptVillager, dt: number): void;
declare function onBuildingUpdate(building: ScriptBuilding, dt: number): void;
declare function onBuildingProduce(building: ScriptBuilding, worker: ScriptVillager): boolean;
declare function onBuildingWork(building: ScriptBuilding, worker: ScriptVillager): void;
declare function onBuildingDay(building: ScriptBuilding): void;
declare function onBuildingRepaired(building: ScriptBuilding, amount: number, worker: ScriptVillager): void;
declare function onBabyBorn(home: ScriptBuilding): void;
declare function onVillagerBorn(villager: ScriptVillager): void;
declare function onMonsterSpawned(monster: ScriptMonster): void;
declare function onMonsterAttached(monster: ScriptMonster): void;
declare function onMonsterDamaged(monster: ScriptMonster, damage: number, source: TaggedEntity | null): void;
declare function onMonsterRemoved(monster: ScriptMonster, reason: string): void;
declare function onVillagerSpawned(villager: ScriptVillager): void;
declare function onVillagerAttached(villager: ScriptVillager): void;
declare function onVillagerProfessionChanged(villager: ScriptVillager): void;
declare function onVillagerDamaged(villager: ScriptVillager, damage: number, source: TaggedEntity | null): void;
declare function onVillagerRemoved(villager: ScriptVillager, reason: string): void;
declare function onBuildingPlaced(building: ScriptBuilding): void;
declare function onBuildingAttached(building: ScriptBuilding): void;
declare function onBuildingChanged(building: ScriptBuilding): void;
declare function onBuildingDamaged(building: ScriptBuilding, damage: number, source: TaggedEntity | null): void;
declare function onBuildingRemoved(building: ScriptBuilding, reason: string): void;
declare function onGuardianAttached(guardian: ScriptMonster): void;
declare function onGuardianSpawned(guardian: ScriptMonster): void;
declare function onGuardianRemoved(guardian: ScriptMonster, reason: string): void;
declare function onAnimalAttached(animal: ScriptMonster): void;
declare function onAnimalSpawned(animal: ScriptMonster): void;
declare function onAnimalRemoved(animal: ScriptMonster, reason: string): void;
declare function onCommandFailed(reason: string): void;
declare function onGoodsConverted(emeralds: number): void;
declare function onResourceCollected(villager: ScriptVillager, amount: number, source: ScriptBuilding): void;
declare function onDeliveryCompleted(villager: ScriptVillager, destination: ScriptBuilding): void;
declare function onTreeHarvested(tree: ScriptBuilding, villager: ScriptVillager): void;
declare function onArmorEnchanted(): void;
declare function onGameOver(): void;

/** Boss wave number; emitted once after the wave is spawned. */
declare function onBossWaveStarted(wave: number): void;
declare function onGuardianDamaged(guardian: ScriptMonster, damage: number, source: TaggedEntity | null): void;
declare function onExplosion(monster: ScriptMonster): void;

declare function onMonsterAttack(monster: ScriptMonster, target: TaggedEntity): void;
declare function onGuardianAttack(guardian: ScriptMonster, target: TaggedEntity): void;
declare function onVillagerAttack(villager: ScriptVillager, target: ScriptMonster): void;
declare function onMonsterPrimed(monster: ScriptMonster): void;
