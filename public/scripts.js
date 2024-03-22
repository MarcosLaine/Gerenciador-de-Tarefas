document.addEventListener('DOMContentLoaded', () => {
    const form = document.getElementById('lembreteForm');
    const nomeInput = document.getElementById('nomeLembrete');
    const dataInput = document.getElementById('dataLembrete');
    const lembretesList = document.getElementById('listaLembretes');

    form.addEventListener('submit', function (e) {
        e.preventDefault();
        criarLembrete(nomeInput.value, dataInput.value);
    });

    function criarLembrete(nome, data) {
        const dataLembrete = new Date(data);
        const agora = new Date();

        if (dataLembrete < agora) {
            alert('A data do lembrete deve ser no futuro.');
            return; // Interrompe a execução da função
        }
        fetch('/api/lembretes', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ nome, data })
        })
            .then(response => response.json())
            .then(adicionarLembreteNaLista)
            .catch(error => console.error('Erro ao criar lembrete:', error));
    }

    function adicionarLembreteNaLista(lembrete) {
        const li = document.createElement('li');
        li.textContent = `${lembrete.nome} - ${new Date(lembrete.data).toLocaleDateString()}`;
        const deleteButton = document.createElement('button');
        deleteButton.textContent = 'X'//logo cria o botão para remover, que será removido pelo id do item
        deleteButton.onclick = function () {
            deletarLembrete(lembrete.id, li);
        };
        li.appendChild(deleteButton);
        lembretesList.appendChild(li);
    }

    function deletarLembrete(id, li) {//deleta o lembrete pelo ID clicando no 'X'
        fetch(`/api/lembretes/${id}`, {
            method: 'DELETE'
        })
            .then(() => {
                lembretesList.removeChild(li);
            })
            .catch(error => console.error('Erro ao deletar lembrete:', error));
    }

    function carregarLembretes() {//carrega os lembretes registrados anteriormente
        fetch('/api/lembretes')
            .then(response => response.json())
            .then(lembretes => {
                lembretes.forEach(adicionarLembreteNaLista);
            })
            .catch(error => console.error('Erro ao carregar lembretes:', error));
    }

    carregarLembretes();
});
