# Validação SENTRA 1.0

Última atualização do M0: 20/09/2026.

Evidência automatizada: GitHub Actions CI #6, SHA 7eeb74313a7343f8df44d74ca4ba2182522d8665.

| Funcionalidade | Estado | Teste | Evidência | Limitação |
|---|---|---|---|---|
| Restore da solução | FUNCIONANDO | GitHub Actions | CI #6 | — |
| Build Release | FUNCIONANDO | GitHub Actions / Windows | CI #6 | — |
| Testes de domínio | FUNCIONANDO | dotnet test / MTP | CI #6 | cobertura ainda restrita ao M0 |
| Estrutura .NET | FUNCIONANDO | build completo | Sentra.sln | — |
| PostgreSQL/EF | IMPLEMENTADO | build + migration compile | Sentra.Infrastructure | conexão real ainda não fornecida |
| Migration inicial | IMPLEMENTADO | build | InitialFoundation | aplicação contra PostgreSQL real aguarda ambiente |
| Health checks | IMPLEMENTADO | build | /health/live e /health/ready | execução hospedada ainda não validada |
| Cliente WPF | IMPLEMENTADO M0 | build Windows | Sentra.Desktop | operação completa é M1+ |
| CI/CD de build/test | FUNCIONANDO | GitHub Actions | CI #6 | publish/installer entra no M8 |
| WhatsApp | NÃO IMPLEMENTADO | não | — | M2 |
| Inteligência | NÃO IMPLEMENTADO | não | — | M3 |
| Instalador | NÃO IMPLEMENTADO | não | — | M8 |

Estados finais permitidos:

- FUNCIONANDO
- CONFIGURADO MAS AGUARDANDO CREDENCIAL
- PARCIAL
- NÃO IMPLEMENTADO

Uma integração externa não pode ser elevada para FUNCIONANDO sem teste real com o serviço correspondente.
