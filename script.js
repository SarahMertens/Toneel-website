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

      const newPerson = { firstName, lastName, date, location };
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
  const sortBy = document.getElementById('sortSelect').value;
  const searchQuery = document
    .getElementById('searchInput')
    .value.toLowerCase();

  const filteredPeople = people.filter((p) =>
    `${p.firstName} ${p.lastName} ${p.date} ${p.location}`
      .toLowerCase()
      .includes(searchQuery)
  );

  const totalEarned = filteredPeople.length * 8;
  document.getElementById(
    'totalEarned'
  ).textContent = `Totaal verdiend: €${totalEarned}`;

  // Bereken aantal per datum
  const dateCounts = {};
  filteredPeople.forEach((p) => {
    dateCounts[p.date] = (dateCounts[p.date] || 0) + 1;
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
      if (sortBy === 'date') {
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
      } else {
        return a[sortBy].localeCompare(b[sortBy]);
      }
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
    infoDiv.textContent = `${person.firstName} ${person.lastName} – ${person.date} – ${person.location}`;

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

    // Wijzigknop
    const editBtn = document.createElement('button');
    editBtn.textContent = 'Wijzig';
    editBtn.onclick = () => {
      showEditForm(person, li);
    };

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
    <select name="location">
      <option>OC De Boeie</option>
      <option>OC De Schelpe</option>
      <option>OC De Blomme</option>
      <option>OC De Ballon</option>
    </select>
    <button type="submit">Opslaan</button>
    <button type="button" class="cancel">Annuleer</button>
  `;

  // Selects instellen
  editForm.date.value = person.date;
  editForm.location.value = person.location;

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
      location: editForm.location.value,
    };

    const index = people.findIndex(
      (p) =>
        p.firstName === person.firstName &&
        p.lastName === person.lastName &&
        p.date === person.date &&
        p.location === person.location
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
  .getElementById('sortSelect')
  .addEventListener('change', updatePersonList);
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

  const data = people.map((p) => ({
    Voornaam: p.firstName,
    Achternaam: p.lastName,
    Datum: p.date,
    Locatie: p.location,
  }));

  const worksheet = XLSX.utils.json_to_sheet(data);

  // Stijl headers: vet, witte tekst, blauwe achtergrond, gecentreerd
  const headerRange = XLSX.utils.decode_range(worksheet['!ref']);
  for (let C = headerRange.s.c; C <= headerRange.e.c; ++C) {
    const cellAddress = XLSX.utils.encode_cell({ r: 0, c: C });
    if (!worksheet[cellAddress]) continue;
    worksheet[cellAddress].s = {
      font: { bold: true, color: { rgb: 'FFFFFF' } },
      fill: { fgColor: { rgb: '1F497D' } },
      alignment: { horizontal: 'center' },
    };
  }

  // Kolombreedtes instellen (wch = width characters)
  worksheet['!cols'] = [
    { wch: 15 }, // Voornaam
    { wch: 20 }, // Achternaam
    { wch: 18 }, // Datum
    { wch: 15 }, // Locatie
  ];

  const workbook = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(workbook, worksheet, 'Personen');

  XLSX.writeFile(workbook, 'personen.xlsx');
});

document.getElementById('date').addEventListener('change', function () {
  const selectedDate = this.value;
  const location = dateToLocationMap[selectedDate] || '';
  document.getElementById('location').value = location;
});
