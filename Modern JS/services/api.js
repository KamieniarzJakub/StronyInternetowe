const BASE_URL = 'https://pokeapi.co/api/v2';

export async function fetchPokemonList(limit = 1500, offset = 0) {
  const res = await fetch(`${BASE_URL}/pokemon?limit=${limit}&offset=${offset}`);
  if (!res.ok) throw new Error('Failed to fetch Pokémon list');
  return res.json();
}

export async function fetchPokemonDetails(url) {
  const res = await fetch(url);
  if (!res.ok) throw new Error('Failed to fetch Pokémon details');
  return res.json();
}

export async function fetchPokemonTypes() {
  const res = await fetch(`${BASE_URL}/type`);
  if (!res.ok) throw new Error('Failed to fetch Pokémon types');
  return res.json();
}
