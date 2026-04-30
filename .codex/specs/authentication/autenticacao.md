# Autenticação, Autorização e Auditoria v1

## Resumo

Implementar autenticação com ASP.NET Core Identity usando cookie, autorização por perfis simples e auditoria persistida de alterações de
entidade com usuário/ator, data, correlação e valores antes/depois em JSON.

Antes da implementação, criar a spec .codex/specs/security/auth-audit-v1.md e a task list .codex/tasks/security/tasks.md com critérios de
aceitação. Ao concluir cada tarefa, marcar como concluída.

## Mudanças Principais

- Adicionar Identity ao AppDbContext, usando ApplicationUser : IdentityUser<int> e IdentityRole<int>.
- Configurar login/logout por cookie em MVC, com AccountController e views Razor.
- Exigir usuário autenticado por padrão em todas as telas, exceto login, logout, acesso negado e arquivos estáticos.
- Criar roles iniciais:
    - Admin: acesso total, gestão de usuários e permissões.
    - Operador: pode executar fluxos operacionais e alterar dados.
    - Consulta: somente leitura.
- Criar seed obrigatório de roles e usuário admin inicial quando não existir, usando credenciais vindas de configuração segura.
- Não implementar JWT nesta etapa; deixar registrado na spec como evolução futura para API externa/SPA/mobile.

## Auditoria

- Criar tabela audit_logs com:
    - id, occurred_at, user_id, user_name, role_names, action, entity_name, entity_id, correlation_id, request_path, before_json, after_json.
- Sobrescrever SaveChangesAsync no AppDbContext para auditar Added, Modified e Deleted.
- Não auditar a própria tabela de auditoria.
- Redigir campos sensíveis no JSON, incluindo PasswordHash, security stamps, tokens, refresh tokens e credenciais protegidas.
- Adicionar ICurrentUserContext para a Infra obter usuário atual sem acoplar domínio ao ASP.NET.
- Registrar eventos críticos não-CRUD via serviço de auditoria:
    - login com sucesso;
    - login inválido;
    - logout;
    - acesso negado;
    - importação manual;
    - envio manual para produção;
    - avanço/cancelamento/conclusão de produção;
    - atualização de status de envio;
    - criação/edição de usuários e roles.
- Manter os eventos de domínio existentes, mas enriquecer domain_events com user_id, user_name e correlation_id para rastrear o ator do evento
de negócio.

## Autorização

- Leituras ficam disponíveis para Admin, Operador e Consulta.
- Ações mutáveis ficam restritas a Admin e Operador.
- Gestão de usuários/roles fica restrita a Admin.
- Aplicar policies/attributes nos controllers atuais, priorizando proteção dos POST.
- Atualizar layout para mostrar usuário logado, role principal e opção de logout.

## Critérios de Aceitação

- Usuário não autenticado é redirecionado para login ao acessar qualquer tela protegida.
- Admin inicial é criado automaticamente quando não existe, usando configuração.
- Consulta consegue visualizar telas, mas não consegue executar ações mutáveis.
- Toda criação, alteração e exclusão persistida gera registro em audit_logs.
- Eventos de domínio persistidos carregam o ator/correlação quando houver usuário autenticado.
- JWT fica fora do v1 e documentado como evolução futura.

## Testes

- Unitários para CurrentUserContext e redaction de campos sensíveis.
- Integração EF para auditar insert/update/delete com usuário autenticado.
- Integração EF garantindo que AuditLogEntity não audita a si mesma.
- Testes de autorização dos controllers:
    - anônimo redireciona para login;
    - Consulta recebe forbidden em POST;
    - Operador executa POST operacional;
    - somente Admin acessa gestão de usuários.
- Testes para login válido, login inválido, logout e acesso negado gerando auditoria.
- Testes para DomainEventPersistenceHandler persistir usuário/correlação.
- Rodar:
    - DOTNET_CLI_HOME=D:\Source\Repos\Dotnet\Craftsman\.dotnet-home\.dotnet dotnet build Craftsman.slnx
    - DOTNET_CLI_HOME=D:\Source\Repos\Dotnet\Craftsman\.dotnet-home\.dotnet dotnet test Craftsman.slnx

## Assumptions

- O v1 usa cookie + Identity, sem JWT.
- O admin inicial será criado em qualquer ambiente quando ausente, mas senha/email devem vir de configuração segura.
- Auditoria guarda antes/depois em JSON, com redaction obrigatória para segredos.
- O domínio permanece sem dependência de ASP.NET Identity ou HttpContext.