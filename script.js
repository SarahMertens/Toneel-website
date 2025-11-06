// Data koppeling datum --> locatie
const dateToLocationMap = {
  '31 januari 2026': 'OC De Boeie',
  '1 februari 2026': 'OC De Boeie',
  '7 februari 2026': 'OC De Schelpe',
  '8 februari 2026': 'OC De Schelpe',
  '14 februari 2026': 'OC De Blomme',
  '15 februari 2026': 'OC De Blomme',
  '22 februari 2026': 'OC De Ballon'
};

let people = JSON.parse(localStorage.getItem('peopleList')) || [];

// Paginawissel
function showPage(pageId) {
  document.querySelectorAll('.page').forEach(p => p.classList.remove('active'));
  document.getElementById(pageId).classList.add('active');
  if (pageId === 'overzicht') updatePersonList();
}

// Toevoeg-formulier: locatie altijd updaten op basis van datum
document.getElementById('date').addEventListener('change', function () {
  const loc = dateToLocationMap[this.value] || '';
  document.getElementById('location').value = loc;
});

// Toevoegen
document.getElementById('addPersonForm').addEventListener('submit', function (e) {
  e.preventDefault();

  const firstName = document.getElementById('firstName').value.trim();
  const lastName = document.getElementById('lastName').value.trim();
  const dateSel = document.getElementById('date');
  const date = dateSel.value;
  const location = dateToLocationMap[date] || '';
  const amount = parseInt(document.getElementById('amount').value, 10) || 1;

  if (!date) {
    alert('Kies eerst een datum.');
    return;
  }

// Zoek bestaande index van dezelfde persoon + datum
  const existingIdx = people.findIndex(
    (p) => p.firstName.toLowerCase() === firstName.toLowerCase() &&
           p.lastName.toLowerCase() === lastName.toLowerCase() &&
           p.date === date
  );

  if (existingIdx !== -1) {
    // Bestaat al: vraag wat de gebruiker wil
    if (!confirm('Je hebt deze persoon met deze datum al toegevoegd. Wil je dit TOCH doen? (Bijvoorbeeld extra ticket voor vriend)')) {
      return; // Annuleer toevoegen
    }
  }

  people.unshift({ firstName, lastName, date, location, amount, addedAt: Date.now() });
  localStorage.setItem('peopleList', JSON.stringify(people));
  this.reset();
  document.getElementById('location').value = '';
  showPage('overzicht');
});

// Overzicht tonen, sorteer op meest recent
function updatePersonList() {
  const list = document.getElementById('personList');
  const search = document.getElementById('searchInput').value.toLowerCase();
  const filtered = people.filter(p =>
    `${p.firstName} ${p.lastName} ${p.date} ${p.location}`.toLowerCase().includes(search)
  );
  filtered.sort((a, b) => (b.addedAt || 0) - (a.addedAt || 0));

  const totalEarned = filtered.reduce((sum, p) => sum + (p.amount || 1)*8, 0);
  document.getElementById('totalEarned').textContent = `Totaal verdiend: €${totalEarned}`;

  const dateCounts = {};
  filtered.forEach(p => {
    dateCounts[p.date] = (dateCounts[p.date] || 0) + (p.amount || 1);
  });
  document.getElementById('locationStats').innerHTML = 
    '<strong>Aantal personen per datum:</strong><ul>' +
    Object.entries(dateCounts).map(([d, n]) => `<li>${d}: ${n}</li>`).join('') +
    '</ul>';

  list.innerHTML = '';
  if (!filtered.length) {
    list.innerHTML = '<li>Geen resultaten gevonden.</li>';
    return;
  }
  filtered.forEach((person, idx) => {
    const li = document.createElement('li');
    const label = person.amount == 1 ? 'persoon' : 'personen';
    li.innerHTML = `
      <div class="person-info">
        ${person.firstName} ${person.lastName} – ${person.date} – ${person.location} – ${person.amount || 1} ${label}
      </div>
      <div class="knoppen-wrapper">
        <button onclick="showEditForm(${idx},this.parentNode.parentNode)">Wijzig</button>
        <button onclick="removePerson(${idx})">Verwijder</button>
      </div>
    `;
    list.appendChild(li);
  });
}

window.removePerson = function(idx) {
  if (confirm('Weet je zeker dat je deze persoon wilt verwijderen?')) {
    people.splice(idx, 1);
    localStorage.setItem('peopleList', JSON.stringify(people));
    updatePersonList();
  }
};

// Wijzig-formulier
window.showEditForm = function(idx, li) {
  const person = people[idx];
  const editForm = document.createElement('form');
  editForm.classList.add('edit-form');
  editForm.innerHTML = `
    <input type="text" name="firstName" value="${person.firstName}" required>
    <input type="text" name="lastName" value="${person.lastName}" required>
    <select name="date" required>
      <option value="" disabled ${!person.date ? "selected" : ""}>&laquo;&laquo; Kies een datum &raquo;&raquo;</option>
      <option value="31 januari 2026"${person.date === "31 januari 2026" ? " selected" : ""}>31 januari 2026</option>
      <option value="1 februari 2026"${person.date === "1 februari 2026" ? " selected" : ""}>1 februari 2026</option>
      <option value="7 februari 2026"${person.date === "7 februari 2026" ? " selected" : ""}>7 februari 2026</option>
      <option value="8 februari 2026"${person.date === "8 februari 2026" ? " selected" : ""}>8 februari 2026</option>
      <option value="14 februari 2026"${person.date === "14 februari 2026" ? " selected" : ""}>14 februari 2026</option>
      <option value="15 februari 2026"${person.date === "15 februari 2026" ? " selected" : ""}>15 februari 2026</option>
      <option value="22 februari 2026"${person.date === "22 februari 2026" ? " selected" : ""}>22 februari 2026</option>
    </select>
    <input type="text" name="location" value="${person.location || ''}" readonly>
    <input type="number" name="amount" min="1" max="10" value="${person.amount || 1}" required>
    <button type="submit">Opslaan</button>
    <button type="button" class="cancel">Annuleer</button>
  `;

  const dateSel = editForm.querySelector('[name="date"]');
  const locInput = editForm.querySelector('[name="location"]');
  dateSel.addEventListener('change', function() {
    locInput.value = dateToLocationMap[this.value] || '';
  });
  // Init locatie direct correct zetten
  locInput.value = dateToLocationMap[person.date] || '';

  editForm.onsubmit = function(e) {
    e.preventDefault();
    person.firstName = editForm.firstName.value.trim();
    person.lastName = editForm.lastName.value.trim();
    person.date = editForm.date.value;
    person.location = dateToLocationMap[person.date] || '';
    person.amount = parseInt(editForm.amount.value, 10) || 1;
    localStorage.setItem('peopleList', JSON.stringify(people));
    updatePersonList();
  };
  editForm.querySelector('.cancel').onclick = updatePersonList;
  li.innerHTML = "";
  li.appendChild(editForm);
}

document.getElementById('searchInput').addEventListener('input', updatePersonList);

// Excel export
document.getElementById('downloadBtn').addEventListener('click', () => {
  if (!people.length) {
    alert('Er zijn geen personen om te exporteren.');
    return;
  }
  const sorted = [...people].sort((a, b) => (b.addedAt || 0) - (a.addedAt || 0));
  const totalEarned = sorted.reduce((sum, p) => sum + (p.amount || 1) * 8, 0);
  const data = sorted.map((p, i) => ({
    Nummer: (i+1).toString().padStart(3,'0'),
    Datum: p.date,
    Locatie: p.location,
    Naam: p.lastName,
    Voornaam: p.firstName,
    Aantal: p.amount || 1,
  }));
  const worksheet = XLSX.utils.json_to_sheet(data, { origin: "A3" });
  XLSX.utils.sheet_add_aoa(worksheet, [[`Totaal bedrag: €${totalEarned}`]], {origin: "A1"});
  worksheet['!merges'] = [{ s: { r:0, c:0 }, e: { r:0, c:5 } }];
  worksheet['!cols'] = [{wch:10},{wch:18},{wch:20},{wch:20},{wch:20},{wch:10}];
  const workbook = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(workbook, worksheet, 'Toneelgroep_De_Ballon');
  XLSX.writeFile(workbook, 'Toneelgroep_De_Ballon.xlsx');
});

// Init overzicht bij laden als actief
if (document.getElementById('overzicht').classList.contains('active')) {
  updatePersonList();
}
