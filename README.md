# SENTRA

**Central Inteligente de Portaria**

Desenvolvido por **Samuel Sanches**.

> Estado atual: desenvolvimento da versão 1.0.0. Este repositório ainda não representa uma release de produção validada.

## Objetivo

O SENTRA é uma central Windows cliente-servidor para operação de portaria, com backend próprio, operação offline controlada e integrações oficiais. A inteligência é infraestrutura de interpretação; o banco e as regras do domínio permanecem como fonte da verdade.

## M0 — Foundation

- .NET 10 LTS;
- WPF para o cliente Windows;
- ASP.NET Core para o backend;
- Entity Framework Core + PostgreSQL;
- migração inicial;
- health endpoint;
- rate limiting;
- base de autenticação/autorização sem segredos hardcoded;
- Serilog;
- testes automatizados;
- GitHub Actions.

As integrações WhatsApp e inteligência **não são apresentadas como funcionais até serem implementadas e validadas com credenciais reais**.

## Build

```powershell
dotnet restore Sentra.sln
dotnet build Sentra.sln -c Release --no-restore
dotnet test Sentra.sln -c Release --no-build
```

Consulte `docs/ARCHITECTURE.md`, `docs/DEPENDENCIES.md` e `docs/DEVELOPMENT.md`.

## Autoria

SENTRA — Central Inteligente de Portaria  
Desenvolvido por Samuel Sanches  
© 2026 Samuel Sanches. Todos os direitos reservados.
