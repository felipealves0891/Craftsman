# Security Tasks

## AUTH-AUDIT-V1 - Autenticacao, autorizacao e auditoria

- [x] Adicionar Identity com `ApplicationUser : IdentityUser<int>` e roles `Admin`, `Operador` e `Consulta`.
- [x] Configurar cookie MVC, login, logout, acesso negado e exigencia de usuario autenticado por padrao.
- [x] Criar seed de roles e usuario admin inicial a partir de configuracao.
- [x] Implementar policies de leitura, escrita operacional e administracao.
- [x] Proteger controllers atuais, priorizando bloqueio de POST para `Consulta`.
- [x] Implementar gestao minima de usuarios restrita a `Admin`.
- [x] Criar `audit_logs` e auditoria de `SaveChangesAsync` para Added, Modified e Deleted.
- [x] Redigir campos sensiveis nos JSONs de auditoria.
- [x] Registrar eventos criticos nao-CRUD de autenticacao e operacao.
- [x] Enriquecer `domain_events` com usuario e correlacao.
- [x] Atualizar layout para usuario logado, role principal e logout.
- [x] Criar testes automatizados cobrindo criterios de aceitacao.
- [ ] Rodar build e testes com `DOTNET_CLI_HOME`.

Critérios de aceitacao:
- Usuario anonimo e redirecionado para login em tela protegida.
- Admin inicial e criado automaticamente quando ausente.
- Consulta visualiza telas e nao executa acoes mutaveis.
- Operador executa POST operacional.
- Somente Admin acessa gestao de usuarios.
- CRUD persistido gera `audit_logs`.
- `AuditLogEntity` nao audita a si mesma.
- Eventos de autenticacao geram auditoria.
- `domain_events` recebe ator/correlacao.
- JWT permanece fora do v1.
