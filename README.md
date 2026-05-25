# 🗓️ Sistema de Agendamento para Barbearia

Um sistema completo de gerenciamento e agendamento de horários desenvolvido em **ASP.NET Core MVC**. A aplicação foi construída focando em uma arquitetura limpa, segurança de dados e separação de permissões entre administradores e clientes.

---

## 🚀 Funcionalidades Principais

* **Autenticação e Autorização Robusta:** Sistema de login e registro de usuários gerenciado via **ASP.NET Core Identity**.
* **Controle de Acesso por Níveis (RBAC):** * **Admin:** Dashboard administrativo completo para gerenciamento de profissionais, serviços e horários. O primeiro usuário cadastrado no sistema herda automaticamente o papel de Administrador.
  * **Cliente:** Área logada para visualizar horários disponíveis e realizar agendamentos.
  * Worker: ** Área com login de colaborador para ver uma fila de atendimento com visual KANBAN e atualizar o status do atendimento
* **Segurança e Validação de Dados:** * Validação em dupla camada (Front-end com JavaScript/HTML5 e Back-end via *Data Annotations* / `ModelState`).
  * Proteção contra ataques com uso de tokens anti-falsificação (`[ValidateAntiForgeryToken]`).
* **Comunicação por E-mail:** Fluxo automatizado para envio de e-mails de boas-vindas assíncrono (disparado em background), integrado ao servidor SMTP do Gmail utilizando **MailKit**.
* **Configurações Seguras:** Isolamento completo de credenciais sensíveis (chaves de API e senhas de banco) utilizando variáveis de ambiente, preparado para deploy seguro.

---

## 🛠️ Tecnologias Utilizadas

* **Back-End:** .NET 8 / C#
* **Framework Web:** ASP.NET Core MVC
* **Persistência de Dados:** Entity Framework Core
* **Banco de Dados:** SQL Server / PostgreSQL (Pronto para produção)
* **Autenticação:** ASP.NET Core Identity
* **Serviço de E-mail:** MailKit / MimeKit (Integração SMTP Gmail via App Password)
* **Front-End:** Razor Views, Bootstrap 5, JavaScript, CSS3

---

## ⚙️ Configuração Local

Para rodar o projeto localmente na sua máquina, siga os passos abaixo:

### 1. Clonar o repositório
```bash
git clone https://github.com/JulioCSSousa/appointmentApp.git

### 2. Configurar as variáveis de ambiente locais (.env)
Na raiz do projeto, certifique-se de ter um arquivo `.env` configurado com as credenciais do seu banco de dados local (MySQL/MariaDB) e do serviço de e-mail:

```env
# Banco de Dados Local
LOCAL_HOST=localhost
LOCAL_PORT=3306
LOCAL_USER=root
LOCAL_PASSWORD=seupassword
LOCAL=appointmentappdb

# Configurações de E-mail
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=seu-email@gmail.com
SMTP_PASS=sua-senha-de-app-do-google

3. Executar as Migrations do Banco de Dados
No Console do Gerenciador de Pacotes ou terminal, rode os comandos para estruturar o banco:

dotnet ef database update

4. Rodar a aplicação

dotnet run

🌐 Deploy & Variáveis de Ambiente (Produção)
O projeto foi desenhado para seguir as boas práticas de segurança, mantendo credenciais fora do código-fonte. Em ambiente de produção (como o Render), as chaves do arquivo .env local devem ser cadastradas diretamente no painel de controle da hospedagem na aba Environment Variables:

LOCAL_HOST (Endereço do banco de dados de produção)

LOCAL_PORT (Porta do banco de dados de produção)

LOCAL_USER (Usuário do banco de dados)

LOCAL_PASSWORD (Senha do banco de dados)

LOCAL (Nome do banco de dados)

SMTP_HOST

SMTP_PORT

SMTP_USER
