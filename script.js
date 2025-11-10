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

function showPage(pageId) {
  document.querySelectorAll('.page').forEach(p => p.classList.remove('active'));
  document.getElementById(pageId).classList.add('active');
  if (pageId === 'overzicht') updatePersonList();
}

function savePeople() {
  localStorage.setItem('peopleList', JSON.stringify(people));
}

document.getElementById('date').addEventListener('change', function () {
  const locationInput = document.getElementById('location');
  if (!this.value) {
    locationInput.value = '';
    locationInput.readOnly = true;
  } else {
    locationInput.value = dateToLocationMap[this.value] || '';
    locationInput.readOnly = true;
  }
});

// Toevoegen -- combineer aantallen als zelfde persoon+datum!
document.getElementById('addPersonForm').addEventListener('submit', function (e) {
  e.preventDefault();

  const firstName = document.getElementById('firstName').value.trim();
  const lastName = document.getElementById('lastName').value.trim();
  const date = document.getElementById('date').value;
  const location = date ? dateToLocationMap[date] || '' : '';
  const amount = parseInt(document.getElementById('amount').value, 10) || 1;

  // Duplicaat check enkel als datum is ingevuld
  if (date) {
    const existingIdx = people.findIndex(
      (p) => p.firstName.toLowerCase() === firstName.toLowerCase() &&
             p.lastName.toLowerCase() === lastName.toLowerCase() &&
             p.date === date
    );
    if (existingIdx !== -1) {
      if (confirm('Deze persoon staat al op deze datum. Wil je het aantal verhogen?')) {
        people[existingIdx].amount = (parseInt(people[existingIdx].amount, 100) || 1) + amount;
        savePeople();
        this.reset();
        document.getElementById('location').value = '';
        showPage('overzicht');
      }
      return;
    }
  }

  people.unshift({ firstName, lastName, date, location, amount, addedAt: Date.now() });
  savePeople();
  this.reset();
  document.getElementById('location').value = '';
  showPage('overzicht');
});

// Overzicht tonen & sorteren, download ticket alleen bij personen met datum
function updatePersonList() {
  const list = document.getElementById('personList');
  const search = document.getElementById('searchInput').value.toLowerCase();
  const filtered = people.filter(p =>
    `${p.firstName} ${p.lastName} ${p.date} ${p.location}`.toLowerCase().includes(search)
  );
  filtered.sort((a, b) => (b.addedAt || 0) - (a.addedAt || 0));

  const totalEarned = filtered.reduce((sum, p) => sum + (p.amount || 1)*8, 0);
  document.getElementById('totalEarned').textContent = `Totaal verdiend: €${totalEarned}`;

  // Statistieken per datum
  const dateCounts = {};
  filtered.forEach(p => {
    const d = p.date || 'Nog niet gekozen';
    dateCounts[d] = (dateCounts[d] || 0) + (p.amount || 1);
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
    const label = person.amount == 1 ? 'persoon' : 'personen';
    const li = document.createElement('li');
    let knopHtml = '';
    if (person.date) {
      knopHtml += `<button class="download-knop" onclick="downloadImageWithText(people[${people.indexOf(person)}])">Download Ticket</button>`;
    }
    knopHtml += `
      <button class="wijzig-knop" onclick="showEditForm(${people.indexOf(person)},this.parentNode.parentNode)">Wijzig</button>
      <button class="verwijder-knop" onclick="removePerson(${people.indexOf(person)})">Verwijder</button>
    `;
    li.innerHTML = `
      <div class="person-info">
        ${person.firstName} ${person.lastName} – ${person.date || 'Nog niet gekozen'} – ${person.location || ""} – ${person.amount || 1} ${label}
      </div>
      <div class="knoppen-wrapper">
        ${knopHtml}
      </div>
    `;
    list.appendChild(li);
  });
}

window.removePerson = function(idx) {
  if (confirm('Weet je zeker dat je deze persoon wilt verwijderen?')) {
    people.splice(idx, 1);
    savePeople();
    updatePersonList();
  }
};

window.showEditForm = function(idx, li) {
  const person = people[idx];
  const editForm = document.createElement('form');
  editForm.classList.add('edit-form');
  editForm.innerHTML = `
    <input type="text" name="firstName" value="${person.firstName}" required>
    <input type="text" name="lastName" value="${person.lastName}" required>
    <select name="date">
      <option value="">Nog niet gekozen</option>
      <option value="31 januari 2026"${person.date === "31 januari 2026" ? " selected" : ""}>31 januari 2026</option>
      <option value="1 februari 2026"${person.date === "1 februari 2026" ? " selected" : ""}>1 februari 2026</option>
      <option value="7 februari 2026"${person.date === "7 februari 2026" ? " selected" : ""}>7 februari 2026</option>
      <option value="8 februari 2026"${person.date === "8 februari 2026" ? " selected" : ""}>8 februari 2026</option>
      <option value="14 februari 2026"${person.date === "14 februari 2026" ? " selected" : ""}>14 februari 2026</option>
      <option value="15 februari 2026"${person.date === "15 februari 2026" ? " selected" : ""}>15 februari 2026</option>
      <option value="22 februari 2026"${person.date === "22 februari 2026" ? " selected" : ""}>22 februari 2026</option>
    </select>
    <input type="text" name="location" value="${person.location || ''}" readonly>
    <input type="number" name="amount" min="1" max="100" value="${person.amount || 1}" required>
    <button type="submit">Opslaan</button>
    <button type="button" class="cancel">Annuleer</button>
  `;
  const dateSel = editForm.querySelector('[name="date"]');
  const locInput = editForm.querySelector('[name="location"]');
  function updateEditLocation() {
    if (!dateSel.value) {
      locInput.value = '';
      locInput.readOnly = true;
    } else {
      locInput.value = dateToLocationMap[dateSel.value] || '';
      locInput.readOnly = true;
    }
  }
  dateSel.addEventListener('change', updateEditLocation);
  updateEditLocation();

  editForm.onsubmit = function(e) {
    e.preventDefault();
    const newFirstName = editForm.firstName.value.trim();
    const newLastName = editForm.lastName.value.trim();
    const newDate = editForm.date.value;
    const newLocation = newDate ? (dateToLocationMap[newDate] || "") : "";
    const newAmount = parseInt(editForm.amount.value, 100) || 1;

    // Kijk of er al een ANDERE persoon met dezelfde naam, datum bestaat
    const existingIdx = people.findIndex(
      (p, i) => i !== idx &&
                p.firstName.toLowerCase() === newFirstName.toLowerCase() &&
                p.lastName.toLowerCase() === newLastName.toLowerCase() &&
                p.date === newDate
    );
    if (existingIdx !== -1) {
      if (confirm('Deze persoon staat al op deze datum. Wil je het aantal samenvoegen?')) {
        // Tel bij andere op, verwijder huidige
        people[existingIdx].amount = (parseInt(people[existingIdx].amount, 100) || 1) + newAmount;
        people.splice(idx, 1);
        savePeople();
        updatePersonList();
        return;
      } else {
        // Annuleer wijziging
        updatePersonList();
        return;
      }
    }

    // Normaal opslaan
    person.firstName = newFirstName;
    person.lastName = newLastName;
    person.date = newDate;
    person.location = newLocation;
    person.amount = newAmount;
    savePeople();
    updatePersonList();
  };
  editForm.querySelector('.cancel').onclick = updatePersonList;
  li.innerHTML = "";
  li.appendChild(editForm);
}

document.getElementById('searchInput').addEventListener('input', updatePersonList);

document.getElementById('downloadBtn').addEventListener('click', () => {
  if (!people.length) {
    alert('Er zijn geen personen om te exporteren.');
    return;
  }
  const sorted = [...people].sort((a, b) => (b.addedAt || 0) - (a.addedAt || 0));
  const totalEarned = sorted.reduce((sum, p) => sum + (p.amount || 1) * 8, 0);
  const data = sorted.map((p, i) => ({
    Nummer: (i+1).toString().padStart(3,'0'),
    Datum: p.date || 'Nog niet gekozen',
    Locatie: p.location || '',
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

function downloadImageWithText(person) {
  if (!person.date) {
    alert('Ticket downloaden kan alleen bij personen met een datum!');
    return;
  }
  const img = new Image();
  img.src = 'Ticket Er was eens ... Rare wereld VK.jpg';
  img.onload = () => {
    const canvas = document.createElement('canvas');
    canvas.width = img.width;
    canvas.height = img.height;
    const ctx = canvas.getContext('2d');
    ctx.drawImage(img, 0, 0);
    ctx.font = '28px Arial';
    ctx.fillStyle = 'black';
    ctx.textAlign = 'center';
    const nameX = 860, nameY = 396;
    const dateX = 695, dateY = 315;
    const locationX = 690, locationY = 356;
    ctx.fillText(person.date, dateX, dateY);
    ctx.fillText(person.location, locationX, locationY);
    ctx.fillText(`${person.lastName} ${person.firstName}`, nameX, nameY);
    const link = document.createElement('a');
    link.download = `${person.lastName}_${person.firstName}-Ticket-Rare_Wereld.png`;
    link.href = canvas.toDataURL('image/png');
    link.click();
  };
  img.onerror = () => {
    alert('Kan ticketafbeelding niet laden!');
  };
}

// Bij laden direct overzicht tonen als actief
if (document.getElementById('overzicht').classList.contains('active')) {
  updatePersonList();
}

