// Aguarda o carregamento do conteúdo da página antes de executar o código
document.addEventListener('DOMContentLoaded', () => {
    // Obtém referências para os elementos do formulário e da lista de lembretes
    const form = document.getElementById('lembreteForm');
    const nomeInput = document.getElementById('nomeLembrete');
    const dataInput = document.getElementById('dataLembrete');
    const lembretesList = document.getElementById('listaLembretes');

    // Adiciona um ouvinte de evento para lidar com o envio do formulário
    form.addEventListener('submit', function (e) {
        e.preventDefault(); // Impede o comportamento padrão de envio do formulário
        criarLembrete(nomeInput.value, dataInput.value); // Chama a função para criar um novo lembrete
    });

    // Função para criar um novo lembrete
    function criarLembrete(nome, data) {
        const dataLembrete = new Date(data);
        const agora = new Date();

        // Verifica se a data do lembrete é no futuro
        if (dataLembrete < agora) {
            alert('A data do lembrete deve ser no futuro.');
            return; // Interrompe a execução da função
        }
        // Faz uma requisição POST para a API para criar o lembrete
        fetch('/api/lembretes', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ nome, data }) // Converte os dados do lembrete para JSON
        })
        .then(response => response.json()) // Converte a resposta para JSON
        .then(adicionarLembreteNaLista) // Adiciona o lembrete recém-criado à lista
        .catch(error => console.error('Erro ao criar lembrete:', error));
    }

    // Função para adicionar um lembrete à lista na página
    function adicionarLembreteNaLista(lembrete) {
        const li = document.createElement('li'); // Cria um novo elemento de lista
        li.textContent = `${lembrete.nome} - ${new Date(lembrete.data).toLocaleDateString()}`; // Define o texto do elemento
        const deleteButton = document.createElement('button'); // Cria um botão para deletar o lembrete
        deleteButton.textContent = 'X';
        deleteButton.onclick = function () {
            deletarLembrete(lembrete.id, li); // Adiciona um ouvinte de evento para deletar o lembrete
        };
        li.appendChild(deleteButton); // Adiciona o botão ao elemento de lista
        lembretesList.appendChild(li); // Adiciona o elemento de lista à lista de lembretes
    }

    // Função para deletar um lembrete
    function deletarLembrete(id, li) {
        // Faz uma requisição DELETE para a API para deletar o lembrete
        fetch(`/api/lembretes/${id}`, {
            method: 'DELETE'
        })
        .then(() => {
            lembretesList.removeChild(li); // Remove o elemento de lista da página
        })
        .catch(error => console.error('Erro ao deletar lembrete:', error));
    }

    // Função para carregar os lembretes existentes ao abrir a página
    function carregarLembretes() {
        // Faz uma requisição GET para a API para buscar os lembretes
        fetch('/api/lembretes')
        .then(response => response.json()) // Converte a resposta para JSON
        .then(lembretes => {
            lembretes.forEach(adicionarLembreteNaLista); // Adiciona cada lembrete à lista
        })
        .catch(error => console.error('Erro ao carregar lembretes:', error));
    }

    carregarLembretes(); // Chama a função para carregar os lembretes ao abrir a página
});
