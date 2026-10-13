# Chess Console

## 1. Visão geral

O **Chess Console** é um projeto de xadrez com duas formas de uso:

- um jogo local no terminal;
- uma aplicação web multiplayer em tempo real.

As duas aplicações usam o mesmo núcleo de regras. Isso evita que o xadrez funcione de uma forma no terminal e de outra forma no navegador.

Na versão web, dois jogadores entram na mesma partida usando um `Guid`. O primeiro jogador recebe as peças brancas, o segundo recebe as peças pretas. O tabuleiro, os movimentos, os relógios e o estado da partida são sincronizados pelo SignalR.

O servidor também entrega o frontend estático. Portanto, a aplicação web é um monólito: o mesmo processo serve os arquivos HTML/CSS/JavaScript, cria sessões de jogador, mantém as partidas e hospeda o Hub do SignalR.

## 2. Tecnologias

| Tecnologia | Uso |
| --- | --- |
| .NET 9 | Runtime e SDK das aplicações |
| C# | Regras de negócio, servidor e testes |
| ASP.NET Core Minimal API | Endpoint de criação de sessão e pipeline HTTP |
| SignalR | Comunicação persistente e eventos em tempo real |
| JavaScript ES Modules | Frontend web |
| HTML e CSS | Interface do jogo |
| xUnit | Testes unitários e de integração |
| `ConcurrentDictionary` | Repositório de partidas em memória |
| `SemaphoreSlim` | Exclusão mútua por partida |

As dependências `@microsoft/signalr` e `Swiper` são carregadas por CDN no `index.html`.

## 3. Estrutura do repositório

```text
chess-console.sln
├── chess-console.Core/
│   ├── Board/              # Tabuleiro, posições, peças e cores
│   ├── Chess/              # Regras completas de uma partida
│   ├── Dto/                # Objetos enviados para o frontend
│   ├── Exceptions/         # Erros esperados das regras
│   └── Matches/            # Sessão, relógio e armazenamento de partidas
├── chess-console.Server/
│   ├── Filters/            # Autenticação da sessão e rate limiting
│   ├── Hubs/               # Contrato SignalR
│   ├── Services/           # Casos de uso e relógio das partidas
│   └── wwwroot/            # Frontend servido pelo ASP.NET Core
├── chess-console/
│   ├── Display/            # Renderização do jogo no terminal
│   ├── Program.cs          # Loop principal do jogo local
│   └── Screen.cs           # Entrada e saída do console
├── chess-console.Core.Tests/
│   ├── Chess/              # Testes de regras do xadrez
│   ├── Dto/                # Testes de mapeamento para a API
│   └── Matches/            # Testes de sessão, relógio e repositório
└── chess-console.Server.Tests/
    ├── Hubs/               # Testes do contrato SignalR e HTTP
    └── Services/           # Testes da camada de aplicação
```

## 4. Como executar

### Pré-requisitos

- .NET SDK 9 instalado;
- acesso à internet para carregar os scripts CDN do frontend web.

### Executar os testes

Na raiz do repositório:

```bash
dotnet test
```

### Executar o servidor web

```bash
dotnet run --project chess-console.Server
```

Depois, abra a URL HTTPS exibida pelo ASP.NET Core. Em desenvolvimento, as origens `http://localhost:3000` e `http://localhost:5173` já são permitidas pelo arquivo `appsettings.Development.json`.

O fluxo normal no navegador é:

1. informar um nome entre 2 e 24 caracteres;
2. clicar em **Connect**;
3. criar uma partida ou entrar em uma partida existente;
4. compartilhar o `Guid` ou o link da partida;
5. jogar quando for a sua vez.

### Executar a versão de console

```bash
dotnet run --project chess-console
```

O programa solicita a casa de origem e a casa de destino em notação de xadrez, como `e2` e `e4`. Na promoção, solicita uma peça entre `Q`, `R`, `B` e `N`.

## 5. Arquitetura

### 5.1 Núcleo de xadrez

O projeto `chess-console.Core` não depende de ASP.NET Core e concentra o estado e as regras do jogo.

#### `Board`

`Board` representa uma matriz de 8 por 8 posições. Ele sabe:

- verificar se uma posição está dentro do tabuleiro;
- localizar uma peça;
- inserir uma peça;
- remover uma peça.

As coordenadas internas começam em zero. A camada `ChessPosition` converte essas coordenadas para a notação humana:

```text
e4 -> linha interna 4, coluna interna 4
a8 -> linha interna 0, coluna interna 0
```

Essa conversão fica centralizada para que o servidor, o frontend e o console usem o mesmo formato.

#### Peças

`Piece` é a classe base das peças. Cada peça implementa `PossibleMoves()`:

- `King`;
- `Queen`;
- `Tower`, enviada para o frontend como `Rook`;
- `Bishop`;
- `Knight`;
- `Pawn`.

Além dos movimentos possíveis, existe `AttackedSquares()`. Essa separação é importante para regras como xeque, roque e validação de casas atacadas.

#### `ChessMatch`

`ChessMatch` coordena uma partida completa. Ele mantém:

- tabuleiro e peças;
- turno numérico;
- cor do jogador atual;
- estado de xeque;
- estado de conclusão;
- vencedor;
- peças capturadas;
- histórico textual de movimentos;
- posição vulnerável a en passant;
- histórico de posições para repetição;
- contador de meio-lances para empate.

O fluxo de um movimento é:

1. validar a origem;
2. validar o destino;
3. mover a peça e capturar, se necessário;
4. mover a torre automaticamente durante o roque;
5. tratar en passant;
6. desfazer o movimento se o próprio rei ficar em xeque;
7. promover o peão quando necessário;
8. verificar xeque, xeque-mate e empate;
9. alternar o jogador;
10. registrar o movimento e a nova posição.

As regras cobertas pelo núcleo incluem:

- movimentos de todas as peças;
- xeque;
- xeque-mate;
- roque dos dois lados;
- en passant;
- promoção para dama, torre, bispo ou cavalo;
- empate por afogamento;
- empate por material insuficiente;
- empate por 75 movimentos sem mover peão ou capturar;
- empate por repetição da mesma posição três vezes;
- rejeição de movimentos que deixam o próprio rei em xeque.

### 5.2 Sessões multiplayer

Uma `ChessMatch` representa apenas o tabuleiro e as regras. Para transformá-la em uma partida multiplayer, o projeto usa `GameSession`.

`GameSession` adiciona:

- um identificador `Guid`;
- assento branco e assento preto;
- associação entre conexão SignalR e cor;
- relógio de 10 minutos para cada jogador;
- oferta de troca de lados;
- sincronização das operações por `SemaphoreSlim`.

Toda alteração da sessão passa por `ExecuteAsync`. O método adquire o semáforo, executa a operação e libera o semáforo em `finally`. Assim, duas chamadas concorrentes não conseguem aplicar movimentos sobre o mesmo estado ao mesmo tempo.

Os métodos que dependem do lock verificam isso com `EnsureLocked()`. Uma chamada direta fora de `ExecuteAsync` gera erro em vez de permitir acesso inseguro.

### 5.3 Armazenamento

`InMemoryMatchRepository` armazena as sessões em um `ConcurrentDictionary<Guid, GameSession>`.

Características da decisão:

- criação e busca de partidas são rápidas;
- não há banco de dados;
- as partidas desaparecem quando o processo é reiniciado;
- o modelo é adequado para uma instância única do servidor;
- não existe sincronização entre réplicas do servidor.

Partidas vazias são removidas. Uma partida que ainda tem um jogador libera o assento do jogador que saiu e continua disponível para outro jogador entrar.

### 5.4 Camada de aplicação

`MatchService` é a camada que conecta o Hub ao núcleo. Ele concentra regras que não pertencem ao tabuleiro, como:

- criar e entrar em partida;
- ocupar assentos;
- localizar partidas esperando adversário;
- deixar ou abandonar uma partida;
- verificar se a conexão pertence à partida;
- verificar se é a vez da conexão;
- converter casas de texto para `Position`;
- interpretar a peça de promoção;
- oferecer e responder à troca de lados;
- iniciar e atualizar o relógio.

Essa separação mantém `ChessHub` focado no transporte e impede que o Hub conheça detalhes do repositório ou da implementação do tabuleiro.

### 5.5 Mapeamento para o frontend

`BoardMapper` transforma objetos internos em DTOs serializáveis. O frontend não recebe `Board`, `ChessMatch` ou objetos com referências circulares.

Os principais DTOs são:

- `PieceDto`: tipo, cor e posição, como `e4`;
- `BoardStateDto`: estado completo da partida;
- `MoveResultDto`: movimento recém-aplicado e novo estado;
- `PossibleMovesDto`: origem e lista de destinos;
- `MatchJoinedDto`: id da partida, cor do jogador, nome e estado inicial.

As matrizes booleanas internas de movimentos possíveis são convertidas para listas como `["e3", "e4"]`. Isso simplifica o JSON e evita que o frontend precise conhecer a indexação interna do tabuleiro.

O SignalR usa camelCase. Por exemplo, `CurrentPlayer` sai como `currentPlayer`.

## 6. Fluxo da aplicação web

### 6.1 Criação da sessão do jogador

Antes de conectar ao SignalR, o frontend chama:

```http
POST /api/player/session
Content-Type: application/json

{
  "name": "Nome do jogador"
}
```

Resposta de sucesso:

```json
{
  "token": "token-aleatorio",
  "playerId": "identificador-do-jogador",
  "name": "Nome do jogador",
  "expiresAtUtc": "2026-10-13T01:17:00Z"
}
```

O nome é normalizado removendo espaços extras e deve ter entre 2 e 24 caracteres. Caracteres de controle são rejeitados. O token é aleatório, não contém dados do jogador e expira após 25 minutos.

O token fica em `sessionStorage`. O nome fica em `localStorage` apenas para preencher novamente o formulário.

### 6.2 Conexão ao Hub

O frontend cria uma conexão em:

```text
/hubs/chess
```

O token é enviado pelo `accessTokenFactory` do cliente SignalR. O `PlayerTokenHubFilter` aceita o token pela query string `access_token` ou pelo header `Authorization: Bearer`.

Cada invocação do Hub precisa de uma sessão válida. Sessões expiradas ou ausentes recebem `HubException`.

### 6.3 Criar ou entrar em uma partida

O primeiro jogador chama `CreateMatch`. O servidor:

1. cria uma `GameSession`;
2. ocupa o assento branco;
3. adiciona a conexão ao grupo SignalR cujo nome é o `matchId`;
4. retorna o tabuleiro inicial.

O segundo jogador chama `JoinMatch(matchId)`. O servidor:

1. localiza a partida;
2. ocupa o assento preto;
3. adiciona a conexão ao mesmo grupo;
4. avisa o primeiro jogador com `OpponentJoined`;
5. envia o estado atualizado do relógio;
6. retorna o tabuleiro inicial ao segundo jogador.

Cada partida é um grupo SignalR. Movimentos são enviados para todos os membros do grupo, enquanto mensagens específicas, como uma oferta de troca de lados, são enviadas para uma conexão individual.

### 6.4 Movimento

Para mostrar os destinos possíveis, o frontend chama `GetPossibleMoves(matchId, origin)`.

Para jogar:

```text
MakeMove(matchId, origin, target)
```

Na promoção:

```text
MakeMoveWithPromotion(matchId, origin, target, promotion)
```

O serviço verifica a conexão, a partida, o turno, a posição e as regras de xadrez. Em caso de sucesso, o Hub envia `ReceiveBoardState` para o grupo inteiro.

Em caso de erro esperado, como jogar fora da vez ou escolher um destino inválido, o Hub envia `ReceiveError` ao jogador que fez a chamada. O estado do tabuleiro não é alterado quando o movimento é rejeitado.

### 6.5 Relógio

Cada jogador começa com 10 minutos. O relógio:

- permanece parado enquanto a partida espera o segundo jogador;
- começa no primeiro movimento;
- troca de jogador junto com o turno;
- é reiniciado temporalmente após cada movimento;
- é atualizado a cada segundo pelo `MatchClockService`;
- conclui a partida quando o tempo do jogador atual chega a zero.

O relógio é calculado no servidor usando UTC. O frontend faz uma interpolação visual entre atualizações para evitar que o contador pare entre eventos do SignalR. O servidor continua sendo a autoridade.

### 6.6 Saída e desconexão

Ao clicar em **Leave match**, o jogador abandona explicitamente a partida:

- se a partida está esperando o adversário, a sessão fica vazia e é removida;
- se a partida está completa, o adversário recebe vitória por abandono;
- a conexão é removida do grupo.

Quando a conexão cai, `OnDisconnectedAsync` libera o assento e avisa o outro jogador com `OpponentLeft`. Se a partida ainda não terminou, o jogador restante pode esperar uma nova conexão ocupar o assento.

## 7. Contrato SignalR

Endpoint do Hub:

```text
/hubs/chess
```

### Métodos chamados pelo cliente

| Método | Parâmetros | Retorno ou efeito |
| --- | --- | --- |
| `CreateMatch` | nenhum | retorna `MatchJoinedDto` |
| `JoinMatch` | `matchId` | retorna `MatchJoinedDto` |
| `GetWaitingMatches` | nenhum | retorna `Guid[]` |
| `ExpireWaitingMatch` | `matchId` | remove uma partida esperando, se chamada pelo dono |
| `LeaveMatch` | `matchId` | libera o jogador ou abandona a partida |
| `GetBoardState` | `matchId` | retorna `BoardStateDto` |
| `GetPossibleMoves` | `matchId`, `origin` | retorna `PossibleMovesDto` |
| `MakeMove` | `matchId`, `origin`, `target` | transmite o novo estado |
| `MakeMoveWithPromotion` | `matchId`, `origin`, `target`, `promotion` | transmite o novo estado |
| `OfferSideSwap` | `matchId` | envia oferta ao adversário |
| `RespondToSideSwap` | `matchId`, `accepted` | aceita ou recusa a oferta |

### Eventos enviados pelo servidor

| Evento | Destinatário | Conteúdo |
| --- | --- | --- |
| `ReceiveBoardState` | grupo da partida | `MoveResultDto` |
| `ReceiveClockState` | grupo da partida | `BoardStateDto` |
| `ReceiveError` | autor da chamada | mensagem de erro |
| `OpponentJoined` | jogador que já estava na partida | cor do novo jogador |
| `OpponentLeft` | jogadores restantes | sem payload |
| `MatchAbandoned` | adversário | `BoardStateDto` final |
| `SideSwapRequested` | adversário | sem payload |
| `SideSwapDeclined` | solicitante | sem payload |
| `SideSwapAccepted` | os dois jogadores | nova cor de cada jogador |

## 8. Formato do estado do tabuleiro

Exemplo simplificado de `BoardStateDto`:

```json
{
  "pieces": [
    { "type": "King", "color": "White", "position": "e1" },
    { "type": "Pawn", "color": "White", "position": "e2" }
  ],
  "turn": 1,
  "currentPlayer": "White",
  "check": false,
  "completed": false,
  "draw": false,
  "timeout": false,
  "resignation": false,
  "whiteTimeSeconds": 600,
  "blackTimeSeconds": 600,
  "clockRunning": false,
  "winner": null,
  "capturedWhitePieces": [],
  "capturedBlackPieces": [],
  "moveHistory": []
}
```

Significado dos campos de finalização:

- `completed`: a partida terminou;
- `draw`: terminou empatada;
- `timeout`: terminou por tempo;
- `resignation`: terminou por abandono;
- `winner`: cor vencedora ou `null` em empate.

## 9. Frontend

O frontend está em `chess-console.Server/wwwroot`.

### Responsabilidades dos módulos JavaScript

| Arquivo | Responsabilidade |
| --- | --- |
| `app.js` | inicialização da aplicação, criação da sessão e reconexão |
| `connection.js` | conexão SignalR, invocações e eventos do servidor |
| `state.js` | estado local, seleção, turno, relógio e zoom |
| `render.js` | montagem e atualização do tabuleiro e do painel |
| `ui.js` | listeners de botões, diálogos e contagem de espera |
| `dom.js` | referências aos elementos HTML e mensagens da interface |
| `swiper-ui.js` | navegação dos painéis responsivos |

O tabuleiro é virado quando o jogador é preto, para que cada jogador veja seu lado na orientação inferior. A seleção de uma peça consulta o servidor para obter os destinos válidos. O cliente não implementa uma segunda versão das regras de xadrez.

O navegador tenta reconectar automaticamente. Quando a conexão volta, o frontend entra novamente na partida atual usando o `matchId` da URL.

## 10. Configuração

### CORS

Em desenvolvimento, `appsettings.Development.json` permite:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:5173"
    ]
  }
}
```

Em outro ambiente, configure `Cors:AllowedOrigins` com as origens reais. Quando há origens configuradas, o servidor permite headers, métodos e credenciais para essas origens.

### Rate limiting

O servidor possui dois limites independentes:

| Configuração | Padrão | Protege |
| --- | ---: | --- |
| `RateLimiting:SignalR:PermitLimit` | 30 | negociações HTTP do SignalR |
| `RateLimiting:SignalR:WindowSeconds` | 10 | janela das negociações |
| `RateLimiting:SignalR:SegmentsPerWindow` | 2 | segmentos da janela |
| `RateLimiting:SignalR:InvocationPermitLimit` | 40 | chamadas de métodos do Hub |
| `RateLimiting:SignalR:InvocationWindowSeconds` | 10 | janela das chamadas do Hub |
| `RateLimiting:SignalR:InvocationSegmentsPerWindow` | 2 | segmentos da janela |

Os limites são particionados pelo endereço IP. A negociação HTTP retorna `429 Too Many Requests`. Uma chamada SignalR excedente recebe um `HubException`.

### Proxy reverso

`ForwardedHeaders:KnownProxies` configura os proxies confiáveis para `X-Forwarded-For` e `X-Forwarded-Proto`. Os valores precisam ser endereços IP válidos.

O servidor usa HTTPS redirection e HSTS fora do ambiente de desenvolvimento.

## 11. Segurança e confiabilidade

Decisões implementadas:

- cada conexão do Hub precisa de token de sessão válido;
- nomes de jogador são validados e normalizados;
- tokens são gerados com `RandomNumberGenerator`;
- o token não é salvo em `localStorage`;
- chamadas do Hub são limitadas por IP;
- negociações HTTP também são limitadas;
- operações de uma mesma partida são serializadas;
- o cliente nunca é a autoridade para validar movimentos ou tempo;
- mensagens de erro esperadas são enviadas pelo contrato `ReceiveError`;
- CORS usa uma lista explícita de origens quando configurada.

Limitações atuais:

- sessões e partidas ficam apenas na memória;
- reiniciar o processo encerra todas as partidas;
- várias instâncias do servidor não compartilham partidas;
- não há conta de usuário, senha ou persistência de identidade;
- o token é anônimo e expira após 25 minutos;
- os scripts externos dependem de CDN;
- o rate limiting por IP pode tratar usuários atrás do mesmo proxy como uma única origem se o encaminhamento não estiver configurado corretamente.

## 12. Estratégia de testes

Os testes são divididos em duas camadas.

### Testes do Core

Validam:

- conversão entre coordenadas internas e notação de xadrez;
- posição inicial;
- movimentos e alternância de turnos;
- capturas;
- xeque e xeque-mate;
- roque;
- en passant;
- promoção;
- empates;
- histórico de posições;
- relógio;
- concorrência de assentos e operações;
- repositório em memória;
- conversão de DTOs.

### Testes do Server

Usando `WebApplicationFactory` e clientes SignalR reais, validam:

- criação e entrada em partidas;
- descoberta de partidas esperando adversário;
- entrada recusada em partidas cheias;
- abandono e desconexão;
- transmissão de movimentos;
- troca de lados;
- rejeição de jogadas fora da vez;
- mensagens de erro;
- contrato JSON em camelCase;
- entrega do frontend pelo mesmo processo;
- CORS de desenvolvimento;
- criação e validação de sessões;
- rate limiting da negociação e das invocações.

## 13. Decisões técnicas importantes

### Reutilizar o núcleo entre console e web

O console foi a primeira interface do projeto. A aplicação web reutiliza `Board`, `Piece` e `ChessMatch` em vez de duplicar as regras. O console renderiza objetos diretamente; a web usa `BoardMapper` para criar DTOs.

### Manter o Hub fino

O Hub recebe chamadas de transporte e envia eventos. As decisões de assento, turno, concorrência, relógio e validação de entrada ficam no `MatchService` e no `GameSession`.

### Usar estado completo nos broadcasts

Depois de cada movimento, o servidor envia o estado inteiro da partida junto com origem e destino. Isso torna a sincronização mais simples e permite que um cliente que perdeu um evento se recupere com `GetBoardState`.

### Usar lock por partida

Um lock global reduziria a concorrência entre partidas diferentes. Por isso, o lock fica dentro de cada `GameSession`: chamadas da partida A não bloqueiam chamadas da partida B.

### Usar memória como armazenamento inicial

O armazenamento em memória reduz a complexidade da primeira versão e é suficiente para uma instância local. A interface `IMatchRepository` deixa explícito o ponto que precisará ser substituído se o projeto evoluir para persistência ou múltiplas instâncias.

## 14. Pontos de extensão

Os próximos componentes podem ser adicionados sem alterar o núcleo de regras:

- um repositório persistente implementando `IMatchRepository`;
- backplane do SignalR para múltiplas instâncias;
- autenticação real no lugar da sessão anônima;
- histórico persistente de partidas;
- espectadores e permissões de leitura;
- ranking ou matchmaking;
- testes automatizados do frontend;
- empacotamento dos scripts CDN como dependências locais.

## 15. Arquivos principais para manutenção

Para alterar regras de xadrez, comece por:

```text
chess-console.Core/Chess/ChessMatch.cs
chess-console.Core/Chess/*.cs
chess-console.Core/Board/*.cs
```

Para alterar multiplayer, assentos ou relógio:

```text
chess-console.Core/Matches/GameSession.cs
chess-console.Server/Services/MatchService.cs
chess-console.Server/Services/MatchClockService.cs
chess-console.Server/Hubs/ChessHub.cs
```

Para alterar o formato enviado ao navegador:

```text
chess-console.Core/Dto/*.cs
chess-console.Core/Dto/BoardMapper.cs
chess-console.Server/Program.cs
```

Para alterar a interface:

```text
chess-console.Server/wwwroot/index.html
chess-console.Server/wwwroot/js/*.js
chess-console.Server/wwwroot/css/*.css
```

Sempre que uma regra ou contrato for alterado, atualize os testes correspondentes em `chess-console.Core.Tests` ou `chess-console.Server.Tests`.
