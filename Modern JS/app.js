import {
    fetchPokemonList,
    fetchPokemonDetails,
    fetchPokemonTypes
  } from './services/api.js';
  
  import PokemonList from './components/PokemonList.js';
  import PokemonDetails from './components/PokemonDetails.js';
  import SearchBar from './components/SearchBar.js';
  import TypeFilter from './components/TypeFilter.js';
  
  const listContainer = document.getElementById('pokemon-list');
  const detailsContainer = document.getElementById('pokemon-details');
  
  const pokemonList = new PokemonList(handlePokemonSelect);
  const pokemonDetails = new PokemonDetails();
  const searchBar = new SearchBar(handleSearch);
  const typeFilter = new TypeFilter(handleTypeFilter);
  
  let fullList = [];      // All Pokémon (with details)
  let currentList = [];   // Filtered by types
  let displayedList = []; // Filtered by search
  
  let type1 = 'all';
  let type2 = 'all';
  
  init();
  
  async function init() {
    try {
      const types = await fetchPokemonTypes();
      const searchInputContainer = document.getElementById('search-input-container');
      const typeFiltersContainer = document.getElementById('type-filters-container');
  
      searchBar.render(searchInputContainer);
      typeFilter.render(typeFiltersContainer, types.results);
  
      listContainer.innerHTML = '<p>Ładowanie Pokémonów...</p>';
  
      for (let offset = 0; offset < 1500; offset += 100) {
        const batch = await fetchPokemonList(100, offset);
  
        const detailedBatch = await Promise.all(
          batch.results.map(async p => {
            try {
              const details = await fetchPokemonDetails(p.url);
              return {
                name: p.name,
                url: p.url,
                types: details.types.map(t => t.type.name),
                sprite: details.sprites.front_default,
                id: details.id,
                stats: details.stats.map(stat => ({
                  stat: stat.stat.name,
                  base_stat: stat.base_stat
                }))
              };
            } catch (e) {
              return null;
            }
          })
        );
  
        const cleanedBatch = detailedBatch.filter(p => p !== null);
  
        fullList.push(...cleanedBatch);
        currentList = [...fullList];
        displayedList = [...fullList];
        pokemonList.render(listContainer, displayedList);
      }
    } catch (err) {
      listContainer.innerHTML = `<p style="color:red;">${err.message}</p>`;
    }
    detailsContainer.classList.add('pokedex-ready');
  }
  
  function handleSearch(query) {
    displayedList = currentList.filter(p => p.name.includes(query));
    pokemonList.render(listContainer, displayedList);
  }
  
  function handleTypeFilter(types) {
    type1 = types[0];
    type2 = types[1];
  
    if (type1 === 'all' && type2 === 'all') {
      currentList = [...fullList];
      handleSearch('');
      return;
    }
  
    currentList = fullList.filter(pokemon => {
      const pTypes = pokemon.types;
      if (!pTypes) return false;
  
      if (type1 !== 'all' && type2 !== 'all') {
        if (type1 === type2) {
          return pTypes.length === 1 && pTypes[0] === type1;
        }
        return pTypes.length === 2 && pTypes.includes(type1) && pTypes.includes(type2);
      }
  
      const selected = type1 !== 'all' ? type1 : type2;
      return pTypes.includes(selected);
    });
  
    handleSearch('');
  }
  
  async function handlePokemonSelect(url) {
    try {
      pokemonDetails.showLoading(detailsContainer);
      const data = await fetchPokemonDetails(url);

  
      pokemonDetails.render(detailsContainer, {
        ...data,
      });
    } catch (err) {
      pokemonDetails.showError(detailsContainer, err.message);
    }
  }
