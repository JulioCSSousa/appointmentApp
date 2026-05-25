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
git clone [https://github.com/SEU_USUARIO/NOME_DO_REPOSITORIO.git](https://github.com/SEU_USUARIO/NOME_DO_REPOSITORIO.git)
cd NOME_DO_REPOSITORIO

2. Configurar as credenciais (appsettings.Development.json)
Crie ou ajuste o arquivo de configuração local incluindo suas credenciais de teste:

{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=AppointmentDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUser": "seu-email@gmail.com",
    "SmtpPass": "sua-senha-de-app-do-google"
  }
}

3. Executar as Migrations do Banco de Dados
No Console do Gerenciador de Pacotes ou terminal, rode os comandos para estruturar o banco:

dotnet ef database update

4. Rodar a aplicação

dotnet run
