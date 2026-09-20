# Arquitetura — SENTRA

SENTRA é uma Central Inteligente de Portaria para Windows. O cliente é leve; integrações e processamento de inteligência ficam no backend.

## Arquitetura alvo

~~~
Meta WhatsApp Business Platform
            |
            v
        SENTRA API
            |
  +---------+-----------+
  |         |           |
PostgreSQL SignalR  Intelligence
  |                     |
  +----------+----------+
             |
             v
       SENTRA Windows
             |
          SQLite
        (marco M6)
~~~

## Limites

- Domain não depende de infraestrutura.
- Application depende de Domain e Contracts.
- Infrastructure implementa persistência e adaptadores externos.
- API compõe serviços e expõe HTTP/SignalR.
- Desktop não recebe credenciais administrativas.

## M0

Inclui estrutura de projetos, EF Core/PostgreSQL, migração inicial, health checks, OpenAPI de desenvolvimento, rate limiting, correlation ID, SignalR, base JWT, WPF, testes e CI.

Sem configuração externa, readiness informa **AGUARDANDO CONFIGURAÇÃO** em vez de simular conexão.
