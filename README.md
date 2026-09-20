# SENTRA

**Central Inteligente de Portaria**

**Desenvolvido por Samuel Sanches**

© 2026 Samuel Sanches. Todos os direitos reservados.

## Estado atual

Marco atual: **M0 — Foundation**.

A fundação contém solução .NET, API ASP.NET Core, cliente Windows WPF, persistência PostgreSQL/EF Core, migração inicial de auditoria, health checks, rate limiting, SignalR, base de autenticação JWT, testes de domínio, CI e configuração sem credenciais versionadas.

WhatsApp e inteligência ainda não são declarados como funcionais neste marco. A interface exibe **AGUARDANDO CONFIGURAÇÃO** enquanto não houver implementação e validação reais.

## Princípios

- O banco é a fonte da verdade operacional.
- Inteligência interpreta linguagem; não define fatos.
- Ações críticas de segurança exigem política explícita e controle humano.
- Credenciais de integração ficam no servidor.
- Nenhuma integração aparece como conectada sem teste real.
- O cliente Windows deve permanecer leve.

## Stack M0

- .NET 10 LTS
- ASP.NET Core
- WPF
- CommunityToolkit.Mvvm
- Entity Framework Core 10
- PostgreSQL via Npgsql
- SignalR
- JWT Bearer
- xUnit v3
- GitHub Actions

## Build

~~~
dotnet restore Sentra.sln
dotnet build Sentra.sln --configuration Release
dotnet test Sentra.sln --configuration Release
~~~

## Roadmap

M0 foundation → M1 operação básica → M2 WhatsApp oficial → M3 inteligência → M4 portaria → M5 operação → M6 offline → M7 UX → M8 instalador → M9 release candidate.

A versão 1.0.0 somente será declarada concluída após validação real.
