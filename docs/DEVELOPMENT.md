# Desenvolvimento

## Requisitos

- .NET 10 SDK.
- Windows para executar o cliente WPF.
- PostgreSQL quando a persistência real for configurada.

## Comandos

~~~
dotnet restore Sentra.sln
dotnet build Sentra.sln -c Release
dotnet test Sentra.sln -c Release
~~~

## Configuração local

Não versionar valores confidenciais. O arquivo .env.example lista apenas nomes de configuração.

## Banco

Migrações ficam em Sentra.Infrastructure/Persistence/Migrations. Depois de publicada, uma migration não deve ser reescrita; alterações geram nova migration.

## Correções

isolar → reproduzir → causa raiz → menor correção correta → testar → avançar.
