let people = JSON.parse(localStorage.getItem('peopleList')) || [];
let lastAddedPerson = null;

const dateToLocationMap = {
  '31 januari 2026': 'OC De Boeie',
  '1 februari 2026': 'OC De Boeie',
  '7 februari 2026': 'OC De Schelpe',
  '8 februari 2026': 'OC De Schelpe',
  '14 februari 2026': 'OC De Blomme',
  '15 februari 2026': 'OC De Blomme',
  '22 februari 2026': 'OC De Ballon',
};

function savePeople() {
  localStorage.setItem('peopleList', JSON.stringify(people));
}

function showPage(pageId) {
  document
    .querySelectorAll('.page')
    .forEach((p) => p.classList.remove('active'));
  document.getElementById(pageId).classList.add('active');

  if (pageId === 'overzicht') {
    updatePersonList();
  }
}

document
  .getElementById('addPersonForm')
  .addEventListener('submit', function (e) {
    e.preventDefault();

    const amount = parseInt(document.getElementById('amount').value, 10) || 1;
    const firstName = document.getElementById('firstName').value.trim();
    const lastName = document.getElementById('lastName').value.trim();
    const date = document.getElementById('date').value;
    const location = document.getElementById('location').value;

    if (firstName && lastName) {
      const duplicate = people.some(
        (p) =>
          p.firstName.toLowerCase() === firstName.toLowerCase() &&
          p.lastName.toLowerCase() === lastName.toLowerCase() &&
          p.date === date
      );

      if (duplicate) {
        alert('Deze persoon is al toegevoegd op deze datum.');
        return;
      }

      const newPerson = { firstName, lastName, date, location, amount };
      people.push(newPerson);
      lastAddedPerson = newPerson;
      savePeople();
      this.reset();
      showPage('overzicht');
      updatePersonList(); // meteen tonen na toevoegen
    }
  });

function updatePersonList() {
  const list = document.getElementById('personList');
  const searchQuery = document
    .getElementById('searchInput')
    .value.toLowerCase();

  const filteredPeople = people.filter((p) =>
    `${p.firstName} ${p.lastName} ${p.date} ${p.location}`
      .toLowerCase()
      .includes(searchQuery)
  );

  const totalEarned = filteredPeople.reduce(
    (sum, p) => sum + (p.amount || 1) * 8,
    0
  );

  document.getElementById(
    'totalEarned'
  ).textContent = `Totaal verdiend: €${totalEarned}`;

  // Bereken aantal per datum
  const dateCounts = {};
  filteredPeople.forEach((p) => {
    dateCounts[p.date] = (dateCounts[p.date] || 0) + (p.amount || 1);
  });

  // Sorteer op echte datumvolgorde
  const sortedDates = Object.keys(dateCounts).sort((a, b) => {
    const parseDate = (d) => {
      const [day, monthName, year] = d.split(' ');
      const months = {
        januari: 0,
        februari: 1,
      };
      return new Date(
        parseInt(year),
        months[monthName.toLowerCase()],
        parseInt(day)
      );
    };
    return parseDate(a) - parseDate(b);
  });

  const statsHTML = sortedDates
    .map((date) => `<li>${date}: ${dateCounts[date]}</li>`)
    .join('');

  document.getElementById('locationStats').innerHTML = `
  <p><strong>Aantal personen per datum:</strong></p>
  <ul>${statsHTML}</ul>
`;

  let sortedPeople = [...filteredPeople];

  // Als net toegevoegd, zet bovenaan
  if (lastAddedPerson) {
    sortedPeople = [
      lastAddedPerson,
      ...sortedPeople.filter(
        (p) =>
          !(
            p.firstName === lastAddedPerson.firstName &&
            p.lastName === lastAddedPerson.lastName &&
            p.date === lastAddedPerson.date &&
            p.location === lastAddedPerson.location
          )
      ),
    ];
    lastAddedPerson = null;
  } else {
    sortedPeople.sort((a, b) => {
      const parseDate = (text) => {
        const [day, monthName, year] = text.split(' ');
        const months = {
          januari: 0,
          februari: 1,
        };
        return new Date(
          parseInt(year),
          months[monthName.toLowerCase()],
          parseInt(day)
        );
      };

      return parseDate(a.date) - parseDate(b.date);
    });
  }

  list.innerHTML = '';

  if (sortedPeople.length === 0) {
    list.innerHTML = '<li>Geen resultaten gevonden.</li>';
    return;
  }

  sortedPeople.forEach((person, index) => {
    const li = document.createElement('li');

    // Maak een container voor standaardweergave en bewerken
    const infoDiv = document.createElement('div');
    const label = person.amount == 1 || !person.amount ? 'persoon' : 'personen';
    infoDiv.textContent = `${person.firstName} ${person.lastName} – ${
      person.date
    } – ${person.location} – ${person.amount || 1} ${label}`;

    const btnsDiv = document.createElement('div');
    btnsDiv.style.display = 'flex';
    btnsDiv.style.gap = '10px';

    // Verwijderknop
    const removeBtn = document.createElement('button');
    removeBtn.textContent = 'Verwijder';
    removeBtn.onclick = () => {
      const confirmDelete = confirm(
        `Weet je zeker dat je ${person.firstName} ${person.lastName} wilt verwijderen?`
      );
      if (!confirmDelete) return;

      const idx = people.findIndex(
        (p) =>
          p.firstName === person.firstName &&
          p.lastName === person.lastName &&
          p.date === person.date &&
          p.location === person.location
      );
      if (idx !== -1) {
        people.splice(idx, 1);
        savePeople();
        updatePersonList();
      }
    };

    // Download-foto knop
    const downloadPhotoBtn = document.createElement('button');
    downloadPhotoBtn.textContent = 'Download Ticket';
    downloadPhotoBtn.onclick = () => {
      downloadImageWithText(person);
    };

    // Wijzigknop
    const editBtn = document.createElement('button');
    editBtn.textContent = 'Wijzig';
    editBtn.onclick = () => {
      showEditForm(person, li);
    };

    btnsDiv.appendChild(downloadPhotoBtn);
    btnsDiv.appendChild(editBtn);
    btnsDiv.appendChild(removeBtn);

    li.appendChild(infoDiv);
    li.appendChild(btnsDiv);

    list.appendChild(li);
  });
}

function showEditForm(person, listItemElement) {
  const editForm = document.createElement('form');
  editForm.classList.add('edit-form');

  editForm.innerHTML = `
    <input type="text" name="firstName" value="${person.firstName}" required>
    <input type="text" name="lastName" value="${person.lastName}" required>
    <select name="date">
      <option>31 januari 2026</option>
      <option>1 februari 2026</option>
      <option>7 februari 2026</option>
      <option>8 februari 2026</option>
      <option>14 februari 2026</option>
      <option>15 februari 2026</option>
      <option>22 februari 2026</option>
    </select>
    <input type="number" name="amount" min="1" max="10" value="${
      person.amount || 1
    }" required>
    <button type="submit">Opslaan</button>
    <button type="button" class="cancel">Annuleer</button>
  `;

  // Selects instellen
  editForm.date.value = person.date;

  // Huidige inhoud wissen
  listItemElement.innerHTML = '';
  listItemElement.appendChild(editForm);

  // Opslaan van wijziging
  editForm.addEventListener('submit', function (e) {
    e.preventDefault();

    const newPerson = {
      firstName: editForm.firstName.value.trim(),
      lastName: editForm.lastName.value.trim(),
      date: editForm.date.value,
      location: dateToLocationMap[editForm.date.value] || '',
      amount: parseInt(editForm.amount.value),
    };

    const index = people.findIndex(
      (p) =>
        p.firstName === person.firstName &&
        p.lastName === person.lastName &&
        p.date === person.date &&
        (p.amount || 1) === (person.amount || 1)
    );

    if (index !== -1) {
      people[index] = newPerson;
      savePeople();
      updatePersonList();
    }
  });

  // Annuleer-knop
  editForm.querySelector('.cancel').addEventListener('click', () => {
    updatePersonList();
  });
}

document
  .getElementById('searchInput')
  .addEventListener('input', updatePersonList);

// Initial display if you're already on page 2
if (document.getElementById('overzicht').classList.contains('active')) {
  updatePersonList();
}

document.getElementById('downloadBtn').addEventListener('click', () => {
  if (people.length === 0) {
    alert('Er zijn geen personen om te exporteren.');
    return;
  }

  const sortedPeople = [...people].sort((a, b) => {
    const parseDate = (text) => {
      const [day, monthName, year] = text.split(' ');
      const months = {
        januari: 0,
        februari: 1,
      };
      return new Date(
        parseInt(year),
        months[monthName.toLowerCase()],
        parseInt(day)
      );
    };
    return parseDate(a.date) - parseDate(b.date);
  });

  const totalEarned = sortedPeople.reduce(
    (sum, p) => sum + (p.amount || 1) * 8,
    0
  );
  const totalRow = [`Totaal bedrag: €${totalEarned}`];

  const data = sortedPeople.map((p, index) => ({
    Nummer: (index + 1).toString().padStart(3, '0'),
    Datum: p.date,
    Locatie: p.location,
    Naam: p.lastName,
    Voornaam: p.firstName,
    Aantal: p.amount || 1,
  }));

  // Voeg gegevens toe met kolomtitels startend op rij 2 (A2)
  const worksheet = XLSX.utils.json_to_sheet(data, { origin: 'A3' });

  // Voeg totaalbedrag op rij 1 toe
  XLSX.utils.sheet_add_aoa(worksheet, [totalRow], { origin: 'A1' });

  // Merge totaalbedrag over kolommen A–F
  worksheet['!merges'] = [{ s: { r: 0, c: 0 }, e: { r: 0, c: 5 } }];

  // ✅ Vetgedrukte totaalbedrag rij
  worksheet['A1'].s = {
    font: { bold: true, sz: 14 },
    alignment: { horizontal: 'center' },
  };

  // ✅ Vetgedrukte kolomkoppen (rij 2)
  const headers = ['A3', 'B3', 'C3', 'D3', 'E3', 'F3'];
  headers.forEach((cell) => {
    if (worksheet[cell]) {
      worksheet[cell].s = {
        font: { bold: true, color: { rgb: 'FFFFFF' } },
        fill: { fgColor: { rgb: '1F497D' } },
        alignment: { horizontal: 'center' },
      };
    }
  });

  // Kolombreedtes instellen
  worksheet['!cols'] = [
    { wch: 10 }, // Nummer
    { wch: 18 }, // Datum
    { wch: 20 }, // Locatie
    { wch: 20 }, // Naam
    { wch: 20 }, // Voornaam
    { wch: 10 }, // Aantal
  ];

  const workbook = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(workbook, worksheet, 'Toneelgroep_De_Ballon');

  XLSX.writeFile(workbook, 'Toneelgroep_De_Ballon.xlsx');
});

document.getElementById('date').addEventListener('change', function () {
  const selectedDate = this.value;
  const location = dateToLocationMap[selectedDate] || '';
  document.getElementById('location').value = location;
});

function downloadImageWithText(person) {
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

    // ✏️ HIER pas je de posities aan (X, Y)
    const nameX = 860;
    const nameY = 396;

    const dateX = 695;
    const dateY = 315;

    const locationX = 690;
    const locationY = 356;

    // 🖊️ Tekst tekenen op aangepaste posities
    ctx.fillText(person.date, dateX, dateY);
    ctx.fillText(person.location, locationX, locationY);
    ctx.fillText(`${person.lastName} ${person.firstName}`, nameX, nameY);

    const link = document.createElement('a');
    link.download = `${person.lastName}_${person.firstName}-Ticket-Rare_Wereld.png`;
    link.href = canvas.toDataURL('image/png');
    link.click();
  };
}
