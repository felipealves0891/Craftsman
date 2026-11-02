<!-- spectro-managed: true -->
# Especificação: Atualização automática do mês na agenda de produção

## Problema e objetivo

- Problema: A agenda de produção não avança automaticamente para o mês atual quando o mês muda. A pessoa pode continuar vendo o mês anterior até atualizar ou navegar manualmente.
- Objetivo: Manter a agenda de produção sem data selecionada alinhada ao mês atual assim que ocorrer a virada, sem exigir atualização manual da página, preservando uma data escolhida explicitamente.

## Escopo

### Incluído

- Atualizar automaticamente a agenda de produção aberta para o mês atual quando nenhuma data tiver sido selecionada.
- Preservar a data escolhida explicitamente e exibir seu mês e as tarefas correspondentes, mesmo após a virada do mês atual.

### Excluído

- Alterar regras de planejamento, status ou duração de tarefas de produção.
- Alterar as regras de seleção manual de data, além de preservar a seleção existente durante a virada do mês.

## Requisitos

### Requisitos funcionais

- `FR-001`: Quando nenhuma data estiver selecionada, a agenda deve apresentar o mês atual ao carregar e acompanhá-lo automaticamente enquanto permanecer aberta, sem exigir recarregamento ou navegação manual.
- `FR-002`: Após a virada do mês, se nenhuma data estiver selecionada, o título, os dias exibidos e as tarefas da agenda devem corresponder ao novo mês atual.
- `FR-003`: Quando uma data for selecionada explicitamente, a agenda deve preservar essa data e continuar exibindo o mês e as tarefas correspondentes, independentemente da virada do mês atual.

### Requisitos não funcionais

- `NFR-001`: A mudança deve ocorrer assim que o mês local atual virar, sem atraso perceptível para a pessoa que mantém a tela aberta. Para esta funcionalidade, mês local segue a data local usada pela aplicação (`DateTime.Today`).

### Restrições

- `CON-001`: A especificação não determina mecanismo ou arquitetura para detectar a virada do mês.
- `CON-002`: As regras existentes de planejamento e estado das tarefas devem ser preservadas.

## Critérios de aceite

- `AC-001`: Dada a agenda de produção aberta sem data selecionada antes do último dia do mês, quando o mês local virar, então a tela avança para o novo mês sem recarregamento ou ação manual.
- `AC-002`: Dada a agenda sem data selecionada após a virada, quando a pessoa consultar a tela, então o título e os dias do calendário representam o novo mês e a lista de tarefas corresponde a esse mês.
- `AC-003`: Dada uma visita à agenda de produção iniciada após a virada do mês sem data selecionada, quando a tela carregar, então ela apresenta o mês atual, independentemente das datas das tarefas cadastradas.
- `AC-004`: Dada a agenda aberta com uma data selecionada explicitamente, quando o mês atual virar, então a data selecionada permanece e a agenda continua mostrando o mês e as tarefas correspondentes à seleção.

## Riscos, dependências e perguntas abertas

- Dependência: A agenda de produção existente e sua consulta de tarefas por mês.
