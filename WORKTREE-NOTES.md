# improve/limitada-sem-abortar

Plan 001 (`plans/001-referencia-viva-nao-aborta-sequencia.md`): feito neste worktree.
`plans/README.md` não foi editado; o revisor mantém o índice.

- `LatestPrice` só devolve preço com pelo menos duas amostras e idade de no máximo 30 segundos. O contador não é zerado em `TakeInterval`.
- Sem referência viva, ou com preço alinhado inválido, aquele passo limitado não é enviado e os seguintes continuam. Uma frase fixa por caso; sem cotação, senha ou texto de exceção.
- Falha de `Send` continua no `catch` geral (`sequência interrompida por falha`).
- Caminho feliz preservado: referência 1003, tick 5, offset 10 → compra limitada 990 e venda limitada 1015.
- Os três testes novos são `[Theory]` com um `InlineData`. `[Fact]` passava o acoplamento de `OrderSequenceTests` de 30 para 31 (`CA1506`); o teto não foi aumentado.

Verificação, as duas com `--configuration Release -p:Platform=x64`:

- filtro `OrderSequenceTests|OrderConcurrencyTests`: exit 0, 9 passed
- suíte inteira: exit 0, 250 passed

Arquivos: `src/MarketData/MarketDataSnapshot.cs`, `src/Orders/OrderSequence.cs`, `tests/DLLNelogica.Tests/OrderConcurrencyTests.cs`, `tests/DLLNelogica.Tests/OrderSequenceTests.cs`, o bullet do preço de referência em `README.md`, e este arquivo.

## Plan 002

Plan 002 (`plans/002-exclusividade-gravador-relatorio.md`): feito neste worktree.
`plans/README.md` não foi editado; o revisor mantém o índice. STOP do passo 1 não disparou.

- Antes de mudar produção, `SecondWriterDoesNotAppendAndReportsOnce` falhou no código atual: a segunda escrita entrou em `_Sessao.txt` (`segunda` depois de `primeira`). Exit 1.
- `ExclusiveAppendStream` foi para `src/Logging/ExclusiveAppendStream.cs` (namespace `DLLNelogica.Logging`), corpo idêntico. `TradeFileSystem.OpenAppend` continua chamando `ExclusiveAppendStream.Open`. `ReportFileWriter.GetOrOpen` abre por esse helper. `HandleWriteFailure` não mudou.
- `rg -n "class ExclusiveAppendStream" src` encontra uma definição, em `src/Logging/ExclusiveAppendStream.cs`. `ReportFileWriter.cs` não abre mais `FileStream` com `FileShare.Read`.

Verificação, as três com `--configuration Release -p:Platform=x64`:

- filtro `TradeFileOutputTests`: exit 0, 3 passed
- filtro `ReportWriterExclusionTests|TradeFileOutputTests`: exit 0, 4 passed
- suíte inteira: exit 0, 251 passed

Arquivos: `src/Logging/ExclusiveAppendStream.cs`, `src/Logging/ReportFileWriter.cs`, `src/TimesAndTrades/ITradeStorage.cs`, `tests/DLLNelogica.Tests/ReportWriterExclusionTests.cs`, e este arquivo.

## Plan 003

Plan 003 (`plans/003-caracterizar-maquina-de-conexao.md`): feito neste worktree.
`plans/README.md` não foi editado; o revisor mantém o índice.

- Drift `45e0db5..HEAD` de `src/Connection/ConnectionStateMachine.cs` vazio. As frases do plano 001 no README ficaram; só o bullet `Os estados oscilam` mudou.
- Login, roteamento e market data 4 travam. Ativação é reatribuída. Market data 5 e 6 não apagam um 4 e não completam a espera sozinhos. Login 2 antes de conectar falha com a mensagem já existente; depois dos quatro sucessos, não desfaz o login.
- Nenhum `.cs` de produção foi editado.

Verificação, as duas com `--configuration Release -p:Platform=x64`:

- filtro `FullyQualifiedName~ConnectionStateMachineTests`: exit 0, 8 passed
- suíte inteira: exit 0, 259 passed
- `git diff --stat -- src/Connection/ConnectionStateMachine.cs` não imprime nada

Arquivos: `tests/DLLNelogica.Tests/ConnectionStateMachineTests.cs`, o bullet dos estados em `README.md`, e este arquivo.

## Plan 004

Plan 004 (`plans/004-ci-release-x64-ubuntu.md`): feito neste worktree.
`plans/README.md` não foi editado; o revisor mantém o índice. Drift `45e0db5..HEAD` só em `README.md` (planos 001 e 003); os três comandos de "Como executar" ainda batiam com o plano. Não houve STOP.

- `.github/workflows/ci.yml`: job `test` em `ubuntu-latest`, `actions/checkout@v4`, `actions/setup-dotnet@v4` com `dotnet-version: 9.0.x`, e `dotnet test DLLNelogica.sln --configuration Release -p:Platform=x64 --nologo`. Sem `permissions`, job Windows, cobertura ou badge.
- Os três comandos do README ganharam `-p:Platform=x64`. Uma frase diz que a solução só configura x64.
- `TradeAbiTests.LayoutMatchesOfficialWin64Contract` continua com `return` quando `!OperatingSystem.IsWindows()`. A ProfitDLL não foi carregada no CI nem baixada de novo.
- SDK local é 10.0.113. O YAML não foi trocado para `10.x`. Não há run do GitHub Actions.

Verificação:

- `dotnet test DLLNelogica.sln --configuration Release -p:Platform=x64 --nologo`: exit 0, 259 passed
- `dotnet build DLLNelogica.sln --configuration Release -p:Platform=x64 --nologo`: exit 0
- `dotnet test DLLNelogica.sln --configuration Release -p:Platform=x64 --no-build --nologo`: exit 0, 259 passed
- `dotnet build DLLNelogica.sln --configuration Release --nologo` sem plataforma: exit 0 (não foi MSB4126). A propriedade permanece no README e no YAML.
- `rg -n "net10" .github README.md`: nenhuma ocorrência

Arquivos: `.github/workflows/ci.yml`, os três comandos e a frase x64 em `README.md`, e este arquivo.
