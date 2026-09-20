# Validação SENTRA 1.0

| Funcionalidade | Estado | Evidência | Limitação |
|---|---|---|---|
| Estrutura .NET | EM VALIDAÇÃO CI | workflow CI | primeiro run pendente |
| Domínio base | EM VALIDAÇÃO CI | Sentra.Domain.Tests | escopo M0 |
| PostgreSQL/EF | IMPLEMENTADO | Sentra.Infrastructure | conexão real ainda não fornecida |
| Health checks | IMPLEMENTADO | /health/live e /health/ready | hospedagem real pendente |
| Cliente WPF | IMPLEMENTADO M0 | Sentra.Desktop | operação completa M1+ |
| WhatsApp | NÃO IMPLEMENTADO | — | M2 |
| Inteligência | NÃO IMPLEMENTADO | — | M3 |
| Instalador | NÃO IMPLEMENTADO | — | M8 |

Estados finais permitidos: FUNCIONANDO, CONFIGURADO MAS AGUARDANDO CREDENCIAL, PARCIAL, NÃO IMPLEMENTADO.

Nunca elevar estado sem evidência.
