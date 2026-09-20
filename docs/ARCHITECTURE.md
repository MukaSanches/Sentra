# Arquitetura — SENTRA

## Estado

Marco atual: **M0 — Foundation**.

A versão 1.0.0 ainda não foi validada como release.

## Princípios

1. O Windows é cliente leve; não executa LLM pesado nem armazena segredos de provedores.
2. O backend é a fronteira para banco, integrações externas e inteligência.
3. A IA interpreta linguagem; regras de domínio e banco definem a verdade.
4. Integrações externas são desacopladas e não podem derrubar toda a operação.
5. Dados operacionais críticos terão estratégia offline explícita em M6.
6. Nenhuma integração é marcada como conectada sem teste real.

## Projetos M0

- `Sentra.Domain`: entidades e invariantes.
- `Sentra.Application`: casos de uso/contratos.
- `Sentra.Infrastructure`: EF Core/PostgreSQL.
- `Sentra.Api`: boundary HTTP e composição.
- `Sentra.Desktop`: cliente Windows WPF.
- `Sentra.Domain.Tests`: testes do domínio.

A divisão será ampliada apenas quando houver responsabilidade concreta.

## Segurança

JWT só é configurado quando Issuer, Audience e SigningKey válidos são fornecidos externamente. Não há credencial padrão embutida.

## Próximos marcos

M1: condomínio, blocos, unidades, moradores, funcionários e RBAC.  
M2: Meta WhatsApp Cloud API oficial.  
M3: interpretação estruturada e Policy Engine.
