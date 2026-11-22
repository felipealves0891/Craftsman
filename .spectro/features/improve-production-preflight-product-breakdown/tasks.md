<!-- spectro-managed: true -->
# Tarefas: Exibir quebra por produto e estoque dos materiais no envio para produção

| ID | Tarefa | Requisito | Critério de aceite | Dependências | Status |
|---|---|---|---|---|---|
| `TASK-001` | Estender o resultado do preflight para preservar grupos por produto e materiais consolidados, associando os detalhes aos valores de disponibilidade agregada sem novas consultas ou recálculo por produto. | `FR-001`, `FR-002`, `FR-003`, `FR-004`, `FR-006`, `FR-008`, `NFR-001`, `CON-002`, `CON-005` | `AC-001`, `AC-002`, `AC-003`, `AC-004`, `AC-005`, `AC-006`, `AC-011` | — | completed |
| `TASK-002` | Atualizar os modelos de aplicação e a projeção de `OrderQueryService` para transportar nome/quantidade do produto, materiais por produto e resumo consolidado, mantendo mensagens de falha e `CanConfirm`. | `FR-002`, `FR-003`, `FR-004`, `FR-005`, `FR-007`, `FR-008`, `NFR-001`, `CON-001` | `AC-001`, `AC-002`, `AC-004`, `AC-005`, `AC-006`, `AC-008`, `AC-011` | `TASK-001` | completed |
| `TASK-003` | Alterar `OrderDetailsPage.razor` e o adaptador Razor da página para renderizar lista expansível de produtos, sublistas de materiais, resumo consolidado global, estados de erro e confirmação única protegida. | `FR-001`, `FR-002`, `FR-003`, `FR-004`, `FR-005`, `FR-006`, `FR-007`, `FR-008`, `NFR-002`, `NFR-003`, `NFR-004`, `CON-003` | `AC-001`–`AC-011` | `TASK-002` | completed |
| `TASK-004` | Adicionar/ajustar testes de domínio, aplicação, componente e integração para os cenários de sucesso, insuficiência, configuração inválida, material compartilhado, responsividade estrutural, antiforgery e preservação do envio. | Todos os requisitos aplicáveis | `AC-001`–`AC-011` | `TASK-001`, `TASK-002`, `TASK-003` | completed |

## Regras de execução

- Não alterar o endpoint `Orders/SendToProduction`, sua autorização, antiforgery, transação ou consumo de estoque.
- Não criar migration, dependência, reserva ou nova regra de disponibilidade.
- Usar a disponibilidade agregada do pedido como fonte única; valores por produto são necessidades derivadas da ficha técnica e não saldos independentes.
- Manter a confirmação única do pedido e bloquear o botão sempre que `CanConfirm` for falso.
- Atualizar os testes existentes que constroem `ProductionPreflightRow`/`ProductionPreflightMaterialRow` e adicionar cenários comportamentais, evitando asserts acoplados a detalhes irrelevantes de HTML.
- Executar os gates configurados somente após a implementação; este plano não executa comandos de build ou teste.

## Progresso

- Concluídas: `TASK-001`, `TASK-002`, `TASK-003`, `TASK-004`
- Bloqueadas: —
