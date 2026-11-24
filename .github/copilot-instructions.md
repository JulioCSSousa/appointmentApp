# Copilot Instructions

## Diretrizes de projeto
- Para distribuir agendamentos sem preferência, o usuário prefere uma seleção aleatória entre profissionais disponíveis, ponderada pela quantidade de agendamentos/atendimentos já atribuídos, para evitar sobrecarregar sempre o mesmo profissional.
- O cliente não deve ser obrigado a agendar como visitante: ele precisa ter a opção de fazer login por telefone e senha quando quiser, inclusive a partir da tela de agendamento. A página de login do cliente deve permanecer sempre disponível e acessível pelo menu/tela de agendamento; não deve ser substituída pelo login administrativo nem removida durante correções de autorização.
- Na homepage pública, o botão de login deve apontar somente para o login administrativo. O login do cliente deve permanecer disponível em uma rota separada, como `/Account/ClientLogin`, acessível a partir do fluxo de agendamento, sem aparecer no contexto da homepage.
- Na página pública de login, exibir somente o login administrativo; remover entradas de login de clientes e profissionais. Clientes e profissionais devem acessar posteriormente pela loja/tenant, quando o sistema de tenants e personalização for criado.
- O sistema deverá evoluir para multi-tenancy: um tenant pode controlar uma ou mais lojas; clientes e profissionais pertencem a uma loja; o tenant administra lojas e seus funcionários.
- O usuário aceitou usar um único banco compartilhado com isolamento rigoroso por StoreId/TenantId, mantendo acesso global explícito apenas para SuperAdmin; consultas de tenant devem validar a loja atual e não usar fallback silencioso para a primeira loja.
- Implementar isolamento multi-tenant centralizado com ITenantContext, filtros globais do EF Core, validação no SaveChangesAsync e testes/build; manter SuperAdmin como exceção explícita.
- Aplicar filtros globais de tenant nos pontos mais críticos do sistema, mantendo SuperAdmin como exceção explícita e preservando validação de StoreId nas escritas.

## Estrutura de autenticação
- Administradores usarão e-mail para login obrigatório.
- Profissionais usarão número de telefone para login obrigatório, com autenticação por senha.
- Clientes poderão usar número de telefone para login opcional, mas o login será opcional para agendamento.