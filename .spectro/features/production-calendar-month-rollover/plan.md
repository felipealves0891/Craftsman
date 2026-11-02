<!-- spectro-managed: true -->
# Plano: Atualização automática do mês na agenda de produção

## Contexto e objetivo técnico

- A agenda é uma view MVC (`src/App/Views/Production/Index.cshtml`) que compõe `ProductionIndexPage` em modo `Static`. O filtro de data é enviado como `plannedDate` para `ProductionController.Index` e `ProductionScheduleService.ListAsync` consulta as tarefas do mês correspondente.
- A tela não tem ciclo interativo depois da renderização inicial. Uma aba aberta conserva o título e os dias originais mesmo após a virada. O objetivo técnico é atualizar o calendário já aberto quando não há `plannedDate`, sem recarregar a página inteira, e preservar o calendário quando há data selecionada.
- Não há ambiguidades pendentes em `clarifications.md`. A decisão Q-001 está consolidada em `FR-001` a `FR-003`.

## Solução proposta

- Usar `TimeProvider` fornecido pelo servidor como fonte testável para a data local atual. No carregamento sem `plannedDate`, definir o mês visível pela data atual do servidor, sem inferi-lo das datas das tarefas.
- Extrair a marcação do calendário da view principal para um partial reutilizável. A view inicial e uma ação GET de atualização devem produzir o mesmo fragmento, usando a lista de tarefas do mês atual e preservando o filtro de status.
- Adicionar uma ação GET pequena que informe o mês atual segundo o mesmo relógio do servidor. Um script carregado apenas na página de produção consulta esse mês periodicamente quando não há data explícita. Quando detectar mudança, solicita o fragmento e o substitui no DOM, sem navegação ou recarga integral.
- Quando `plannedDate` estiver presente, não iniciar a atualização automática. O calendário, a data no formulário e as tarefas permanecem ligados à seleção explícita.
- Evitar requisições sobrepostas. Em falha de consulta, manter o calendário atual e tentar novamente no próximo ciclo ou quando a aba voltar a ficar visível.

## Arquitetura e contratos

- `ProductionController.Index`: continua aceitando `status` e `plannedDate`; entrega ao renderizador o mês de referência atual quando `plannedDate` não existe e a data selecionada quando existe.
- Nova ação GET de mês atual: protegida pela política de leitura já aplicada ao controller; retorna ano/mês atuais do servidor, sem consultar ou alterar persistência.
- Nova ação GET do fragmento: aceita o filtro de status, consulta `ProductionScheduleService.ListAsync(status, plannedDate: null)` e retorna o partial do calendário para o mês corrente. Não deve substituir uma seleção manual.
- Partial de calendário: recebe a coleção de tarefas, mês de referência, status e dados já necessários à renderização estática; expõe um contêiner substituível com identificador estável.
- Script de produção: consulta a ação de mês atual somente na página sem data selecionada; ao detectar mudança, solicita o partial e troca o contêiner. Mantém o filtro de status e não cria timers paralelos.
- `TimeProvider`: registrado na composição da aplicação como `TimeProvider.System` e injetado nos pontos que precisam determinar o mês atual, permitindo testes com relógio fixo.
- Persistência: nenhuma mudança de esquema, entidade, repositório ou dados persistidos.

## Alternativas consideradas

- Recarregar a página inteira na virada: simples, mas contradiz os critérios que exigem atualização sem recarregamento e pode interromper o contexto da pessoa.
- Tornar o componente Razor interativo: amplia a migração de renderização e o ciclo de vida da tela para uma necessidade localizada; a atualização de fragmento preserva o padrão MVC/Razor existente.
- Calcular a virada apenas com o relógio do navegador: pode divergir da data local usada pela aplicação no servidor. A consulta ao mês do servidor mantém uma única referência temporal.

## Riscos e compatibilidade

- O polling gera tráfego enquanto a página sem data explícita estiver aberta. Usar uma resposta mínima para o mês, uma cadência moderada, pausar enquanto a página estiver oculta e verificar novamente ao retornar à aba.
- Se a atualização do fragmento falhar, a tela pode permanecer no mês anterior até a próxima tentativa. Manter o conteúdo atual e repetir a consulta sem interromper o uso da página.
- A troca de HTML deve preservar acessibilidade, ações de tarefas e token antifalsificação já renderizado. A resposta do partial precisa passar pelo mesmo pipeline de Razor e não aceitar conteúdo HTML fornecido pelo cliente.
- O endpoint permanece protegido pela autorização de leitura definida no controller; nenhuma alteração de autorização é prevista.
- Não foi encontrado harness de testes de navegador na suíte. A lógica de referência temporal e os endpoints devem ter testes automatizados; a atualização real no navegador requer validação de aceitação manual, salvo se um harness existente for identificado durante a implementação.
- A ação GET repetida consulta apenas um valor de data, mas a atualização do fragmento busca novamente tarefas e pedidos. Recarregar os dados apenas quando o mês mudar limita esse custo.

## Estratégia de testes

- Testes automatizados com relógio controlado: carregamento sem data selecionada usa o mês atual mesmo quando há tarefas em outros meses ou quando o mês atual não tem tarefas; data explícita continua selecionando seu próprio mês.
- Testes de controller/partial: a consulta do mês atual retorna o mês do `TimeProvider`; o fragmento usa o mês atual, respeita status e não recebe `plannedDate`; as políticas de leitura continuam aplicadas.
- Testes de renderização: o fragmento possui um contêiner substituível e preserva dias, título, estado vazio e ações da agenda.
- Aceitação manual em navegador: manter `/Production` aberta atravessando uma virada simulada/ambiente controlado e confirmar atualização sem recarga; repetir com `plannedDate` explícita e confirmar que calendário e filtro não mudam.
- Executar os gates definidos em `.spectro/config.yaml` após a implementação: build, suíte de testes, segurança de pacotes e cobertura.

## Rastreabilidade

| Requisito | Critério de aceite | Tarefa |
|---|---|---|
| `FR-001` | `AC-001`, `AC-003` | `TASK-001`, `TASK-002`, `TASK-004` |
| `FR-002` | `AC-001`, `AC-002` | `TASK-002`, `TASK-003`, `TASK-004` |
| `FR-003` | `AC-004` | `TASK-001`, `TASK-002`, `TASK-003`, `TASK-004` |
| `NFR-001` | `AC-001` | `TASK-002`, `TASK-003`, `TASK-004` |
| `CON-001` | Todos os critérios aplicáveis | `TASK-002`, `TASK-003` |
| `CON-002` | `AC-002`, `AC-004` | `TASK-001`, `TASK-002`, `TASK-004` |
