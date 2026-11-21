<!-- spectro-managed: true -->
# Tarefas: Pré-validação de estoque antes do envio para produção

| ID | Tarefa | Requisito | Critério de aceite | Dependências | Status |
|---|---|---|---|---|---|
| `TASK-001` | Extrair/reutilizar no domínio a preparação compartilhada de requisitos e disponibilidade, incluindo resultado completo para prévia e causa tipada para falha, sem duplicar a regra do planner. | `FR-002`, `FR-006`, `NFR-001`, `NFR-002`, `CON-001`, `CON-002`, `CON-005` | `AC-001`, `AC-005`, `AC-006`, `AC-010` | — | completed |
| `TASK-002` | Criar o contrato/modelo de aplicação para transportar linhas da prévia, status bloqueante, diagnósticos de configuração e quantidades formatáveis, mantendo entidades e exceções fora da UI. | `FR-002`, `FR-003`, `FR-004`, `NFR-001`, `NFR-004` | `AC-001`, `AC-002`, `AC-003`, `AC-004` | `TASK-001` | completed |
| `TASK-003` | Integrar a prévia ao carregamento de detalhes e implementar a modal Bootstrap em `OrderDetailsPage`, com tabela de requerido/disponível/unidade, destaque vermelho textual/acessível, bloqueio e confirmação antiforgery. | `FR-001`, `FR-003`, `FR-004`, `FR-005`, `NFR-004`, `CON-003`, `CON-004` | `AC-001`, `AC-002`, `AC-003`, `AC-004`, `AC-005` | `TASK-002` | completed |
| `TASK-004` | Ajustar o resultado do envio no controller/serviço para preservar causas conhecidas, mapear mensagens acionáveis, tratar concorrência e exibir correlação segura somente em falha inesperada; manter auditoria segura. | `FR-006`, `FR-007`, `FR-008`, `NFR-002`, `NFR-003`, `CON-005` | `AC-006`, `AC-007`, `AC-008`, `AC-009`, `AC-010` | `TASK-001` | completed |
| `TASK-005` | Adicionar testes de domínio, aplicação, renderização e HTTP para prévia, bloqueios, mensagens, autorização, antiforgery, revalidação, sucesso e ausência de efeitos parciais. | Todos | `AC-001` a `AC-010` | `TASK-003`, `TASK-004` | completed |
| `TASK-006` | Executar revisão final de escopo, segurança e rastreabilidade e os gates obrigatórios, registrando limitações de concorrência real ou cobertura visual quando o ambiente não as suportar. | Todos | `AC-001` a `AC-010` | `TASK-005` | completed |

## Detalhamento das tarefas

### TASK-001

- Componentes afetados: `ProductionPlanner`, serviços de produção/inventário e tipos de resultado no domínio.
- Critérios de conclusão: prévia e planejamento usam a mesma agregação; todos os materiais necessários aparecem, não apenas faltantes; falhas de configuração/estoque/concorrência podem ser diferenciadas; nenhuma persistência ocorre durante a leitura da prévia.
- Testes esperados: unitários de agregação, saldo exato/insuficiente, material compartilhado, produto/ficha inválidos e regressão do planner.
- Risco: médio; extração pode alterar o fluxo já corrigido.

### TASK-002

- Componentes afetados: `OrderDetailViewModel` ou modelo específico de prévia e mapeamento em `OrderQueryService`/serviço de aplicação.
- Critérios de conclusão: modelo é imutável, não expõe entidade/exceção, representa estado bloqueante e permite apresentar nome, unidade, requerido, disponível e falta.
- Testes esperados: mapeamento de sucesso, insuficiência, configuração inválida e falha de consulta.
- Risco: baixo/médio; preservar contrato existente da página de detalhes.

### TASK-003

- Componentes afetados: `src/App/Views/Orders/Details.cshtml` e `src/UI/Components/Orders/OrderDetailsPage.razor`.
- Critérios de conclusão: botão atual abre modal; confirmação fica desabilitada para qualquer bloqueio; insuficiência tem classe/estado vermelho e texto; o formulário usa POST/antiforgery existentes; modal permanece utilizável por teclado e leitor de tela.
- Testes esperados: testes de renderização/componente e teste HTTP do HTML de detalhes.
- Risco: médio; componente é renderizado estaticamente e depende do Bootstrap já carregado.

### TASK-004

- Componentes afetados: `OrdersController`, `OrderProductionPlanningService`, auditoria/contexto de correlação e mensagens TempData.
- Critérios de conclusão: cada causa conhecida recebe mensagem específica e orientação; conflito informa atualização/nova tentativa; falha inesperada não expõe detalhes técnicos e inclui correlação segura; auditoria não registra sucesso falso.
- Testes esperados: mutações HTTP e unidade do mapeamento de falhas/correlação.
- Risco: alto; mudança de `bool` para resultado classificável pode afetar testes e consumidores do serviço.

### TASK-005

- Componentes afetados: `tests/Craftsman.Tests/Inventory`, `Production`, `Application` e `Integration/Http`.
- Critérios de conclusão: todos os critérios de aceite têm ao menos um teste comportamental; cenários negativos confirmam ausência de tarefas/movimentos/status parcial; autorização e antiforgery permanecem protegidos.
- Testes esperados: `dotnet test --no-restore` e cobertura conforme gate.
- Risco: médio; testes de HTML podem exigir adaptação ao componente estático existente.

### TASK-006

- Componentes afetados: artefatos Spectro e evidências de gates.
- Critérios de conclusão: diff focado, revisão manual de segurança concluída, build/test/security/coverage executados conforme configuração; limitações registradas sem alegações não verificadas.
- Testes esperados: comandos definidos em `.spectro/config.yaml`.
- Risco: baixo; gates podem revelar limitação de cobertura ou infraestrutura PostgreSQL.

## Regras de execução

- Não criar migration, tabela de reserva ou alteração de esquema nesta feature.
- Não duplicar cálculo de ficha técnica entre UI, aplicação e domínio.
- Não permitir que a prévia reserve, consuma ou altere estoque.
- Não remover autorização, antiforgery ou validação definitiva do POST.
- Não usar texto de exceção como contrato de mensagem; mapear causas tipadas.
- Não registrar nem exibir stack trace, SQL, credenciais, tokens ou dados pessoais desnecessários.
- Preservar movimentações históricas e o escopo da feature `fix-production-order-stock-validation`.
- Se a concorrência real não puder ser verificada contra PostgreSQL, registrar a limitação nos gates e na entrega.

## Progresso

- Concluídas: `TASK-001`, `TASK-002`, `TASK-003`, `TASK-004`, `TASK-005`, `TASK-REV-001`, `TASK-REV-002`
- Bloqueadas: nenhuma

## Evidências de execução

- Build: `dotnet build src/App/Craftsman.csproj --no-restore --warnaserror -v minimal -m:1 -p:UseSharedCompilation=false` — aprovado, 0 avisos e 0 erros.
- Testes: `dotnet test tests/Craftsman.Tests/Craftsman.Tests.csproj --no-restore -v minimal -m:1 -p:UseSharedCompilation=false` — aprovado, 414 testes.
- Segurança de dependências: `dotnet list src/App/Craftsman.csproj package --vulnerable --include-transitive` — nenhum pacote vulnerável encontrado.
- Cobertura: `coverage-gate.ps1 -AllowDirtyTree` — aprovado na medição local: 88,17% linhas, 73,22% branches, 96,84% linhas novas e 88,24% branches novos. O modo oficial sem `-AllowDirtyTree` não executa com alterações locais não commitadas.
- Revisão manual: fluxo preserva autorização, antiforgery, revalidação no servidor, ausência de consumo na prévia e mensagens sem detalhes técnicos.
- Remediação `FINDING-001`: exceções inesperadas agora retornam mensagem segura com referência, registram log estruturado e têm testes de falha inesperada e falha de auditoria.
- Remediação `FINDING-002`: gate configurado passou com 88,56% linhas, 73,85% branches, 99,57% linhas novas e 92,54% branches novos.

## TASK-REV-001 - Remediate FINDING-001

- **Status:** completed
- **Finding:** FINDING-001
- **Severity:** High
- **Return stage:** implement
- **Description:** Tratar exceções inesperadas no envio para produção com rollback seguro, log estruturado com correlação e mensagem TempData sem detalhes internos; adicionar teste HTTP/controlador.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.
- **Expected tests:** Add or update behavior-focused coverage as required.
## TASK-REV-002 - Remediate FINDING-002

- **Status:** completed
- **Finding:** FINDING-002
- **Severity:** Medium
- **Return stage:** gates
- **Description:** Adicionar cobertura comportamental para os branches novos reportados e executar novamente o gate configurado até atingir New Branch Coverage mínimo de 85%, registrando a evidência.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.
- **Expected tests:** Add or update behavior-focused coverage as required.

