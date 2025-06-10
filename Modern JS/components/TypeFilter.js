import { getTypeIcon } from './PokemonIcons.js'; // Importujemy funkcję z pliku PokemonIcons.js

export default class TypeFilter {
    constructor(onTypeChange) {
        this.onTypeChange = onTypeChange;
        this.selectedType1 = 'all';
        this.selectedType2 = 'all';
    }

    render(container, types) {
        const wrapper = document.createElement('div');
        wrapper.className = 'type-selectors';

        const label1 = document.createElement('label');
        label1.innerText = 'Type 1: ';
        const select1 = document.createElement('select');
        this.populateSelect(select1, types);

        const label2 = document.createElement('label');
        label2.innerText = 'Type 2: ';
        const select2 = document.createElement('select');
        this.populateSelect(select2, types);

        select1.addEventListener('change', () => {
            this.selectedType1 = select1.value;
            this.notifyChange();
        });

        select2.addEventListener('change', () => {
            this.selectedType2 = select2.value;
            this.notifyChange();
        });

        label1.appendChild(select1);
        label2.appendChild(select2);
        wrapper.appendChild(label1);
        wrapper.appendChild(label2);
        container.appendChild(wrapper);
    }

    populateSelect(select, types) {
        const defaultOption = document.createElement('option');
        defaultOption.value = 'all';
        defaultOption.textContent = 'All';
        select.appendChild(defaultOption);

        types.forEach(type => {
            const icon = getTypeIcon(type.name);
            if (!icon) return;

            const option = document.createElement('option');
            option.value = type.name;
            option.textContent = `${icon} ${type.name}`;
            select.appendChild(option);
        });
    }

    notifyChange() {
        this.onTypeChange([this.selectedType1, this.selectedType2]);
    }
}
