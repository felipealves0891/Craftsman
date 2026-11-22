<!-- spectro-managed: true -->
# Revisão: Exibir quebra por produto e estoque dos materiais no envio para produção

## Resumo

- Revisão independente da implementação do commit `681e6e9e234c21cef2978b5fe03f9280170058ce`.
- A implementação está alinhada ao objetivo aprovado: detalhe por produto em lista expansível, sublista de materiais e resumo consolidado por matéria-prima.
- Os quatro gates obrigatórios passaram e a evidência pertence à mesma revisão de código.
- Decisão: `approved`.

## Artefatos examinados

- `spec.md`, `clarifications.md`, `plan.md`, `tasks.md`.
- `gates.json` e evidências da execução `gate-20261122T175527Z`.
- Pacote independente `reviews/review-20261122T175911Z/`, incluindo `manifest.json`, `review-packet.md`, `diff.patch` e `traceability.json`.
- Constituição e configuração Spectro.
- Implementação em `ProductionPreflightService`, modelos/projeção de aplicação, `Details.cshtml` e `OrderDetailsPage.razor`.
- Testes de preflight, componente e integração de pedidos/produção.

## Consistência da revisão

- `gates.json.code_revision` corresponde ao `HEAD` revisado: `681e6e9e234c21cef2978b5fe03f9280170058ce`.
- `gates.json` foi validado pelo contrato Spectro.
- Build: aprovado, sem avisos.
- Testes: 427 aprovados, 0 falhas, 0 ignorados.
- Segurança de pacotes: nenhum pacote vulnerável identificado.
- Cobertura: 88,66% de linhas, 73,88% de branches, 100% de linhas novas e 100% de branches novos.

## Requisitos e comportamento verificados

- `FR-001`–`FR-004`: o domínio transporta grupos por produto e materiais com necessidade, estoque global, unidade e situação.
- `FR-005`–`FR-008`: `CanConfirm` permanece a fonte da habilitação; a UI mantém uma confirmação única e exibe o resumo consolidado sem recalcular estoque.
- `NFR-001`: o detalhe por produto é enriquecido a partir do índice de disponibilidade agregada, sem consulta ou saldo independente por produto.
- `NFR-002`–`NFR-003`: a UI usa elementos expansíveis, tabelas responsivas e valores decimais/unidades existentes.
- `NFR-004`: endpoint, autorização e antiforgery não foram alterados; testes de integração continuam cobrindo POST sem antiforgery.
- `CON-001`–`CON-005`: não houve mudança de esquema, dependência, persistência, consumo ou regra de disponibilidade.
- `AC-001`–`AC-011`: todos aparecem com validação no plano/tarefas e os cenários principais têm cobertura nos testes de domínio, componente e integração.

## Achados

### FINDING-001

- Severidade: `Low`
- Categoria: `consistency`
- Localização: `tasks.md`, linha da tabela de tarefas; `traceability.json`.
- Evidência: `validate_traceability.ps1` retornou `uncovered_requirements: ["CON-004"]`. A restrição exige justificar/evitar migration, dependência ou pacote novo, mas não está vinculada a uma tarefa específica.
- Impacto: a rastreabilidade automatizada fica incompleta para essa restrição, embora a implementação não tenha introduzido migration, dependência ou pacote novo e as regras de execução registrem essa limitação.
- Recomendação: associar `CON-004` a `TASK-004` ou registrar explicitamente sua validação no plano/tarefa em uma revisão documental futura.
- Estágio de retorno: `plan`
- Status: não bloqueia a aprovação da implementação; não requer correção de código.

## Segurança

- Superfície revisada: somente apresentação e transporte interno de dados do preflight; nenhum endpoint, entrada externa, permissão ou persistência nova.
- Controles preservados: autorização de escrita, antiforgery, decisão única por pedido e bloqueio quando `CanConfirm` é falso.
- Verificações: revisão estática, testes de integração e gate de vulnerabilidades de pacotes.
- Achados críticos/altos: nenhum.
- Limitação: os gates não substituem análise humana, threat modeling ou pentest.

## Riscos residuais

- O estoque global é repetido nas sublistas por produto; a UI o rotula como compartilhado e oferece o resumo consolidado para evitar interpretação como saldo aditivo.
- A revisão não mediu performance em pedidos excepcionalmente grandes; não há evidência atual de regressão e o uso de uma única consulta agregada reduz o risco de N+1 de disponibilidade.

## Decisão final

`approved`

A implementação pode avançar para entrega. O único achado é documental, de baixa severidade e não altera o comportamento nem exige retorno à implementação.
