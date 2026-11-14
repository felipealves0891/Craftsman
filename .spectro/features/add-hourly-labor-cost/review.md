<!-- spectro-managed: true -->
# Revisão: Exibir mão de obra na tabela sem incorporá-la aos custos

## Resumo

- Decisão: `changes_requested`
- Código revisado: `0e4ee96842153b9756f7a889327b462401889638`
- Base da revisão: `develop`
- Pacote independente: `.spectro/features/add-hourly-labor-cost/reviews/review-20261114T194721Z/`
- Rastreabilidade: completa, sem requisitos ou critérios de aceite descobertos.
- Gates obrigatórios: build, testes, segurança e cobertura aprovados no `gate-20261114T194617Z`.

A implementação atende ao comportamento principal: o valor monetário de mão de obra continua disponível, mas não compõe custo de produção, custo total, margem ou cards; a tabela financeira apresenta a nova coluna. Os achados abaixo precisam ser resolvidos antes da aprovação final.

## Artefatos examinados

- `.spectro/features/add-hourly-labor-cost/spec.md`
- `.spectro/features/add-hourly-labor-cost/clarifications.md`
- `.spectro/features/add-hourly-labor-cost/plan.md`
- `.spectro/features/add-hourly-labor-cost/tasks.md`
- `.spectro/features/add-hourly-labor-cost/gates.json`
- `.spectro/features/add-hourly-labor-cost/reviews/review-20261114T194721Z/manifest.json`
- `.spectro/features/add-hourly-labor-cost/reviews/review-20261114T194721Z/traceability.json`
- Diff `develop..HEAD` e arquivos de domínio, aplicação, infraestrutura, UI e testes relacionados à feature.

## Requisitos verificados

- `FR-001` e `FR-002`: novos acertos usam custo material em `ProductionCostAmount`; `LaborCostAmount` permanece informativo.
- `FR-003`: cards continuam baseados no resumo de custo/margem e não recebem a mão de obra.
- `FR-004`: a listagem expõe `LaborCostAmount` e a tabela renderiza a coluna monetária.
- `NFR-001` e `NFR-002`: formatação monetária e separação entre domínio, aplicação e apresentação foram preservadas.
- `CON-001` a `CON-004`: não houve remoção de cadastro/horas, nem alteração de receita, desconto, envio ou arquitetura.
- `AC-001` a `AC-006`: há cobertura de cálculo, reconciliação, projeção e renderização; a distinção entre ausência e zero permanece pendente conforme `FINDING-002`.

## Achados

### `FINDING-001` — Migration pode normalizar acertos novos em implantação fora de ordem

- Severidade: `High`
- Categoria: Functional / Persistence
- Localização: `src/Infra/Persistence/Migrations/20261114120000_RevertLaborCostFromFinancialTotals.cs:14-19`
- Evidência: a migration subtrai `labor_cost_amount` de todo registro com `labor_cost_reconciled = TRUE` e valor positivo. Acertos criados pela regra nova também podem satisfazer essa condição, pois continuam persistindo o valor monetário e o marcador como reconciliado.
- Impacto: se a migration for aplicada depois de a versão nova começar a criar acertos, ela pode reduzir indevidamente o custo material, o custo total e a margem desses acertos.
- Recomendação: adicionar um marcador explícito ou uma condição baseada em dados inequívocos que diferencie registros produzidos pela regra antiga; cobrir a ordem migration/deploy e rollback com teste de persistência. Não depender somente da ordem operacional de implantação.
- Return stage: `implement`
- Remediação: `TASK-REV-001`.

### `FINDING-002` — A tabela não distingue ausência de valor monetário de valor zero

- Severidade: `Medium`
- Categoria: Functional / UI
- Localização: `src/App/Models/FinancialSettlementListItemViewModel.cs:10`, `src/UI/Components/Finance/FinanceIndexPage.razor:85`
- Evidência: `LaborCostAmount` é `decimal` obrigatório e a célula sempre renderiza seu valor formatado. Assim, registros sem valor disponível aparecem como `R$ 0,00`, enquanto `AC-005` exige o estado vazio adotado pela tabela.
- Impacto: o usuário não consegue diferenciar uma mão de obra monetária realmente igual a zero de uma informação ausente ou ainda não reconciliada.
- Recomendação: representar ausência explicitamente no contrato de listagem e renderizar o placeholder padrão, mantendo zero como valor monetário válido; adicionar testes para ambos os casos.
- Return stage: `implement`
- Remediação: `TASK-REV-002`.

## Testes e gates

- Suíte completa: 361 testes aprovados.
- Build com `--warnaserror`: aprovado.
- Segurança de pacotes: aprovado.
- Cobertura: aprovada pelos limiares configurados.
- Evidências: `.spectro/features/add-hourly-labor-cost/outputs/gate-20261114T194617Z/evidence/`.
- Relatório: `.spectro/features/add-hourly-labor-cost/gates.json`.

## Riscos residuais

- A migration histórica depende atualmente de uma classificação implícita dos registros antigos; o risco está formalizado em `FINDING-001`.
- A semântica de valor zero versus ausência precisa ser definida no contrato da listagem; o risco está formalizado em `FINDING-002`.
- Não foram observados achados críticos de segurança, cobertura ou separação arquitetural.

## Próximo estágio

Retornar a `spectro-implement` para executar `TASK-REV-001` e `TASK-REV-002`, renovar os gates e executar nova revisão. A aprovação final fica bloqueada enquanto `FINDING-001` permanecer não resolvido.
