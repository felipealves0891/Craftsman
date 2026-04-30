# Autenticacao, Autorizacao e Auditoria v1

## Escopo

Implementar autenticacao MVC com ASP.NET Core Identity e cookie, autorizacao por roles simples e auditoria persistida de alteracoes de entidades e eventos criticos da aplicacao.

JWT fica fora do v1 e deve ser tratado como evolucao futura para API externa, SPA ou mobile.

## Roles

- Admin: acesso total, incluindo gestao de usuarios e roles.
- Operador: pode executar fluxos operacionais e alterar dados.
- Consulta: acesso somente leitura.

## Auditoria

Persistir registros em `audit_logs` com ator, data, correlacao, caminho da requisicao, acao, entidade, identificador e valores antes/depois em JSON.

Campos sensiveis devem ser redigidos no JSON, incluindo hashes de senha, stamps de seguranca, tokens, refresh tokens e credenciais protegidas.

`audit_logs` nao deve auditar a si mesma.

## Domain Events

Eventos persistidos em `domain_events` devem receber `user_id`, `user_name` e `correlation_id` quando houver contexto autenticado.

## Criterios de Aceitacao

- Usuario anonimo e redirecionado para login ao acessar telas protegidas.
- Admin inicial e criado automaticamente quando ausente, usando configuracao segura.
- Consulta consegue visualizar telas e recebe forbidden em acoes mutaveis.
- Operador consegue executar POST operacional.
- Somente Admin acessa gestao de usuarios.
- Toda criacao, alteracao e exclusao persistida gera registro em `audit_logs`.
- `AuditLogEntity` nao audita a si mesma.
- Login valido, login invalido, logout e acesso negado geram auditoria.
- Eventos de dominio persistidos carregam ator e correlacao quando houver usuario autenticado.
- JWT nao e implementado no v1.

## Testes

- Unitarios para `CurrentUserContext` e redaction de campos sensiveis.
- Integracao EF para auditar insert/update/delete com usuario autenticado.
- Integracao EF garantindo que `AuditLogEntity` nao audita a si mesma.
- Testes de autorizacao para anonimo, Consulta, Operador e Admin.
- Testes para auditoria de login valido, login invalido, logout e acesso negado.
- Teste para `DomainEventPersistenceHandler` persistir usuario/correlacao.
