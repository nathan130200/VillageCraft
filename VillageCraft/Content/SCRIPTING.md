# Componentes JavaScript: ciclo de vida, eventos e world

O componente independente ScriptRuntime mantém um engine Jint por arquivo por mundo. Não há estado JS compartilhado entre vilas, nem serialização JSON na ponte. Os arquivos Scripts/*.js são carregados automaticamente, uma vez. Arquivos iniciados com `_` são bibliotecas, não componentes independentes. Reinicie após editar.

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

O alvo é opcional. Uma IA sem alvo serve como comportamento padrão referenciado por Units/*.toml (Behavior) ou anexado a uma instância. Os arquivos villager.js, guardian.js e animal.js são os padrões dos respectivos grupos. Um script de construção sem alvo pode ser referenciado por Prefabs/*.toml (Script) ou anexado a uma instância. Um alvo explícito substitui o padrão dos TOMLs. Duas substituições explícitas para o mesmo tipo/alvo são rejeitadas com diagnóstico, sem depender da ordem dos arquivos.

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
| onBuildingDay(building) | Início do novo dia em construção ativada |
| onDayStarted() | Mods; início do novo dia |
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

Se um override estiver ausente, não implementar o callback ou falhar, o runtime tenta o componente JavaScript padrão do tipo. Não existe uma segunda IA de combate escondida em C#. Se o próprio padrão falhar, o erro fica registrado e a ação para até a correção/reinicialização. Os scripts antigos function update(ctx) devem migrar para onMonsterUpdate e declarar o tipo.

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

Os scripts não têm importação de CLR, Node ou acesso ao save. Cada execução possui limites de 200000 instruções, 32 MB de alocação e 250 ms (inclui primeiro acesso às APIs nativas). Uma falha desativa aquele componente até reiniciar e registra o arquivo/motivo no console; outros scripts continuam.
`onTreeHarvested(tree, villager)` fires after harvesting a tree and starting its regrowth timer.

## 1.0.5-beta

- `onGoodsConverted(emeralds)` fires once at the 06:00 settlement; `onDawn()` follows.
- `onResourceCollected(villager, amount, sourceBuilding)` and `onDeliveryCompleted(villager, destinationBuilding)` report physical collection/delivery.
- `villager.cargo(resource)` reads in-transit delivery items. `villager.effect(name)` reads remaining seconds; `applyEffect(name, seconds)` supports Burning, Poison, Slowness and Weakness. Milk clears all effects.
- `onArmorEnchanted()` reports the city-wide equipment enchantment.
- `world.stock(resource)` counts stored resources, excluding personal inventories and cargo. Explicit `addResource` mod grants enter warehouse storage. `villager.give` only transfers from a reachable nearby store when the villager is physically at it. Emeralds and Goods cannot be carried.

`onGameOver()` reports the elimination of the final villager. `world.gameOver` reads the outcome; simulation and new villager creation stop after defeat. Monster bindings expose `leatherHelmet`, `burningInSun` and `bossWave` read-only. Sunlight damage uses onMonsterDamaged; sunlight deaths use onMonsterRemoved("killed") and are included in the saved sunlight death counter.


## Comportamentos padrão editáveis

As decisões de trabalho, suprimentos, entregas, floresta, reparos, patrulha e combate dos aldeões estão em `Scripts/villager.js`. Distribuição de empregos está em `employment.js`. Os inimigos usam `melee.js`, `ranged.js` ou `creeper.js`, conforme `Units/<tipo>.toml`. Seleção inicial de alvos e consultas comuns estão em `_ai.js`. Guardiões e animais usam `guardian.js` e `animal.js`.

Cada prefab distribuído possui **seu próprio componente**, `Scripts/building_<id>.js`, indicado por `Script` no TOML. Por exemplo: `building_farm.js`, `building_chicken_coop.js`, `building_pig_pen.js`, `building_iron_foundry.js` e `building_oak_tree.js`. Edite um sem alterar os outros. `building.js` fornece somente o fallback genérico para novos conteúdos sem componente próprio. `well.js` continua disponível para ligações antigas.

C# continua responsável pelas operações da simulação: navegação, colisão, validação de distância/alcance/cooldown, transferências físicas, receitas, dano, persistência e relógio. JavaScript decide qual ação tentar, em quem, quando e com qual prioridade. Efeitos biológicos, economia global e geração de encontros continuam sistemas da simulação, não rotinas de movimento/decisão individuais.

### Recursos e tarefas nomeados

~~~js
const food = world.stock(resources.Food);
worker.task = tasks.GoingToWork;
worker.collect(warehouse, resources.Food, 2, dt);
~~~

`resources` e `tasks` são objetos constantes e congelados, disponíveis em todos os componentes. Em C#, estoques, receitas, inventários, carga, custos, bônus e estatísticas usam `ResourceType`, não chaves string. Na interface JS e no TOML os nomes continuam legíveis; a ponte valida nomes estritamente e converte para enum. Nomes numéricos ou desconhecidos não criam recursos novos. O beta 1.0.6 usa formato binário 11 com IDs numéricos de recursos e não carrega os saves beta anteriores.

Para adicionar um **novo tipo de recurso**, adicione um membro com valor explícito e estável em `ResourceType`, sua definição em `resources.toml` e traduções/ícones, depois recompile. Não renumere membros existentes. Construções, profissões e modelos continuam extensíveis por TOML.

### Ações de aldeões

| API | Uso |
|---|---|
| `home`, `workplace` | Vínculo da casa/trabalho ativo e conectado, ou null |
| `task`, `hasWorked` | Estado de tarefa e serviço; use `tasks.<nome>` |
| `resourceTarget`, `repairTarget`, `pickupSource`, `deliveryTarget`, `deliveryResource` | IDs/estado das tarefas; carga e destino de entrega persistem |
| `cargoCount`, `hasEffects`, `combatRange` | Consultas do estado individual |
| `walkTo(building, dt, roadsOnly=false)` | Caminha até a construção; retorna true ao chegar |
| `canReach(building)`, `canSee(x,z)` | Acessibilidade e linha de visão |
| `collect(building, resource, amount, dt)` | Caminha e retira suprimentos físicos para inventário pessoal |
| `pickup(source, destination, resource)` | Retira carga de entrega somente ao chegar à origem |
| `deliver(destination)` | Deposita carga somente ao chegar ao destino |
| `work(dt)` | Acumula trabalho local e solicita o callback de produção |
| `chop(tree, dt)` | Acumula corte de árvore próxima ao acampamento atribuído |
| `drinkMilk()` | Consome leite carregado e aplica recuperação |
| `patrol(x,z,dt)`, `attackMonster(id)` | Movimento defensivo e ataque validado |

Os passos aceitam `0 < dt <= 0.2`, em segundos de simulação. Não invoque várias ações de movimento para a mesma entidade no mesmo passo. Inventários continuam limitados a 32 unidades entre itens pessoais e carga. Consultas retornam vínculos vivos; não suponha que um alvo ainda existe depois de uma remoção enfileirada.

### Guardiões, animais e alvos

`onGuardianUpdate(ctx, dt)` e `onAnimalUpdate(ctx, dt)` recebem o mesmo contexto básico de movimento de monstros. Use `set_script_type(eST_AI, "iron_golem")`, `"snow_golem"`, `"chicken"`, `"pig"`, `"sheep"` ou `"cow"` para um override por tipo; `"guardian"` e `"animal"` substituem o grupo. `ctx.self.home` e `ctx.self.age` informam origem e idade.

`world.getGuardians()` e `world.getAnimals()` consultam esses grupos. Eventos `onGuardianAttached`, `onGuardianSpawned`, `onAnimalAttached` e `onAnimalSpawned` permitem descobrir entidades existentes/criadas. Comportamentos também podem ser anexados por instância a esses grupos.

`ctx.selectTarget(kind, id)` seleciona `"villager"`, `"building"`, `"guardian"` ou `"monster"`. `distance`, `visible`, `targetVillager`, `targetBuilding` e `targetGuardian` passam a refletir o alvo escolhido. Também existem `attackGuardian(id)` e `attackMonster(id)`. Um ataque inválido não causa dano, e uma chamada de atualização do contexto aceita no máximo uma ação efetiva. A seleção inicial de inimigos é feita pelo helper JS antes de `onMonsterUpdate`; o componente pode substituí-la.

### Produção e serviço de construções

~~~js
set_script_type(eST_Building);

function onBuildingProduce(building, worker) {
    // Retorne true quando o ciclo efetivamente produzir/prestar serviço.
    return building.produce(worker);
}
function onBuildingDay(building) {
    if (!building.worked || world.day % 3 !== 0) return;
    building.serviceCredit += building.efficiency;
    building.breed();
}
~~~

`onBuildingProduce` decide o ciclo antes da aplicação da receita; não é uma notificação. `building.produce(worker)` valida a presença do trabalhador, consome Input local e aplica Output do TOML atomicamente. `onBuildingWork` é a notificação posterior e fica enfileirada para evitar reentrada de JavaScript. Se um componente não implementar produção, o runtime usa o arquivo do prefab ou o fallback genérico.

`onBuildingDay` da construção roda uma vez no início do novo dia, antes de limpar a contabilidade do dia anterior. Mods recebem a notificação enfileirada. Isso é distinto de `onDawn()`, às 06:00.

Propriedades adicionais: `worked`, `efficiency`, `serviceCredit` (gravável de 0 a 2), `jobs`, `job`, `animal`, `adultAnimals`, `defender`, `createsGuardian`, `harvestSource`, `harvestRadius`, `growthDays`, `warehouse`, `priority`, `connected`, `path`, `natural`, `indestructible`, `blocksRaiders`, `storageFree`, `requestedResource`, `inputs`, `outputs` e `inputTarget(resource)`.

Ações: `rewardWork(worker)` concede a contribuição de serviço; `breed()` tenta criar um filhote com crédito, alimento e limites válidos; `collectAnimalProduct(resource, amount)` deposita produtos locais; `createGuardian()` valida crédito, materiais e limite da oficina; `grow()` avança um dia de regeneração. Confira os componentes individuais para exemplos completos.

### Consultas específicas e empregos


`world.roads()` retorna pontos `{x,z}` conectados, ordenados; `anchorX`, `anchorZ` e `elapsedDay` fornecem centro e segundos do dia atual. `world.resourceNames()` retorna os nomes dos recursos disponíveis.

`employment.js` recebe `onAssignJobs()` quando a simulação solicita redistribuição de empregos. `building.qualified(villager)` consulta elegibilidade de especialista; cidadãos são selecionados pelo script como ajudantes. `world.assignJobs(villagerIds, workplaceIds)` aplica o plano completo atomicamente, validando IDs, vagas, qualificação e conexão. Moradores não incluídos ficam sem trabalho. Edite o algoritmo do componente para mudar prioridades.

### Verificar alterações

Execute os checks em uma compilação de desenvolvimento:

~~~sh
dotnet run --project VillageCraft -c Debug -- --self-test
~~~

Os checks e modos de captura não são compilados em Release. Capturas de teste ficam em `screenshots/` na raiz do projeto. Reinicie o jogo depois de editar scripts: não há hot reload. Estado global JS não é persistido; use tags, os campos de tarefa/destino e o estado numérico de monstros quando precisar de continuidade após carregar.


## Modding com tipos, reparos e população (1.0.6)

`game.d.ts` declara a API viva completa e os callbacks. `jsconfig.json` é configuração apenas do editor, não um arquivo carregado pelo jogo. Abra Content no VS Code ou outro editor TypeScript para autocomplete; valide com `tsc --project VillageCraft/Content/jsconfig.json`. Os componentes distribuídos têm JSDoc nos callbacks. Scripts continuam JavaScript normal: não importe os tipos em tempo de execução.

Todos os aldeões podem executar `repair(building, dt)`, que exige proximidade, materiais carregados e tempo de trabalho. `repairTarget` reserva a construção. `world.getDamagedBuildings()`, `building.repairInputs` e `building.repairCost(resource)` ajudam a escolher tarefas. O evento `onBuildingRepaired(building, amount, worker)` informa o resultado. Materiais, duração e cura por lote estão em economy.toml.

`building.availableSupply(resource)` exclui a reserva de Input de fontes não-armazém. Use esta consulta para evitar retirar a alimentação de um cercado como produto excedente. `inputAmount(resource)` permite medir quantos ciclos de suprimento restam. As rotas preferem caminhos e respeitam entradas, com desvio por terreno quando necessário.

`population.js` decide nascimentos em onDayStarted: `world.birthChance` é a probabilidade (0..1), `world.queueBirth(homeId)` agenda um bebê oculto e `world.pendingBirths` consulta a quantidade. Casas expõem `homes`. São necessários dois moradores e uma vaga; limites são reavaliados na execução. `onBabyBorn(home)` ocorre ao agendar; `onVillagerBorn(villager)` ocorre ao crescer no dia seguinte. Pendências e probabilidade persistem em saves v11. Não há modelo visível de bebê durante esse período.

`employment.js` implementa `onReassignProfessions()`, acionado pelo botão em Moradores. `world.getProfessions()`, `building.allowedProfessions`, `villager.setProfession(id)` e `world.assignJobs(...)` permitem adaptar a política. setProfession não cobra treinamento nem reentra na distribuição; o botão é uma reorganização automática explícita.

Tochas, oficina de tochas, recurso de tochas e callbacks de iluminação de tochas foram removidos. Use apenas a API atual declarada em game.d.ts.


## Localização e logging permanente

Use IDs estáveis definidos em Locales/*.toml. Cada chave tem um comentário explicando contexto e argumentos. Exemplo:

~~~js
world.log(world.translate("ui.options"));
world.log(world.format("message.building_placed", [buildingName]));
world.warn("Production input missing for building " + building.id);
world.error("Custom behavior could not acquire its target");
~~~

`translate(key)` retorna a tradução, com fallback para inglês e depois para o ID. `format(key, arguments)` recebe um array de argumentos; a ordem pode mudar na tradução e os números respeitam a cultura selecionada. Veja [o guia de localização](Locales/README.md).

`log`, `warn` e `error` sempre gravam em UserData/Logs, inclusive em Release. O arquivo de cada execução identifica o componente e usa horário UTC. Mensagens longas continuam no arquivo; somente mensagens curtas de log também entram no diário da vila. Exceções dos callbacks gravam a stack trace e desativam o componente com falha conforme as regras do runtime. Não é preciso ativar logging de desenvolvimento.

`onNightStarted()` continua sendo um evento de início de noite. Encontros normais agora podem gerar vários grupos entre 18:00 e 05:00; use `onMonsterSpawned` para observar cada criação. Pausa e velocidade usam o mesmo relógio da simulação, e os intervalos persistem no save beta v11.

### Guardian patrol example

`guardian.js` keeps a random reachable road destination per golem, follows it until arrival or timeout, and immediately prioritizes monsters. `MobScriptContext.canReach(x, z)` checks a route without moving; `moveTo(x, z)` follows it through the existing navigation system. Patrol state is transient JavaScript state and is rebuilt after loading.
