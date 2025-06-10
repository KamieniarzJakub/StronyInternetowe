export default class PokemonList {
    constructor(onSelect) {
      this.onSelect = onSelect;
    }
  
    render(container, pokemons) {
      container.innerHTML = '';
  
      if (pokemons.length === 0) {
        container.innerHTML = '<p>No Pokémon found.</p>';
        return;
      }
  
      pokemons.forEach(pokemon => {
        const card = document.createElement('div');
        card.className = 'pokemon-card';
        card.textContent = pokemon.name;
        card.addEventListener('click', () => this.onSelect(pokemon.url));
        container.appendChild(card);
      });
    }
  }
  