# Guia de conteúdo do VillageCraft



Os arquivos são carregados uma única vez na inicialização. Reinicie o jogo após editar. Os comentários no começo de cada TOML indicam o **tipo do arquivo** e os comentários de cada propriedade explicam seu uso. Erros de conteúdo informam o caminho do arquivo. Nunca traduza IDs: eles conectam dados, scripts e saves.



| Pasta/arquivo | Tipo | Para que serve |

|---|---|---|

| Prefabs/*.toml | Plan | Construções e caminhos: modelo, custo, vagas, entradas, produção, defesa e script |

| Professions/*.toml | ProfessionPlan | Profissões: aparência, atividades aceitas, eficiência e custo de treinamento |

| Models/*.toml | ModelFile / Parts | Geometria e UV das texturas; não contém economia ou comportamento |

| Units/*.toml | UnitFile / UnitPlan | Vida, dano, alcance, velocidade e comportamento JS de unidades de combate |

| Locales/*.toml | LanguageFile | Idioma, nome exibido no menu e traduções |

| Scripts/*.js | Componentes com callbacks e world | Lógica JavaScript com acesso direto a APIs do jogo |

| game.toml | GameDefaults / VillageRules / StarterPlacement | Mapa inicial, recursos iniciais, imigração, regras e vila inicial |

| resources.toml | ResourceFile / ResourcePlan | IDs, ícones e preços de compra/venda |

| upgrades.toml | UpgradeFile / UpgradePlan | Melhorias sequenciais de armaduras e ferramentas |

| names.toml | NamePool | Listas de nomes e sobrenomes sorteados para novos aldeões |

| rendering.toml | RenderingSettings | Shader de recorte de transparência e limite de alpha |

| Templates/*.example | Exemplo, não carregado | Copie para a pasta apropriada e remova .example |



## Como adicionar uma profissão



1. Copie Templates/profession.toml.example para Professions/herbalist.toml.

2. Use Id = "herbalist", um ID exclusivo e estável.

3. Model deve existir em Models. Reutilize "farmer" para começar.

4. Jobs contém **atividades**, não IDs de construções. Jobs = ["Farming"] permite trabalhar nos prefabs cujo Job = "Farming". Outra opção é incluir "herbalist" no array Professions do prefab.

5. Ajuste Efficiency, TrainingCost e ResourceBonus. As chaves dos recursos devem existir em resources.toml.

6. Adicione "profession.herbalist" em [Text] de Locales/pt-BR.toml e en-US.toml. Sem tradução, o jogo usa Name.

7. Reinicie. A nova profissão aparece no seletor em Moradores e pode ser treinada.

8. Para permitir imigrantes dessa profissão, adicione "herbalist" em ImmigrationProfessions de game.toml.



Adicionar apenas uma profissão não cria uma construção, uma unidade de combate nem um novo modelo. Use o tipo de arquivo apropriado para cada um.



## Como adicionar uma construção



Copie um prefab parecido em Prefabs, dê um novo Id e configure Model, Cost, Input, Output, Job, Jobs, Homes, Water, Joy, Health e conexão de entrada. As propriedades têm comentários no próprio arquivo. A entrada aponta para +Z na rotação zero; o jogador gira a entrada em passos de 90 graus. O prefab ocupa um terreno.



O novo arquivo aparece automaticamente no menu. Use as traduções "prefab.<Id>.name" e "prefab.<Id>.description". Category é um ID interno: reutilize uma categoria existente ou traduza uma nova chave de categoria.



Para lógica personalizada, use Script = "meu_predio" e crie Scripts/meu_predio.js. Veja SCRIPTING.md. onBuildingWork ocorre após pagar Input; onBuildingDay ocorre ao amanhecer nas construções ativadas. Declare set_script_type(eST_Building) para uma ligação TOML ou indique o prefab como segundo argumento para substituí-lo. A produção Output continua funcionando sem JS.



## Como adicionar ou trocar um idioma



Copie Locales/en-US.toml para outro arquivo .toml. Altere Id para uma cultura .NET válida, por exemplo "es-ES", e Name para o nome exibido no menu. Traduza os valores de [Text], mantendo as chaves e os marcadores {0}, {1}, {0:0.0} etc. Reinicie: o idioma aparece em Opções > Idioma.



Português brasileiro é o padrão. A escolha é aplicada imediatamente e salva em UserData/options.toml. Chaves ausentes usam inglês; nomes de novos conteúdos usam o fallback Name/Description do seu TOML. IDs de saves e scripts não mudam com o idioma.



As entradas prefixadas por prefab., profession., entity., resource. e upgrade. nomeiam conteúdos. As demais entradas traduzem a interface, atividades e mensagens. Nomes pessoais são sorteados uma vez de names.toml e persistem no save, independentemente do idioma.



## Inventário e saves



Cada aldeão tem inventário próprio, com limite de 32 unidades. Comida e ferramentas são transferidas do estoque da vila; a transferência conserva os recursos. A comida carregada é consumida diariamente antes da comida do estoque. Produção normal permanece no estoque compartilhado; não há logística de transporte de produção nesta versão.



Clique direito no aldeão ou selecione seu nome em Moradores para ver vida, profissão, eficiência, progresso, proteção, moradia, emprego e inventário. Os botões entregam comida ou ferramentas do estoque. Scripts também podem entregar recursos usando person.give.



Saves binários v7 incluem nome, sobrenome, inventário, IDs de entidades, tags e comportamento anexado. Saves v3/v4/v5/v6 continuam legíveis. Os arquivos antigos não são reescritos até salvar. Remover um ID de conteúdo ainda usado por um save impede carregar esse save.



## Terrain and tutorial data



`terrain.toml` controls mountain height, lake radius, tree/grass density and the level starter area. New Game selects dimensions (32x32 by default) and seed. Neighboring elevations differ by at most one block; water blocks routes. Older saves retain flat terrain.



`tutorial.toml` stages have `Id`, `Title`, `Description` and `Goal` (inspect, road, trees, water, homes, defense). Text is localized. Tutorial progress persists.



The oak_tree prefab uses Natural, Harvest and RegrowDays. Foresters use HarvestSource and HarvestRadius; each tree must regrow after harvesting. Model parts accept TextureMode = "repeat" or "stretch". Animated block textures use a square frame to avoid incorrect UVs.



Model parts support `Shape = "cube"` (default) or `Shape = "cross"`. Cross draws two diagonal, two-sided cutout planes using the full texture, for flowers and grass; Size gives the footprint and height. Use this instead of thin cubes to avoid textured edges/end caps. Shape is independent of TextureMode; crossed plants always use full-face UVs.



## 1.0.5-beta: economy and logistics



`economy.toml` defines Goods rewards, the Goods-to-emerald exchange rate, delivery load size, workplace input buffer size, milk healing and armor enchantment costs/effects. Work and combat earn Goods; the city converts the balance at 06:00, then pays upkeep. Emeralds and Goods are treasury values and are never carried or stored in buildings.



Prefab fields: `StorageCapacity` (shared capacity across resource types; default 200), `Warehouse` (warehouse priority for supply collection), `Indestructible` (permanent fixtures), `LightRadius` (torch illumination radius). Small/large warehouses hold 2,000/10,000 items and employ two/five deliverymen. Output stays at its production building; recipes consume local Input supplies. Deliverymen physically pick up resources, carry them in Cargo and deliver to input buffers or warehouses. A market sale requests delivery of 20 units; press Sell again after they arrive. Purchases enter market storage. Construction, training and equipment upgrades use the city's aggregate stored budget.



Villagers walk to the nearest reachable warehouse with their supplies before trying production sources. They collect food and milk in person. A cow shed produces Leather/Milk while consuming local feed. Milk clears Burning, Poison, Slowness and Weakness and heals injuries. Armor recipes include leather; a connected mage hall staffed by a mage unlocks a single protection/damage enchantment.



`torch_workshop` turns delivered wood into Torches. Torch fixtures require no fuel, can be damaged, repaired or demolished, and can be placed on empty dry land. Villagers visit them at dusk/dawn to switch them on/off. Lit torches illuminate terrain and nearby objects.



Binary saves v8 append building storage/torch state/market requests, villager cargo/delivery targets/effects, settlement day and enchantment status. v3-v7 and legacy JSON imports retain supplies in a starter warehouse; overloaded legacy stock is preserved, but production cannot add more until space is freed. Tutorial dismissal is a global preference in UserData/options.toml; Reset tutorial restores it for current and future games.



Model parts accept `Pitch` in degrees to rotate their cuboids around the local X axis before heading rotation. The cow uses this to map its original skin onto its horizontal torso.



Warehouses use distinct loading-depot models with pallets and stacked crates. The digital 24-hour clock, Minecraft clock icon, lighting, monster spawns and defender patrols agree: midnight is 00:00, sunrise 06:00, noon 12:00 and sunset 18:00. Ordinary workers finish at 17:00; torches are lit from dusk until dawn.



## Sunlight and defeat



`sunlight.toml` defines RegularHelmetChance (0..1, default 0.65) and DamagePerSecond (default 4). Unit files mark `BurnsInSun = true`; zombies and skeletons use it. Ordinary spawns roll once for a leather helmet. Boss waves always give susceptible units helmets. Helmets remain equipped and block sunlight burning. Other nether units retain their immunity. Water and prefab tiles marked `SheltersFromSun = true` prevent exposure; daylight must be bright enough to burn. Night monsters no longer disappear at dawn. Sunlight kills count as defeated monsters but do not award labor Goods.



The final villager's removal ends the game and freezes simulation/immigration. The game-over screen lists time, map, population, losses, combat, deliveries, economy, equipment, stores, production, remaining buildings/entities and journal. Retry reloads an existing living save without overwriting it. The red Delete save button asks for confirmation and removes only the selected name's .vcs/.json files. Unsaved games offer New Game or selection of another save. Saving is disabled in the defeat menu to preserve the retry snapshot.



Binary v9 appends enemy helmet/boss flags, defeat state and lifetime statistics. Versions v3-v8 remain readable. New lifetime counters for older villages start on the import day (displayed as Recorded since day); prior historical activity cannot be reconstructed. Existing non-night hostiles receive boss protection when importing older saves. All new state persists in subsequent saves.





## Comportamento de construções e recursos tipados



Cada prefab distribuído aponta para um arquivo independente `Scripts/building_<id>.js` por meio de `Script`. Custos, receitas e estatísticas ficam no TOML; decisões de produção, reprodução, regeneração e criação de guardiões ficam no componente. Novas construções podem usar `building.js` como base. Veja a seção de comportamentos padrão em [SCRIPTING.md](SCRIPTING.md).



As chaves de recursos nos TOMLs são nomes de `ResourceType`: são convertidas uma única vez para dicionários com chaves enum. Recursos novos exigem adicionar um membro estável ao enum e recompilar, além da definição em `resources.toml`; nomes inválidos são rejeitados. Isso não altera a possibilidade de adicionar profissões, modelos e prefabs por arquivos.



## Beta 1.0.6

Tochas, oficina e conversão de tochas foram removidas. O formato de save atual é binário v11 e rejeita versões beta anteriores sem sobrescrever os arquivos. BirthChance em game.toml/Rules define a chance diária por casa elegível (0.6 por padrão); a interface permite alterar por vila. O botão de redistribuição de profissões fica em Moradores. Reparos automáticos usam materiais físicos e os parâmetros RepairWorkSeconds, RepairAmount e RepairCost em economy.toml. game.d.ts e jsconfig.json fornecem tipos e autocomplete para os scripts.


## Traduções, encontros e logs

As traduções usam IDs estáveis (`ui.options`, `message.building_placed` etc.), com comentário de contexto antes de cada chave. Não use mais frases em inglês como chave. Veja [Locales/README.md](Locales/README.md) para criar outro idioma.

`NightSpawnIntervalMinutes` em game.toml/Rules define o intervalo em minutos do jogo (60 por padrão). `NightCount` agora é a quantidade por encontro. Encontros se repetem entre 18:00 e 05:00, depois das cinco noites de paz, com limite de inimigos e estado salvo. `NightAmbient` em rendering.toml controla apenas a claridade mínima da noite (0.4 por padrão).

Cada execução grava um log UTF-8 em UserData/Logs, ao lado do executável. Logs de script ficam sempre ativos, inclusive em Release: world.log, world.warn e world.error. Os erros incluem o nome do componente. world.translate e world.format permitem localizar as mensagens dos scripts.

### Nether model skins

Piglin variants use distinct `entity/piglin/piglin.png`, `piglin_brute.png` and `zombified_piglin.png` skins (64�64); Blaze uses `entity/blaze.png` (64�32). Supply these externally. The Programmer Art pack in a standard asset index may omit Piglin skins. Missing skins are logged and use model fallback colors.

The Piglin cuboid UV regions can also be inspected in [Mojang�s geometry reference](https://github.com/Mojang/bedrock-samples/blob/main/resource_pack/models/entity/piglin.geo.json). The game uses its own TOML models, including separate limb regions, snout, tusks and clothing layers. No proprietary skin is bundled.
