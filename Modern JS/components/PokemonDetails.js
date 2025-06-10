import { getTypeIcon } from './PokemonIcons.js';

export default class PokemonDetails {
    render(container, pokemon) {
        const formattedNumber = this.formatPokemonNumber(pokemon.id);
        const statsSum = this.calculateStatsSum(pokemon.stats);

        container.innerHTML = `
            <h2>${pokemon.name.toUpperCase()} #${formattedNumber}</h2>
            <img src="${pokemon.sprites.front_default}" alt="${pokemon.name}" />
            <p><strong>Types:</strong> ${pokemon.types.map(t => getTypeIcon(t.type.name) + ' ' + t.type.name).join(', ')}</p>
            <p><strong>Total Stats:</strong> ${statsSum}</p>
            <ul class="pokemon-stats">
                ${pokemon.stats.map(stat => `<li><strong>${stat.stat.name.toUpperCase()}:</strong> ${stat.base_stat}</li>`).join('')}
            </ul>
        `;
    }

    formatPokemonNumber(number) {
        return number.toString().padStart(3, '0');
    }

    calculateStatsSum(stats) {
        return stats.reduce((sum, stat) => sum + stat.base_stat, 0);
    }

    showLoading(container) {
        container.innerHTML = '<p>Loading details...</p>';
    }

    showError(container, message) {
        container.innerHTML = `<p style="color:red;">${message}</p>`;
    }
}
