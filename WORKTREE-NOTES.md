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
