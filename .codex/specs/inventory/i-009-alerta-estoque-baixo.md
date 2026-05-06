# Especificacao I-009 - Alerta de estoque baixo

## Summary

Criar regras de estoque baixo para materia-prima, com dois niveis de alerta: `Aviso` e `Critico`.

O sistema deve permitir configurar os limites no cadastro/edicao da materia-prima e usar esses limites para alertar o usuario nos fluxos operacionais em que a falta de material impacta a producao ou exige reposicao.

Embora a tarefa mencione "produto", o estoque controlado pelo Inventory Context e consumido pela producao e de materia-prima. Portanto, a regra deve pertencer a `RawMaterial`, e o alerta deve ser calculado pelo saldo interno da materia-prima.

## Objetivo

- Permitir cadastrar e editar quantidade minima e quantidade critica de estoque por materia-prima.
- Classificar o saldo atual de cada materia-prima como `Normal`, `Aviso` ou `Critico`.
- Exibir alerta de `Aviso` ao enviar pedido para producao e no sino de notificacoes.
- Exibir alerta `Critico` ao enviar pedido para producao, na tela de entrada de estoque e no sino de notificacoes.
- Manter o estoque auditavel e calculado a partir de movimentacoes internas.
- Manter a interface sem acesso direto ao `DbContext`.

## Fora de escopo

- Criar estoque de produto acabado.
- Criar compras, solicitacoes de compra ou integracao com fornecedor.
- Bloquear producao somente por estar em nivel de aviso ou critico quando ainda houver material suficiente para o consumo planejado.
- Alterar a regra existente que bloqueia producao por material insuficiente.
- Criar notificacoes persistidas por usuario.
- Enviar alertas por e-mail, WhatsApp ou integracoes externas.
- Criar regras diferentes por origem de pedido.

## Regras de negocio

- Cada materia-prima pode ter uma quantidade minima (`MinimumStockLevel`) e uma quantidade critica (`CriticalStockLevel`).
- Os limites sao opcionais para materia-prima existente, mas quando informados devem ser maiores ou iguais a zero.
- Quando os dois limites forem informados, a quantidade critica deve ser menor ou igual a quantidade minima.
- Se apenas o limite critico for informado, ele deve gerar alerta critico quando o saldo for igual ou menor que esse valor.
- Se apenas o limite minimo for informado, ele deve gerar alerta de aviso quando o saldo for igual ou menor que esse valor.
- O nivel `Critico` tem prioridade sobre `Aviso`.
- Uma materia-prima sem limites configurados nao gera alerta de estoque baixo.
- O saldo usado nos alertas deve vir de `IStockMovementRepository.GetBalanceAsync` ou consulta equivalente baseada nas movimentacoes internas.
- Alertas de estoque baixo nao substituem a validacao de disponibilidade para producao.
- Materias-primas inativas podem aparecer nos alertas quando possuem regra configurada e saldo baixo, mas nao podem ser consumidas em novos planejamentos conforme regra ja existente.

## Modelo de dominio

Atualizar `RawMaterial` em `src/Domains/Inventory/Entities/RawMaterial.cs`:

- Adicionar propriedades:
  - `MinimumStockLevel`: `decimal?`
  - `CriticalStockLevel`: `decimal?`
- Validar limites no construtor e em metodo de atualizacao.
- Adicionar metodo sugerido:
  - `SetLowStockRule(decimal? minimumStockLevel, decimal? criticalStockLevel)`
- Adicionar comportamento de classificacao, se fizer sentido no dominio:
  - `GetStockAlertLevel(decimal currentBalance)`

Adicionar enum no Inventory Context:

```csharp
public enum StockAlertLevel
{
    Normal,
    Warning,
    Critical
}
```

Nomes em UI podem ser exibidos como `Normal`, `Aviso` e `Critico`.

## Aplicacao e UI

Atualizar modelos em `src/App/Models/ManualInventoryModels.cs`:

- `RawMaterialListItemViewModel` deve expor limite minimo, limite critico e nivel atual quando houver saldo disponivel.
- `RawMaterialInputModel` deve permitir informar quantidade minima e quantidade critica.
- Criar view model para alertas do sino, por exemplo `StockAlertNotificationViewModel`, contendo materia-prima, saldo, unidade, nivel e limite atingido.

Atualizar `InventoryAppService`:

- Cadastro e edicao de materia-prima devem salvar os limites de alerta.
- Listagem de materias-primas deve exibir os limites configurados.
- Consulta de saldo deve retornar o nivel de alerta de cada materia-prima.
- Tela de entrada de estoque deve carregar alertas criticos antes do lancamento.
- Alteracoes em materia-prima e movimentacoes de estoque devem invalidar caches relacionados a saldos e alertas.

Atualizar views de inventario:

- `Inventory/EditMaterial.cshtml` deve conter campos de quantidade minima e quantidade critica.
- `Inventory/Index.cshtml` deve exibir os limites configurados.
- `Inventory/Balances.cshtml` deve indicar visualmente `Aviso` e `Critico`.
- `Inventory/Movements.cshtml` ou a tela usada para entrada de estoque deve exibir alerta critico de materias-primas antes ou durante o lancamento de entrada.

Atualizar envio para producao:

- Ao enviar pedido para producao, calcular os materiais da ficha tecnica e consultar o saldo atual.
- Se algum material relacionado ao pedido estiver igual ou abaixo do minimo, exibir alerta de `Aviso`.
- Se algum material relacionado ao pedido estiver igual ou abaixo do critico, exibir alerta `Critico`.
- Se tambem houver material insuficiente para planejar a producao, manter o erro de indisponibilidade existente.
- Os alertas devem informar materia-prima, saldo atual, unidade e limite atingido.

## Sino de notificacoes

Adicionar um item de notificacoes na topbar do layout usando o padrao visual do SB Admin 2 ja presente no projeto.

Comportamento esperado:

- Mostrar contador somente quando houver alertas de estoque baixo.
- Listar materias-primas em `Critico` primeiro e `Aviso` depois.
- Cada item deve mostrar nome da materia-prima, saldo atual, unidade e nivel.
- O clique no item deve levar para a tela de saldos ou para o historico da materia-prima.
- A consulta deve ser feita por servico de aplicacao, partial view ou view component, sem acesso direto ao `DbContext` pela view.
- O sino deve refletir alteracoes de estoque apos invalidacao de cache.

## Persistencia

Atualizar entidade e configuracao EF:

- Adicionar em `RawMaterialEntity`:
  - `MinimumStockLevel`: `decimal?`
  - `CriticalStockLevel`: `decimal?`
- Mapear colunas em `RawMaterialConfiguration`:
  - `minimum_stock_level`
  - `critical_stock_level`
- Criar migration EF para adicionar as colunas como nullable.
- Atualizar `RawMaterialRepository` para mapear os novos campos.
- Manter invalidacao de cache de inventario quando materia-prima for criada ou alterada.

## Servicos e consultas

Criar servico ou consulta de aplicacao para avaliar alertas:

- Entrada: lista opcional de materias-primas ou todas as materias-primas configuradas.
- Saida: colecao ordenada por nivel (`Critico`, `Aviso`) e nome.
- Deve usar repositorios do dominio/infra ja existentes.
- Deve ser cacheavel por curto periodo ou invalidavel por prefixo `inventory:` quando houver movimentacao ou alteracao de regra.

Nomes sugeridos:

- `StockAlertAppService`
- `StockAlertViewModel`
- `StockAlertLevel`

## Criterios de aceitacao

- Usuario consegue cadastrar e editar limite minimo e limite critico no cadastro/edicao de materia-prima.
- Limites negativos sao rejeitados.
- Limite critico maior que limite minimo e rejeitado quando ambos forem informados.
- Quando o saldo estiver igual ou abaixo da quantidade minima, o sistema alerta no envio para producao e no sino de notificacoes.
- Quando o saldo estiver igual ou abaixo da quantidade critica, o sistema alerta no envio para producao, na tela de entrada de estoque e no sino de notificacoes.
- Alertas usam saldo interno de estoque calculado pelas movimentacoes.
- Alerta critico tem prioridade sobre alerta de aviso.
- Producao continua bloqueada quando houver material insuficiente, independentemente do alerta configurado.
- A interface nao acessa `DbContext` diretamente.
- Alteracoes de estoque ou de regra de alerta invalidam as consultas cacheadas relacionadas.

## Test Plan

Criar ou atualizar testes para:

- Dominio:
  - cria materia-prima sem regra de estoque baixo;
  - cria materia-prima com minimo e critico;
  - rejeita limite negativo;
  - rejeita critico maior que minimo;
  - classifica saldo como `Normal`, `Aviso` ou `Critico`;
  - trata saldo igual ao limite como alerta.
- Aplicacao:
  - cadastro manual salva minimo e critico;
  - edicao atualiza minimo e critico;
  - listagem retorna limites configurados;
  - consulta de alertas retorna criticos antes de avisos;
  - tela de entrada carrega apenas alertas criticos;
  - envio para producao inclui alertas de materiais da ficha tecnica.
- Persistencia:
  - repositorio salva e carrega limites de alerta;
  - migration adiciona colunas nullable;
  - cache de inventario e invalidado ao alterar materia-prima e ao registrar movimentacao.
- Razor views:
  - cadastro/edicao de materia-prima contem campos de minimo e critico;
  - saldos exibem estado de aviso e critico;
  - layout contem sino de notificacoes com contador quando existem alertas;
  - tela de entrada exibe alerta critico.
- Integracao:
  - fluxo de envio para producao mostra alerta de aviso/critico sem bloquear quando ha saldo suficiente;
  - fluxo de envio para producao continua bloqueando quando ha material insuficiente.

Rodar ao final:

```powershell
$env:DOTNET_CLI_HOME='D:\Source\Repos\Dotnet\Craftsman\.dotnet-home\.dotnet'
dotnet build Craftsman.slnx -v:minimal /p:UseSharedCompilation=false /nr:false
dotnet test Craftsman.slnx --no-build -v:minimal
```

## Checklist de conclusao

- [ ] Dominio de materia-prima atualizado com regra de estoque baixo.
- [ ] Aplicacao e view models atualizados.
- [ ] Views de materia-prima, saldo e entrada de estoque atualizadas.
- [ ] Sino de notificacoes implementado na topbar.
- [ ] Fluxo de envio para producao exibe alertas de estoque baixo.
- [ ] Persistencia, repositorio e migration atualizados.
- [ ] Cache de inventario invalidado nos pontos necessarios.
- [ ] Testes unitarios, de aplicacao, persistencia, Razor e integracao criados ou atualizados.
- [ ] Build executado.
- [ ] Testes executados.
- [ ] I-009 marcada como concluida em `.codex/tasks/inventory/tasks.md` apos implementacao.
