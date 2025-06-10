const newButton = document.getElementById("newButton");
const table = document.getElementById("myCollection");

newButton.addEventListener("click", function () {
    const asdf = document.createElement("tr");
    asdf.innerHTML = `
    <tr>
        <td><input></td>
        <td><input></td>
        <td>
            <button class="saveButton">Save</button>
            <button class="removeButton">Remove</button>
        </td>
    </tr>
    `;

    //table.insertAdjacentHTML("beforeend", newTableRow);
    table.appendChild(asdf);
    //updateRowColors();
});


table.addEventListener("click", function(e) {
    let row = e.target.closest("tr")
    if (e.target.classList.contains("removeButton")) {
        row.remove();
        updateRowColors()
    }
    else if (e.target.classList.contains("saveButton")){
        
        let cells = row.querySelectorAll("td")
        cells.forEach(cell => {
            let input = cell.querySelector("input");
            if (input){
                cell.textContent = input.value;
            }
        });
        e.target.textContent = "Edit";
        e.target.classList.replace("saveButton", "editButton");
    }
    else if (e.target.classList.contains("editButton")){
        let cells = row.querySelectorAll("td");
        cells.forEach(cell => {
            if (!cell.querySelector("button")) { 
                let input = document.createElement("input");
                input.value = cell.textContent;
                cell.textContent = "";
                cell.appendChild(input);
            }
        });
        e.target.textContent = "Save";
        e.target.classList.replace("editButton", "saveButton");
    }
});
