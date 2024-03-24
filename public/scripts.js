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
            return;
        }

        fetch('/api/lembretes', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ nome, data })
        })
        .then(response => response.json())
        .then(lembreteCriado => {
            // Reordena a lista após adicionar um novo lembrete
            carregarLembretes();
        })
        .catch(error => console.error('Erro ao criar lembrete:', error));
    }

    function adicionarLembreteNaLista(lembrete) {
        const dataFormatada = new Date(lembrete.data).toLocaleDateString();
        let secaoData = lembretesList.querySelector(`[data-date="${dataFormatada}"]`);
        if (!secaoData) {
            secaoData = document.createElement('section');
            secaoData.dataset.date = dataFormatada;

            const tituloData = document.createElement('h3');
            tituloData.textContent = `Lembretes do Dia ${dataFormatada}:`;
            secaoData.appendChild(tituloData);

            const listaLembretes = document.createElement('ul');
            secaoData.appendChild(listaLembretes);

            lembretesList.appendChild(secaoData);
        } 

        const listaLembretes = secaoData.querySelector('ul');
        const itemLembrete = document.createElement('li');
        itemLembrete.textContent = lembrete.nome;

        const botaoRemover = document.createElement('button');
        botaoRemover.textContent = 'X';
        botaoRemover.onclick = () => {
            deletarLembrete(lembrete.id, secaoData, itemLembrete);
        };

        itemLembrete.appendChild(botaoRemover);
        listaLembretes.appendChild(itemLembrete);
    }

    function deletarLembrete(id, secaoData, itemLembrete) {
        fetch(`/api/lembretes/${id}`, {
            method: 'DELETE'
        })
        .then(() => {
            secaoData.querySelector('ul').removeChild(itemLembrete);
            if (!secaoData.querySelector('ul').hasChildNodes()) {
                lembretesList.removeChild(secaoData);
            }
        })
        .catch(error => console.error('Erro ao deletar lembrete:', error));
    }

    function carregarLembretes() {
        fetch('/api/lembretes')
        .then(response => response.json())
        .then(lembretes => {
            lembretesList.innerHTML = ''; // Limpa a lista para reordenar
            lembretes.sort((a, b) => new Date(a.data) - new Date(b.data));
            lembretes.forEach(lembrete => adicionarLembreteNaLista(lembrete));
        })
        .catch(error => console.error('Erro ao carregar lembretes:', error));
    }

    carregarLembretes();
});
