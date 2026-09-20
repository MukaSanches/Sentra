# Desenvolvimento

## Pré-requisitos

- Windows 10/11 para o cliente WPF;
- .NET 10 SDK;
- PostgreSQL para persistência real.

## Build

```powershell
dotnet restore Sentra.sln
dotnet build Sentra.sln -c Release --no-restore
dotnet test Sentra.sln -c Release --no-build
```

## Configuração

Segredos devem vir de variáveis de ambiente, secret manager ou configuração local ignorada pelo Git.

A API não usa credenciais padrão embutidas.

## Banco

A migration inicial está em `src/Sentra.Infrastructure/Persistence/Migrations`.

Não aplique migrations automaticamente em produção sem deployment e backup explícitos.
