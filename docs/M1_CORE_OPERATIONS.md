# M1 — Operação básica

O M1 implementa a base real para condomínio, usuários, unidades e moradores.

## Preservação do M0

O M1 mantém:

- Sentra.Contracts;
- SignalR;
- correlation ID;
- readiness health check;
- WPF e identidade visual;
- auditoria;
- CI existente.

## Autenticação

### Modo local recomendado para V1

Configure:

- `AUTH_MODE=Local`
- `AUTH_ISSUER`
- `AUTH_AUDIENCE`
- `AUTH_SIGNING_KEY` com pelo menos 32 caracteres.

O primeiro administrador é criado por bootstrap protegido por `SENTRA_BOOTSTRAP_TOKEN`.

Não existe usuário padrão, senha padrão ou chave padrão.

### Modo externo

`AUTH_MODE=External` preserva suporte a uma autoridade JWT externa por `AUTHORITY` + `AUTH_AUDIENCE`.

O endpoint de login local não é exposto nesse modo.

## Bootstrap

O bootstrap somente funciona enquanto o banco não possui condomínio/funcionário e exige o header:

`X-Sentra-Bootstrap-Token`

Ele cria:

- condomínio real informado;
- perfil Administrador;
- catálogo explícito de permissões;
- primeiro administrador;
- senha armazenada por PasswordHasher do ASP.NET Core.

## Escopo inicial

- blocos;
- unidades;
- moradores;
- telefone E.164;
- vínculo morador/unidade;
- RBAC básico;
- login local;
- lockout após cinco tentativas inválidas.

O WhatsApp utilizará o telefone E.164 para localizar o morador no M2.
