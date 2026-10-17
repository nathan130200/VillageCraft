# Componentes JavaScript: ciclo de vida, eventos e world

O componente independente ScriptRuntime mantém um engine Jint por arquivo por mundo. Não há estado JS compartilhado entre vilas, nem serialização JSON na ponte. Os arquivos Scripts/*.js são carregados automaticamente, uma vez. Reinicie após editar.

## Tipo do script: primeira chamada vence

Declare o tipo no início do arquivo:

~~~js
set_script_type(eST_AI, "blaze");          // substitui a IA dos blazes
// ou:
set_script_type(eST_Building, "well");     // lógica dos poços
// ou:
set_script_type(eST_Mod);                 // mod global
~~~

Use somente uma dessas linhas por arquivo. A primeira chamada de set_script_type determina o tipo; chamadas seguintes são ignoradas. Sem chamada, ou com tipo desconhecido, o arquivo é um mod. A declaração deve ocorrer no corpo inicial do arquivo: chamadas dentro de callbacks não alteram o tipo.

O alvo é opcional. Uma IA sem alvo serve como comportamento padrão referenciado por Units/*.toml (Behavior) ou anexado a uma instância. Um script de construção sem alvo pode ser referenciado por Prefabs/*.toml (Script) ou anexado a uma instância. Um alvo explícito substitui o padrão dos TOMLs. Duas substituições explícitas para o mesmo tipo/alvo são rejeitadas com diagnóstico, sem depender da ordem dos arquivos.

Alvos de IA aceitos: ID de unidade (ex.: "blaze"), ID de profissão (ex.: "farmer") ou "villager" para todos os aldeões. Alvos de construção são IDs de prefab. Uma profissão específica tem preferência sobre "villager". Uma ligação por instância tem preferência sobre os dois.

## Ciclo de vida: todos os tipos

~~~js
function onInit() {
    // world já está disponível; consulte objetos existentes ou enfileire comandos.
}
function onUpdate(dt) {
    // Uma chamada por passo da simulação, não uma chamada por entidade.
}
function onShutdown() {
    // Uma chamada ao sair/substituir o mundo. Comandos novos não são aceitos aqui.
}
~~~

As funções são opcionais. onInit ocorre quando o componente se conecta ao mundo; onShutdown ocorre uma vez ao sair ou carregar/criar outra vila. Salvar não reinicia scripts. Quando pausado ou com o menu aberto não há onUpdate. dt é tempo de simulação em segundos, já multiplicado pela velocidade do jogo; passos grandes são subdivididos em até 0,2 s.

O global world é uma API viva da vila. Não guarde referências de outra vila entre sessões. Variáveis globais JS pertencem àquele arquivo/mundo; o arquivo de IA é compartilhado pelas suas instâncias. Para estado individual persistente, use a propriedade state do monstro (número) e tags das instâncias. Variáveis JS globais não são gravadas nos saves.

## Eventos de mundo

Mods recebem os eventos abaixo. Scripts de IA/construção recebem somente os eventos das instâncias que eles controlam.

| Callback | Argumentos |
|---|---|
| onWorldLoaded() | Apenas mods; depois de onInit, com objetos do mundo já acessíveis |
| onMonsterSpawned(monster) | Monstro efetivamente criado |
| onMonsterAttached(monster) | Monstro existente ao conectar o componente, ou ao anexar comportamento |
| onMonsterDamaged(monster, damage, source) | Dano efetivamente aplicado; source pode ser null |
| onMonsterRemoved(monster, reason) | reason: "killed", "dawn" ou "deleted" |
| onVillagerSpawned(villager) | Novo morador por imigração/API |
| onVillagerAttached(villager) | Morador existente ou comportamento anexado |
| onVillagerProfessionChanged(villager) | Profissão alterada por treinamento |
| onVillagerDamaged(villager, damage, source) | Dano aplicado |
| onVillagerRemoved(villager, reason) | "killed" ou "deleted" |
| onBuildingPlaced(building) | Nova construção colocada com sucesso |
| onBuildingAttached(building) | Construção existente ou comportamento anexado |
| onBuildingChanged(building) | Caminho repavimentado |
| onBuildingDamaged(building, damage, source) | Dano aplicado |
| onBuildingRemoved(building, reason) | "destroyed", "demolished" ou "deleted" |
| onBuildingWork(building, worker) | Ciclo produtivo bem-sucedido; depois de pagar Input |
| onBuildingDay(building) | Amanhecer em construção ativada |
| onDayStarted() | Mods; amanhecer |
| onNightStarted() | Mods; início da noite |
| onCommandFailed(reason) | Mods; comando aceito na fila não pôde ser executado |

Ao carregar um save, não são emitidos eventos de criação fictícios: use onWorldLoaded ou on...Attached para descobrir objetos existentes. Os argumentos são referências vivas, não cópias. Damage registra a quantidade daquele impacto; a vida consultada pode já refletir outros impactos. O objeto de um evento de remoção ainda pode ser lido, mas não faz mais parte das consultas do mundo.

Callbacks de eventos e comandos ficam em uma fila: não há reentrada de JS durante uma alteração da simulação. A fila tem até 2048 itens e cada drenagem processa até 256; o restante continua no próximo passo. As filas não são persistidas nos saves.

## Consultas por função, chave ou ID

~~~js
const bosses = world.getMonsters("boss"); // tag OU tipo
const blazes = world.getMonsters("type:blaze");
const wave = world.getMonsters("tag:wave90");
const hurt = world.getVillagers(v => v.health < 30);
const houses = world.getBuildings(b => b.prefab === "cottage");
const specific = world.getBuildings("id:42");
const numeric = world.getMonsters(42);
const all = world.getBuildings(); // também aceita "*"
~~~

getMonsters, getVillagers e getBuildings retornam arrays de vínculos vivos, com a lista de membros capturada no momento da consulta. Os filtros recebem cada objeto. Uma chave simples combina ID numérico, tag ou tipo/profissão/prefab, com comparação sensível a maiúsculas. Prefixos: tag:, id:, type:, profession:, prefab:. gerBuildings é um alias de getBuildings, compatível com a grafia do pedido original.

## Identidade e tags de instância

Monstros, aldeões e construções possuem id estável, tags (string), tag (alias), hasTag(key), behavior (arquivo anexado), attachBehavior(scriptId) e remove().

~~~js
const villageGuards = world.getVillagers("warrior");
for (const guard of villageGuards) {
    guard.tags = "guard north_gate"; // tags separadas por espaço, vírgula ou ponto e vírgula
}
world.getMonsters("boss")[0]?.attachBehavior("my_boss");
~~~

Tags têm até 256 caracteres sem caracteres de controle. São dados da instância, não do prefab. IDs, tags e comportamento anexado são gravados no save binário v6. Saves v3/v4/v5 continuam legíveis e ganham IDs/tags padrão. attachBehavior enfileira a ligação; string vazia restaura o comportamento por alvo/TOML.

## Criação, dano e remoção enfileirados

~~~js
function onInit() {
    world.queueSpawnMonster("blaze", 10, 12, "boss wave90");
    world.queueSpawnBuilding("well", 9, 15, 0, "service north");
}
function onMonsterSpawned(monster) {
    if (monster.hasTag("wave90")) world.log("Onda do Nether!");
}
function onBuildingDamaged(building, damage, source) {
    if (building.health < 20) world.queueDelete(building);
}
~~~

| Método | Efeito |
|---|---|
| queueSpawnMonster(type, x, z, tags="") | Cria unidade conhecida dentro do mapa e do limite de inimigos |
| queueSpawnBuilding(prefab, x, z, rotation=0, tags="") | Segue regras normais: custo, terreno livre e entrada conectada |
| queueSpawnVillager(profession, homeId, tags="") | Exige casa disponível e limite de população |
| queueDelete(entity) | Remove uma entidade obtida pelas consultas/callbacks |
| queueDeleteMonster(id), queueDeleteVillager(id), queueDeleteBuilding(id) | Remove por ID |
| queueDamageMonster(id, amount), queueDamageVillager(id, amount), queueDamageBuilding(id, amount) | Aplica dano de script e emite eventos; quantidade de 0 a 10000 |

Os métodos retornam se a solicitação foi aceita na fila, não o objeto futuro. Consultas imediatamente após enfileirar ainda não veem o objeto; use on...Spawned/Placed. Limites e regras são reavaliados na execução. Remoção por script é intencional, inclusive de construções ocupadas: moradores são realocados ou ficam sem casa. A remoção por API não concede recompensa de combate.

## IA de monstros

~~~js
set_script_type(eST_AI, "blaze");
function onMonsterSpawned(monster) { monster.tags = "nether patrol"; }
function onMonsterUpdate(ctx, dt) {
    if (ctx.visible && ctx.distance <= ctx.range) ctx.attack();
    else ctx.move();
}
~~~

onMonsterUpdate recebe contexto de combate e dt. Propriedades: self (vínculo do monstro), type, x, z, health, state (gravável, 0..3600), distance, range, visible, cooldown, targetVillager e targetBuilding. ctx.game continua como alias compatível das APIs básicas; prefira world.

Métodos: move(), attack(), explode(), moveTo(x,z), attackVillager(id), attackBuilding(id). Apenas uma ação por chamada. O jogo valida velocidade, pathfinding, alcance, linha de visão, cooldown e proteção. moveTo não teleporta. explode aplica dano em raio de dois terrenos e remove o monstro. Para selecionar seus próprios alvos:

~~~js
function onMonsterUpdate(ctx, dt) {
    const people = world.getVillagers(v => v.health > 0);
    let chosen = null, distance = Infinity;
    for (const v of people) {
        const d = (v.x-ctx.x)**2 + (v.z-ctx.z)**2;
        if (d < distance) { chosen = v; distance = d; }
    }
    if (!chosen) { ctx.move(); return; }
    if (distance <= ctx.range*ctx.range) ctx.attackVillager(chosen.id);
    else ctx.moveTo(chosen.x, chosen.z);
}
~~~

Se o comportamento de um monstro estiver ausente ou falhar, ele usa combate básico para continuar se movimentando. Os scripts antigos function update(ctx) devem migrar para onMonsterUpdate e declarar o tipo.

## IA de aldeões

~~~js
set_script_type(eST_AI, "citizen");
function onVillagerUpdate(villager, dt) {
    villager.moveTo(16, 16, dt);
}
~~~

Substitui o trabalho/movimento normal dos aldeões correspondentes. O autor controla o que eles fazem; use world e eventos para tarefas personalizadas. Não use um override vazio se deseja manter as tarefas padrão. A escolha "villager" abrange todas as profissões.

## Construções

~~~js
set_script_type(eST_Building, "well");
function onBuildingDay(building) { building.repair(2); }
function onBuildingWork(building, worker) {
    if (world.takeResource("Food", 1)) world.addResource("Gold", 1);
    worker.give("Food", 1);
}
function onBuildingUpdate(building, dt) {
    // Uma chamada por construção ativada a cada passo.
}
~~~

onBuildingUpdate é adicional ao onUpdate global do arquivo. A produção TOML continua funcionando sem JS.

## Métodos dos vínculos e APIs de economia

ScriptBuilding: id, prefab, x, z, health, maximumHealth; enabled é gravável e redistribui empregos. repair(amount) cura até 25 de vida, limitado ao máximo.

ScriptVillager: id, name, profession, x, z, health. inventory(resource) consulta itens; give(resource, amount) transfere do estoque real, respeitando capacidade de 32; heal(amount) cura até 25 sem ultrapassar 50; moveTo(x,z,dt) move com pathfinding.

ScriptMonster: id, type, x, z, health, state, tags e ligações de comportamento. As ações de combate são acessadas pelo contexto onMonsterUpdate.

world expõe day, night, width, height, timeOfDay (0..1), population, stock(resource), addResource(resource, amount), takeResource(resource, amount), villager(id), building(id) e log(message). Produção/consumo direto: até 100 por chamada, IDs de recursos conhecidos; log aceita até 256 caracteres.

Os scripts não têm importação de CLR, Node ou acesso ao save. Cada execução possui limites de 10000 instruções, 32 MB de alocação e 250 ms (inclui primeiro acesso às APIs nativas). Uma falha desativa aquele componente até reiniciar e registra o arquivo/motivo no console; outros scripts continuam.
`onTreeHarvested(tree, villager)` fires after harvesting a tree and starting its regrowth timer.

## 1.0.5-beta

- `onGoodsConverted(emeralds)` fires once at the 06:00 settlement; `onDawn()` follows.
- `onResourceCollected(villager, amount, sourceBuilding)` and `onDeliveryCompleted(villager, destinationBuilding)` report physical collection/delivery.
- `onTorchChanged(building)` reports a fixture change; `building.torchLit` reads the state, `building.setTorch(lit)` changes it, and `building.storage(resource)` / `storageCapacity` inspect local storage.
- `villager.cargo(resource)` reads in-transit delivery items. `villager.effect(name)` reads remaining seconds; `applyEffect(name, seconds)` supports Burning, Poison, Slowness and Weakness. Milk clears all effects.
- `onArmorEnchanted()` reports the city-wide equipment enchantment.
- `world.stock(resource)` counts stored resources, excluding personal inventories and cargo. Explicit `addResource` mod grants enter warehouse storage. `villager.give` only transfers from a reachable nearby store when the villager is physically at it. Emeralds and Goods cannot be carried.

`onGameOver()` reports the elimination of the final villager. `world.gameOver` reads the outcome; simulation and new villager creation stop after defeat. Monster bindings expose `leatherHelmet`, `burningInSun` and `bossWave` read-only. Sunlight damage uses onMonsterDamaged; sunlight deaths use onMonsterRemoved("killed") and are included in the saved sunlight death counter.
