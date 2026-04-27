# Bug Fix Tasks

## BUG-FIX-001 - Corrigir avanço de tarefa de produção
- [ ] Corrigir conflito de tracking do EF ao iniciar, concluir ou cancelar tarefa de produção.
- [ ] Adicionar teste automatizado cobrindo atualização após carregar a tarefa no mesmo contexto.

Critérios de aceitação:
- Iniciar, concluir ou cancelar uma tarefa de produção não deve gerar conflito de entidade já rastreada.
- A alteração de status e datas deve ser persistida corretamente.
