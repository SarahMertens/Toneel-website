// The only JavaScript in the app: the browser APIs that C# cannot call
// directly. Loaded on demand by Services/BrowserFiles.cs.

/** Offers bytes as a file download. */
export function downloadFile(fileName, contentType, bytes) {
  const url = URL.createObjectURL(new Blob([bytes], { type: contentType }));
  save(url, fileName);
  URL.revokeObjectURL(url);
}

// Where each line goes on the 1240 × 584 ticket image, in pixels.
const TICKET_TEXT = {
  date: { x: 695, y: 311 },
  location: { x: 690, y: 352 },
  name: { x: 860, y: 392 },
};

/** Draws date, location and name on the ticket image and downloads the result as PNG. */
export async function downloadTicket(imageUrl, fileName, date, location, name) {
  const image = new Image();
  image.src = imageUrl;
  await image.decode(); // rejects when the image cannot be loaded

  const canvas = document.createElement('canvas');
  canvas.width = image.naturalWidth;
  canvas.height = image.naturalHeight;

  const context = canvas.getContext('2d');
  context.drawImage(image, 0, 0);
  context.font = '28px Arial, sans-serif';
  context.fillStyle = 'black';
  context.textAlign = 'center';

  // The fourth argument is a maximum width: a long name is squeezed
  // instead of running off the dotted line.
  context.fillText(date, TICKET_TEXT.date.x, TICKET_TEXT.date.y, 500);
  context.fillText(location, TICKET_TEXT.location.x, TICKET_TEXT.location.y, 500);
  context.fillText(name, TICKET_TEXT.name.x, TICKET_TEXT.name.y, 420);

  save(canvas.toDataURL('image/png'), fileName);
}

/**
 * Draws the demo ticket from scratch, in the style of the app: a yellow stub
 * with the number of seats and the details of the invented reservation.
 */
export async function downloadDemoTicket(fileName, company, title, date, location, name, seats) {
  const display = '"Big Shoulders Display", "Arial Narrow", sans-serif';
  const body = '"Atkinson Hyperlegible", Arial, sans-serif';

  // Canvas text falls back silently to another font when one is not loaded yet.
  await Promise.all([
    document.fonts.load(`800 100px ${display}`),
    document.fonts.load(`400 30px ${body}`),
    document.fonts.load(`700 30px ${body}`),
  ]);

  const width = 1240;
  const height = 584;
  const stub = 300;
  const ink = '#17223b';
  const stock = '#f7d34a';

  const canvas = document.createElement('canvas');
  canvas.width = width;
  canvas.height = height;
  const c = canvas.getContext('2d');

  c.fillStyle = '#ffffff';
  c.fillRect(0, 0, width, height);

  // Stub
  c.fillStyle = stock;
  c.fillRect(0, 0, stub, height);
  c.fillStyle = ink;
  c.textAlign = 'center';
  c.font = `800 230px ${display}`;
  c.fillText(String(seats), stub / 2, 345);
  c.font = `400 34px ${body}`;
  c.fillText(seats === 1 ? 'plaats' : 'plaatsen', stub / 2, 400);

  // Perforation with a notch at both ends
  c.strokeStyle = 'rgba(23, 34, 59, 0.45)';
  c.lineWidth = 3;
  c.setLineDash([12, 10]);
  c.beginPath();
  c.moveTo(stub, 30);
  c.lineTo(stub, height - 30);
  c.stroke();
  c.setLineDash([]);
  c.fillStyle = '#e9edf1';
  for (const y of [0, height]) {
    c.beginPath();
    c.arc(stub, y, 26, 0, Math.PI * 2);
    c.fill();
  }

  // Body
  const left = stub + 64;
  const maxWidth = width - left - 64;
  c.textAlign = 'left';
  c.fillStyle = ink;
  c.font = `700 28px ${body}`;
  c.fillText(company, left, 86, maxWidth);
  c.font = `800 92px ${display}`;
  c.fillText(title, left, 186, maxWidth);

  c.fillStyle = stock;
  c.fillRect(left, 222, 120, 8);

  const rows = [
    ['Datum', date],
    ['Locatie', location],
    ['Naam', name],
  ];
  rows.forEach(([label, value], index) => {
    const y = 300 + index * 62;
    c.fillStyle = '#55607a';
    c.font = `400 26px ${body}`;
    c.fillText(label, left, y);
    c.fillStyle = ink;
    c.font = `700 34px ${body}`;
    c.fillText(value, left + 150, y, maxWidth - 150);
  });

  c.fillStyle = '#b3261e';
  c.font = `700 24px ${body}`;
  c.fillText('Demoticket met verzonnen gegevens, niet geldig als toegangsbewijs', left, height - 46, maxWidth);

  save(canvas.toDataURL('image/png'), fileName);
}

function save(url, fileName) {
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.append(link);
  link.click();
  link.remove();
}
