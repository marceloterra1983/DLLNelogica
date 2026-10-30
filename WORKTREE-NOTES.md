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
