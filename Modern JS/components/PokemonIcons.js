export function getTypeIcon(type) {
    const icons = {
        fire: '🔥',
        water: '💧',
        grass: '🌿',
        electric: '⚡',
        psychic: '🧠',
        ice: '❄️',
        dragon: '🐉',
        dark: '🌑',
        fairy: '🧚',
        normal: '📦',
        flying: '🕊️',
        bug: '🐛',
        poison: '☠️',
        ground: '🌍',
        rock: '🪨',
        ghost: '👻',
        steel: '⚙️',
        fighting: '🥊'
    };
    return icons[type] || '❓';
}