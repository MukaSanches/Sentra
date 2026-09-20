# M1 — Operação básica

O M1 implementa a base real para cadastro e autenticação.

## Bootstrap

Não existe usuário ou senha padrão.

O servidor deve receber `SENTRA_BOOTSTRAP_TOKEN` com pelo menos 32 caracteres. A configuração inicial usa o header `X-Sentra-Bootstrap-Token` e somente funciona enquanto o banco ainda não possui condomínio/funcionário.

O bootstrap cria:

- condomínio informado pelo operador;
- perfil Administrador;
- catálogo explícito de permissões;
- primeiro administrador com senha hasheada.

## Senhas

As senhas são armazenadas com PBKDF2-HMAC-SHA512, salt aleatório por senha, comparação em tempo constante e bloqueio temporário após cinco tentativas inválidas.

## Login

O login exige:

- CondominiumId;
- usuário;
- senha.

Quando JWT não está configurado, a API retorna 503 em vez de emitir token fictício.

## Escopo

Endpoints iniciais cobrem:

- blocos;
- unidades;
- moradores;
- telefone E.164;
- vínculo morador/unidade.

Mais operações de usuários/perfis serão ampliadas antes do fechamento da V1.
