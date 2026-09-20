# SENTRA

**Central Inteligente de Portaria**

**Desenvolvido por Samuel Sanches**

© 2026 Samuel Sanches. Todos os direitos reservados.

## Estado atual

M0 e M1 possuem base implementada e validada por CI. O marco em desenvolvimento é **M2 — WhatsApp Cloud API oficial**.

A solução contém:

- .NET 10 LTS;
- API ASP.NET Core;
- Windows WPF;
- PostgreSQL + EF Core;
- autenticação local JWT e RBAC;
- condomínio, blocos, unidades e moradores;
- SignalR;
- auditoria e correlation ID;
- adaptador oficial da Meta WhatsApp Cloud API;
- webhook assinado, inbox durável e processamento em background;
- testes automatizados e migrations validadas em PostgreSQL.

A integração Meta permanece **AGUARDANDO CONFIGURAÇÃO** até existir credencial oficial real e o endpoint de verificação concluir chamadas reais à Meta.

Inteligência, operação completa de portaria, offline e instalador ainda não são declarados concluídos.

## Princípios

- O banco é a fonte da verdade operacional.
- Inteligência interpreta linguagem; não define fatos.
- Ações críticas exigem regra explícita e controle humano.
- Segredos ficam no servidor.
- Nenhuma integração aparece como conectada sem teste real.
- O cliente Windows permanece leve.
- Operações externas incertas não recebem retry cego.

## Build

```powershell
dotnet restore Sentra.sln
dotnet build Sentra.sln --configuration Release --no-restore
dotnet test Sentra.sln --configuration Release --no-build
```

## Documentação

Consulte:

- `docs/ARCHITECTURE.md`
- `docs/SECURITY.md`
- `docs/M1_CORE_OPERATIONS.md`
- `docs/WHATSAPP.md`
- `docs/V1_VALIDATION.md`

## Roadmap

M0 foundation → M1 operação básica → **M2 WhatsApp oficial** → M3 inteligência → M4 portaria → M5 operação → M6 offline → M7 UX → M8 instalador → M9 release candidate.

A versão 1.0.0 somente será declarada concluída após validação real.
