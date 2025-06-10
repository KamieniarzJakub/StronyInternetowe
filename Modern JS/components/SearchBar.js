export default class SearchBar {
    constructor(onSearch) {
      this.onSearch = onSearch;
    }
  
    render(container) {
      const input = document.createElement('input');
      input.type = 'text';
      input.placeholder = 'Search Pokémon by name...';
  
      input.addEventListener('input', (e) => {
        this.onSearch(e.target.value.toLowerCase());
      });
  
      container.appendChild(input);
    }
  }
  