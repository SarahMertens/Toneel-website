 let people = JSON.parse(localStorage.getItem('peopleList')) || [];
let lastAddedPerson = null;

const dateToLocationMap = {
  '2026-01-31': 'OC De Boeie',
  '2026-02-01': 'OC De Boeie',
  '2026-02-07': 'OC De Schelpe',
  '2026-02-08': 'OC De Schelpe',
  '2026-02-14': 'OC De Blomme',
  '2026-02-15': 'OC De Blomme',
  '2026-02-22': 'OC De Ballon',
};

// Hulpfunctie: formatteer datum naar Nederlandse weergave
function formatDateToDutch(isoDate) {
  if (!isoDate) return 'Nog niet gekozen';
  
  const months = [
    'januari', 'februari', 'maart', 'april', 'mei', 'juni',
    'juli', 'augustus', 'september', 'oktober', 'november', 'december'
  ];
  const date = new Date(isoDate + 'T00:00:00');
  const day = date.getDate();
  const month = months[date.getMonth()];
  const year = date.getFullYear();
  return `${day} ${month} ${year}`;
}

// Hulpfunctie: sorteer op datum (entries zonder datum komen achteraan)
function sortByDate(a, b) {
  // Entries zonder datum komen achteraan
  if (!a.date && !b.date) return 0;
  if (!a.date) return 1;
  if (!b.date) return -1;
  
  const dateA = new Date(a.date + 'T00:00:00');
  const dateB = new Date(b.date + 'T00:00:00');
  return dateA - dateB;
}

function savePeople() {
  try {
    localStorage.setItem('peopleList', JSON.stringify(people));
  } catch (e) {
    alert('Fout bij opslaan: localStorage is mogelijk vol of uitgeschakeld.');
    console.error('localStorage error:', e);
  }
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
    const date = document.getElementById('date').value || null;
    const location = document.getElementById('location').value.trim();

    if (firstName && lastName) {
      const duplicate = people.some(
        (p) =>
          p.firstName.toLowerCase() === firstName.toLowerCase() &&
          p.lastName.toLowerCase() === lastName.toLowerCase() &&
          p.date === date &&
          p.location === location
      );

      if (duplicate) {
        alert('Deze persoon is al toegevoegd met dezelfde datum en locatie.');
        return;
      }

      const newPerson = { 
        firstName, 
        lastName, 
        date, 
        location, 
        amount,
        addedAt: Date.now() // Timestamp voor sortering
      };
      people.push(newPerson);
      lastAddedPerson = newPerson;
      savePeople();
      this.reset();
      showPage('overzicht');
    }
  });

function updatePersonList() {
  const list = document.getElementById('personList');
  const searchQuery = document
    .getElementById('searchInput')
    .value.toLowerCase();

  const filteredPeople = people.filter((p) => {
    const dutchDate = formatDateToDutch(p.date);
    return `${p.firstName} ${p.lastName} ${dutchDate} ${p.location}`
      .toLowerCase()
      .includes(searchQuery);
  });

  const totalEarned = filteredPeople.reduce(
    (sum, p) => sum + (p.amount || 1) * 8,
    0
  );

  document.getElementById(
    'totalEarned'
  ).textContent = `Totaal verdiend: €${totalEarned}`;

  // Bereken aantal per datum (inclusief 'Geen datum')
  const dateCounts = {};
  filteredPeople.forEach((p) => {
    const dutchDate = formatDateToDutch(p.date);
    dateCounts[dutchDate] = (dateCounts[dutchDate] || 0) + (p.amount || 1);
  });

  // Sorteer op datum (zonder datum komt achteraan)
  const sortedDates = Object.keys(dateCounts).sort((a, b) => {
    if (a === 'Nog niet gekozen') return 1;
    if (b === 'Nog niet gekozen') return -1;
    
    const findISODate = (dutchDate) => {
      const person = filteredPeople.find(p => formatDateToDutch(p.date) === dutchDate);
      return person ? person.date : '';
    };
    const dateA = new Date(findISODate(a) + 'T00:00:00');
    const dateB = new Date(findISODate(b) + 'T00:00:00');
    return dateA - dateB;
  });

  const statsHTML = sortedDates
    .map((date) => `<li>${date}: ${dateCounts[date]}</li>`)
    .join('');

  document.getElementById('locationStats').innerHTML = `
    <p><strong>Aantal personen per datum:</strong></p>
    <ul>${statsHTML}</ul>
  `;

  // Sorteer op meest recent toegevoegd (nieuwste bovenaan)
  let sortedPeople = [...filteredPeople].sort((a, b) => {
    const timeA = a.addedAt || 0;
    const timeB = b.addedAt || 0;
    return timeB - timeA; // Omgekeerde volgorde: nieuwste eerst
  });

  list.innerHTML = '';

  if (sortedPeople.length === 0) {
    list.innerHTML = '<li>Geen resultaten gevonden.</li>';
    return;
  }

  sortedPeople.forEach((person) => {
    const li = document.createElement('li');

    const infoDiv = document.createElement('div');
    infoDiv.classList.add('person-info');
    const label = person.amount == 1 || !person.amount ? 'persoon' : 'personen';
    const dutchDate = formatDateToDutch(person.date);
    const locationText = person.location || 'Geen locatie';
    infoDiv.textContent = `${person.firstName} ${person.lastName} – ${dutchDate} – ${locationText} – ${person.amount || 1} ${label}`;

    const btnsDiv = document.createElement('div');
    btnsDiv.classList.add('knoppen-wrapper');
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
          p.location === person.location &&
          p.addedAt === person.addedAt
      );
      if (idx !== -1) {
        people.splice(idx, 1);
        savePeople();
        updatePersonList();
      }
    };

    // Download-foto knop (alleen als datum aanwezig is)
    const downloadPhotoBtn = document.createElement('button');
    downloadPhotoBtn.textContent = 'Download Ticket';
    if (!person.date) {
      downloadPhotoBtn.disabled = true;
      downloadPhotoBtn.title = 'Selecteer eerst een datum';
    } else {
      downloadPhotoBtn.onclick = () => {
        downloadImageWithText(person);
      };
    }

    // Wijzigknop
    const editBtn = document.createElement('button');
    editBtn.textContent = 'Wijzig';
    editBtn.onclick = () => {
      showEditForm(person, li);
    };

    removeBtn.classList.add('verwijder-knop');
    downloadPhotoBtn.classList.add('download-knop');
    editBtn.classList.add('wijzig-knop');

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

  const dateValue = person.date || '';
  const isDateSelected = !!person.date;

  editForm.innerHTML = `
    <input type="text" name="firstName" value="${person.firstName}" required>
    <input type="text" name="lastName" value="${person.lastName}" required>
    <input type="date" name="date" value="${dateValue}">
    <input type="text" name="location" value="${person.location || ''}" ${isDateSelected ? 'readonly' : ''}>
    <input type="number" name="amount" min="1" max="10" value="${person.amount || 1}" required>
    <button type="submit">Opslaan</button>
    <button type="button" class="cancel">Annuleer</button>
  `;

  // Update locatie wanneer datum verandert
  editForm.date.addEventListener('change', function() {
    const selectedDate = this.value;
    if (selectedDate) {
      const location = dateToLocationMap[selectedDate] || '';
      editForm.location.value = location;
      editForm.location.readOnly = true;
    } else {
      editForm.location.readOnly = false;
    }
  });

  // Huidige inhoud wissen
  listItemElement.innerHTML = '';
  listItemElement.appendChild(editForm);

  // Opslaan van wijziging
  editForm.addEventListener('submit', function (e) {
    e.preventDefault();

    const newPerson = {
      firstName: editForm.firstName.value.trim(),
      lastName: editForm.lastName.value.trim(),
      date: editForm.date.value || null,
      location: editForm.location.value.trim(),
      amount: parseInt(editForm.amount.value),
      addedAt: person.addedAt || Date.now() // Behoud originele timestamp
    };

    const index = people.findIndex(
      (p) =>
        p.firstName === person.firstName &&
        p.lastName === person.lastName &&
        p.date === person.date &&
        p.location === person.location &&
        p.addedAt === person.addedAt
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

// Initial display
if (document.getElementById('overzicht')?.classList.contains('active')) {
  updatePersonList();
}

document.getElementById('downloadBtn').addEventListener('click', () => {
  if (people.length === 0) {
    alert('Er zijn geen personen om te exporteren.');
    return;
  }

  const sortedPeople = [...people].sort(sortByDate);

  const totalEarned = sortedPeople.reduce(
    (sum, p) => sum + (p.amount || 1) * 8,
    0
  );
  const totalRow = [`Totaal bedrag: €${totalEarned}`];

  const data = sortedPeople.map((p, index) => ({
    Nummer: (index + 1).toString().padStart(3, '0'),
    Datum: formatDateToDutch(p.date),
    Locatie: p.location || 'Geen locatie',
    Naam: p.lastName,
    Voornaam: p.firstName,
    Aantal: p.amount || 1,
  }));

  const worksheet = XLSX.utils.json_to_sheet(data, { origin: 'A3' });
  XLSX.utils.sheet_add_aoa(worksheet, [totalRow], { origin: 'A1' });

  worksheet['!merges'] = [{ s: { r: 0, c: 0 }, e: { r: 0, c: 5 } }];

  worksheet['A1'].s = {
    font: { bold: true, sz: 14 },
    alignment: { horizontal: 'center' },
  };

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

  worksheet['!cols'] = [
    { wch: 10 },
    { wch: 18 },
    { wch: 20 },
    { wch: 20 },
    { wch: 20 },
    { wch: 10 },
  ];

  const workbook = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(workbook, worksheet, 'Toneelgroep_De_Ballon');

  XLSX.writeFile(workbook, 'Toneelgroep_De_Ballon.xlsx');
});

document.getElementById('date').addEventListener('change', function () {
  const selectedDate = this.value;
  const locationField = document.getElementById('location');
  
  if (selectedDate) {
    const location = dateToLocationMap[selectedDate] || '';
    locationField.value = location;
    locationField.readOnly = true;
  } else {
    locationField.readOnly = false;
    locationField.value = '';
  }
});

function downloadImageWithText(person) {
  if (!person.date) {
    alert('Kan geen ticket genereren: geen datum geselecteerd.');
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

    const nameX = 860;
    const nameY = 396;
    const dateX = 695;
    const dateY = 315;
    const locationX = 690;
    const locationY = 356;

    const dutchDate = formatDateToDutch(person.date);

    ctx.fillText(dutchDate, dateX, dateY);
    ctx.fillText(person.location, locationX, locationY);
    ctx.fillText(`${person.lastName} ${person.firstName}`, nameX, nameY);

    const link = document.createElement('a');
    link.download = `${person.lastName}_${person.firstName}-Ticket-Rare_Wereld.png`;
    link.href = canvas.toDataURL('image/png');
    link.click();
  };

  img.onerror = () => {
    alert('Fout: kan de afbeelding niet laden. Controleer of het bestand bestaat.');
  };
}
