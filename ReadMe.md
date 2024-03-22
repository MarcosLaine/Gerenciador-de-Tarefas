# Sistema de Lembretes

Criei esse reporsitório para o desafio do processo seletivo de candidatos da DTI Digital, onde era necessário criar um sistema de lembretes com Dia, Mes e Ano, remover ao clicar no X e utilizar obrigatoriamente C# no código para fazer o back-end do projeto

## Funcionalidades

- Adicionar lembretes com nome e data
- Visualizar lista de lembretes
- Remover lembretes

## Tecnologias Utilizadas

- Frontend: HTML, CSS (gerado através de um arquivo SCSS), JavaScript
- Backend: ASP.NET Core Web API
- LocalStorage

## Premissas assumidas

- Como o foco do projeto era o desenvolvimente do Back-end em C#, não foquei muito na estilização do Front-end
- Como não foi especificado a necessidade da criação de um Banco de Dados externo (como MySQL, MongoDB, etc), usei o LocalStorage da máquina para armazenar os lembretes do usuário
- O usuário pode ter vários lembretes armazenados no navegador do usuário, entretanto, por motivos claros, nenhum lembrete pode ser adicionado para uma data anterior à data atual

# Decisões do Projeto

- Decidi que usaria o framework ASP.NET Core para a criação da API RESTful, o que me permitiria uma maior compatibilidade com o armazenamento de dados no dispositivo
- Decidi que usaria C# como linguagem principal para o Back-end, assim como pedido
- Decidi que para o Front-end, usaria a estrutura básica (HTML, CSS e JavaScript), mas utilizando SCSS como foi proposto inicialmente

# Intruções de Execução


Para executar este projeto localmente, siga os passos abaixo:

1. Clone o repositório para a sua máquina local usando:
   'git clone <uhttps://github.com/MarcosLaine/Teste_DTI.git>'

2. Abra o diretório do projeto no terminal e navegue até a pasta onde se encontra o projeto C# (`LembretesApi`).

3. Dentro dessa pasta, execute o seguinte comando para restaurar as dependências do projeto e compilar o código:
   'dotnet restore' e 'dotnet build'

4. Depois disso, você pode iniciar o servidor com o comando:
    'dotnet run'
    Isso iniciará o servidor em `http://localhost:5285` por padrão, ou em uma porta diferente se você configurou de outra forma.
5. Para acessar a aplicação, abra seu navegador e digite o endereço `http://localhost:5285`.

6. Você também precisará compilar o SCSS para CSS antes de carregar a aplicação pela primeira vez. Para isso, volte ao diretório raiz do projeto e execute:
    'npm install' e 'npm run build-css'
    Isso vai compilar seu arquivo SCSS em um arquivo CSS utilizável.
    
7. Com o servidor em execução e o SCSS compilado, recarregue sua página no navegador. Com isso, a aplicacão deverá funcionar corretamente.
    
 
